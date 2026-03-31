using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;
using UnityVolumeRendering;

public class MainMenuManager : MonoBehaviour
{
    public Button LoadDatasetButton;

    private VRVolumeCreator _volumeCreator;
    private TextMeshProUGUI _buttonText;
    private string _originalButtonText;
    private UniTask<VolumeRenderedObject> _intensityTask;
    private float _currentSampleRateMultiplier;
    private bool _isLoadingActive;
    // Incremented whenever we switch loading phases (e.g. intensity → label map). Each
    // LoadingProgressHandler captures its own snapshot of this value, so stale UniTask.Post
    // callbacks from a completed phase are silently dropped by the guard in UpdateProgressDisplay.
    private int _loadPhase;

    public float CurrentSampleRateMultiplier
    {
        get => _currentSampleRateMultiplier;
    }

    private void Start()
    {
        _volumeCreator = FindFirstObjectByType<VRVolumeCreator>();
        if (_volumeCreator == null)
            Debug.LogError("Could not find VRVolumeCreator in scene.");

        _buttonText = LoadDatasetButton.GetComponentInChildren<TextMeshProUGUI>();
        if (_buttonText != null)
            _originalButtonText = _buttonText.text;

        // RemoveListener before AddListener prevents duplicate subscriptions when domain reload is disabled.
        LoadDatasetButton.onClick.RemoveListener(ShowIntensityFileBrowser);
        LoadDatasetButton.onClick.AddListener(ShowIntensityFileBrowser);
        
        //set initial sample rate multiplier
        _currentSampleRateMultiplier = .5f;
    }

    private void ShowIntensityFileBrowser()
    {
        FileBrowser.Instance.gameObject.transform.root.gameObject.SetActive(true);
        FileBrowser.SetFilters(false, new FileBrowser.Filter("NRRD Files", ".nrrd"));
        if (!FileBrowser.IsOpen)
        {
            FileBrowser.ShowLoadDialog(OnIntensitySelected, OnIntensityCancelled,
                FileBrowser.PickMode.Files, false, null, null, null,
                "Load Intensity File");
        }
    }

    private void ShowLabelMapFileBrowser()
    {
        FileBrowser.Instance.gameObject.transform.root.gameObject.SetActive(true);

        FileBrowser.SetFilters(false, new FileBrowser.Filter("NRRD Files", ".nrrd"));
        if (!FileBrowser.IsOpen)
        {
            FileBrowser.ShowLoadDialog(OnLabelMapSelected, OnLabelMapCancelled,
                FileBrowser.PickMode.Files, false, null, null, null,
                "Load Label Map File");
        }
    }

    private void OnIntensityCancelled()
    {
        FileBrowser.Instance.gameObject.transform.root.gameObject.SetActive(false);
        SetButtonLoading(false);
    }

    private void OnIntensitySelected(string[] paths)
    {
        SetButtonLoading(true);
        // Intensity is the only known dataset right now, so it owns 0–100%.
        // Capture the current phase so this handler's UniTask.Post callbacks can be invalidated
        // the moment we advance to the label map phase.
        int phase = _loadPhase;
        var intensityHandler = new LoadingProgressHandler(
            p => { if (_loadPhase == phase) UpdateProgressDisplay(p); }, 0f, 1f);
        _intensityTask = _volumeCreator.CreateAndSetupIntensityVolumeAsync(paths[0], intensityHandler).Preserve();
        ShowLabelMapFileBrowser();
    }

    private void OnLabelMapSelected(string[] paths)
    {
        FileBrowser.Instance.gameObject.transform.root.gameObject.SetActive(false);
        LoadLabelMapAsync(paths[0]).Forget();
    }

    private void OnLabelMapCancelled()
    {
        FileBrowser.Instance.gameObject.transform.root.gameObject.SetActive(false);
        // No label map is coming, so enable the intensity renderer as soon as it is ready.
        EnableIntensityRendererAsync().Forget();
        SetButtonLoading(false);
    }

    /// <summary>
    /// Waits for the intensity task to finish (may already be done) then enables its renderer.
    /// Called when the label map dialog is cancelled so the intensity volume isn't left hidden.
    /// </summary>
    private async UniTaskVoid EnableIntensityRendererAsync()
    {
        VolumeRenderedObject intensityVolume = null;
        try
        {
            intensityVolume = await _intensityTask;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Intensity volume load threw an exception: {ex.Message}");
            return;
        }

        if (intensityVolume != null)
            await _volumeCreator.FinalizeIntensitySetup(intensityVolume);
    }

    private async UniTaskVoid LoadLabelMapAsync(string filePath)
    {
        // Await intensity — it started in parallel, may already be done.
        VolumeRenderedObject intensityVolume = null;
        try
        {
            intensityVolume = await _intensityTask;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Intensity volume load threw an exception: {ex.Message}");
            SetButtonLoading(false);
            return;
        }

        if (intensityVolume == null)
        {
            Debug.LogError("Intensity volume load returned null — aborting label map load.");
            SetButtonLoading(false);
            return;
        }

        // Intensity is complete. Advance the phase — this atomically invalidates any pending
        // UniTask.Post callbacks from the intensity handler that haven't fired yet (they check
        // _loadPhase == their captured phase, which is now stale).
        _loadPhase++;
        // With two datasets each owns 50%; snap the display immediately.
        UpdateProgressDisplay(0.5f);

        VolumeRenderedObject labelMap = null;
        try
        {
            int phase = _loadPhase;
            var labelMapHandler = new LoadingProgressHandler(
                p => { if (_loadPhase == phase) UpdateProgressDisplay(p); }, 0.5f, 1f);
            labelMap = await _volumeCreator.CreateAndSetupLabelMapVolumeAsync(filePath, intensityVolume, labelMapHandler);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Label map load threw an exception: {ex.Message}");
        }

        if (labelMap != null)
        {
            // Both volumes ready — enable label map renderer and finalize intensity on the same frame.
            labelMap.meshRenderer.enabled = true;
            await _volumeCreator.FinalizeIntensitySetup(intensityVolume);
        }
        else
        {
            // Label map failed — finalize intensity on its own so it isn't left hidden.
            Debug.LogWarning("Label map failed — finalizing intensity volume without label map.");
            await _volumeCreator.FinalizeIntensitySetup(intensityVolume);
        }

        SetButtonLoading(false);
    }

    private void SetButtonLoading(bool loading)
    {
        _isLoadingActive = loading;
        if (!loading) _loadPhase++; // invalidate any callbacks still in flight
        LoadDatasetButton.interactable = !loading;
        if (_buttonText != null)
            _buttonText.text = loading ? "Loading... (0%)" : _originalButtonText;
    }

    /// <summary>
    /// Updates the button label with a clamped 0–100 percentage.
    /// The _isLoadingActive guard prevents updates after loading ends.
    /// The phase token embedded in each handler's callback prevents a completed phase
    /// from overwriting a later phase's display via delayed UniTask.Post delivery.
    /// </summary>
    private void UpdateProgressDisplay(float progress)
    {
        if (!_isLoadingActive || _buttonText == null) return;
        int pct = Mathf.Clamp(Mathf.RoundToInt(progress * 100f), 0, 100);
        _buttonText.text = $"Loading... ({pct}%)";
    }

    public void SetCurrentDataSetQualityLevel(float qualityLevel)
    {
        foreach (var volume in FindObjectsByType<VolumeRenderedObject>(sortMode: FindObjectsSortMode.None))
        {
            volume.SetSamplingRateMultiplier(qualityLevel);
            _currentSampleRateMultiplier = qualityLevel;
        }
    }
}