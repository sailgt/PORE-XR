using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityVolumeRendering;

public class VolumeManagementUIManager : MonoBehaviour
{
    public TextMeshProUGUI DataSetName;

    public Button DeleteVolumeButton;

    public Toggle IntensityVisibilityToggle;

    public Button CreateCrossSectionPlaneButton;
    public Button DeleteAllCrossSectionsButton;

    public Button CreateCutoutBoxButton;
    public Button CreateCutoutSphereButton;
    public Button DeleteAllCutoutVolumesButton;

    public GameObject VolumeManagementPanel;

    private VolumeRenderedObject _targetVolume;
    private InteractiveVolumeManager _interactiveVolumeManager;
    private CanvasGroup _panelCanvasGroup;

    private void Awake()
    {
        // Use a CanvasGroup instead of SetActive so the panel's GameObject stays active in the
        // hierarchy (HandMenu tracking continues to work) while being visually and raycast-invisible
        // when no volume is loaded. SetActive(false) would suppress raycasts, but SetActive(true)
        // re-enables them and the world-space canvas physically overlaps the wrist toggle button,
        // blocking input to it.
        _panelCanvasGroup = VolumeManagementPanel.GetComponent<CanvasGroup>();
        if (_panelCanvasGroup == null)
            _panelCanvasGroup = VolumeManagementPanel.AddComponent<CanvasGroup>();

        // Wire listeners once — Remove+Add pattern is idempotent for domain-reload safety.
        DeleteVolumeButton.onClick.RemoveListener(OnDeleteVolumeButton);
        DeleteVolumeButton.onClick.AddListener(OnDeleteVolumeButton);

        CreateCrossSectionPlaneButton.onClick.RemoveListener(OnCreateCrossSectionPlane);
        CreateCrossSectionPlaneButton.onClick.AddListener(OnCreateCrossSectionPlane);

        CreateCutoutBoxButton.onClick.RemoveListener(OnCreateCutoutBoxButton);
        CreateCutoutBoxButton.onClick.AddListener(OnCreateCutoutBoxButton);

        CreateCutoutSphereButton.onClick.RemoveListener(OnCreateCutoutSphereButton);
        CreateCutoutSphereButton.onClick.AddListener(OnCreateCutoutSphereButton);

        DeleteAllCutoutVolumesButton.onClick.RemoveListener(OnDeleteAllCutoutVolumesButton);
        DeleteAllCutoutVolumesButton.onClick.AddListener(OnDeleteAllCutoutVolumesButton);

        DeleteAllCrossSectionsButton.onClick.RemoveListener(OnDeleteAllCrossSectionsButton);
        DeleteAllCrossSectionsButton.onClick.AddListener(OnDeleteAllCrossSectionsButton);

        IntensityVisibilityToggle.onValueChanged.RemoveListener(OnIntensityVisibilityToggle);
        IntensityVisibilityToggle.onValueChanged.AddListener(OnIntensityVisibilityToggle);

        SetPanelVisible(false);
    }

    private void SetPanelVisible(bool visible)
    {
        _panelCanvasGroup.alpha          = visible ? 1f : 0f;
        _panelCanvasGroup.interactable   = visible;
        _panelCanvasGroup.blocksRaycasts = visible;
    }

    /// <summary>
    /// Points the hand menu at a specific volume. Called by VRVolumeCreator when a volume is
    /// finalized (auto-focus) or when the user grabs a volume (target switch).
    /// </summary>
    public void SetTargetVolume(VolumeRenderedObject volumeRenderedObject)
    {
        _targetVolume = volumeRenderedObject;
        _interactiveVolumeManager = volumeRenderedObject.GetComponent<InteractiveVolumeManager>();
        DataSetName.text = "DataSet: " + volumeRenderedObject.dataset.datasetName;
        IntensityVisibilityToggle.SetIsOnWithoutNotify(volumeRenderedObject.meshRenderer.enabled);
        SetPanelVisible(true);
    }

    /// <summary>
    /// Clears the current target and hides the panel. Called after a volume is deleted so no
    /// dangling references remain.
    /// </summary>
    private void ClearTarget()
    {
        _targetVolume = null;
        _interactiveVolumeManager = null;
        SetPanelVisible(false);
    }

    private void OnIntensityVisibilityToggle(bool visible)
    {
        if (_targetVolume == null) return;
        _targetVolume.meshRenderer.enabled = visible;
    }

    private void OnDeleteAllCrossSectionsButton()
    {
        if (_interactiveVolumeManager == null) return;
        _interactiveVolumeManager.RemoveAllCrossSections();
    }

    private void OnDeleteAllCutoutVolumesButton()
    {
        if (_interactiveVolumeManager == null) return;
        _interactiveVolumeManager.RemoveAllCutoutVolumes();
    }

    private void OnCreateCutoutSphereButton()
    {
        if (_targetVolume == null) return;
        _interactiveVolumeManager.CreateCutoutSphere(_targetVolume.transform.position).Forget();
    }

    private void OnCreateCutoutBoxButton()
    {
        if (_targetVolume == null) return;
        _interactiveVolumeManager.CreateCutoutBox(_targetVolume.transform.position, Quaternion.identity).Forget();
    }

    private void OnCreateCrossSectionPlane()
    {
        if (_targetVolume == null) return;
        _interactiveVolumeManager.CreateCrossSectionPlane(_targetVolume.transform.position, Quaternion.Euler(0f, 90f, 0f)).Forget();
    }

    private void OnDeleteVolumeButton()
    {
        if (_targetVolume == null) return;
        Destroy(_targetVolume.gameObject);
        ClearTarget();
    }
}
