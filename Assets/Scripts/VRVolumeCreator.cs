using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using UnityVolumeRendering;
using TransferFunction = UnityVolumeRendering.TransferFunction;
using IProgressHandler = UnityVolumeRendering.IProgressHandler;

public class VRVolumeCreator : MonoBehaviour
{
    [Header("UI & Visuals")]
    public Material DataBoundsMaterial;
    public Mesh DataBoundsMesh;

    [Header("Label Map")]
    public Color LabelMapColor = Color.red;

    private MainMenuManager _mainMenuManager;
    private VolumeManagementUIManager _uiManager;

    private void Start()
    {
        _mainMenuManager = FindFirstObjectByType<MainMenuManager>();
        _uiManager = FindFirstObjectByType<VolumeManagementUIManager>();
    }

    /// <summary>
    /// Loads a .nrrd intensity file, creates a volume with full XR interactivity and management UI.
    /// The MeshRenderer is hidden until texture generation is complete.
    /// </summary>
    public async UniTask<VolumeRenderedObject> CreateAndSetupIntensityVolumeAsync(string filePath, IProgressHandler progressHandler = null)
    {
        VolumeDataset dataset = await LoadNrrdDatasetAsync(filePath);
        if (dataset == null)
        {
            Debug.LogError($"Failed to load intensity dataset from: {filePath}");
            return null;
        }

        // Renderer is disabled inside InstantiateVolumeObject.
        VolumeRenderedObject volumeObject = InstantiateVolumeObject(dataset, parent: null, normalizeScale: true);
        volumeObject.transform.position = transform.position;
        volumeObject.transform.rotation = Quaternion.identity;

        // Texture generation — renderer stays hidden throughout.
        Texture3D dataTexture = await dataset.GetDataTextureAsync(progressHandler ?? NullProgressHandler.instance);
        volumeObject.meshRenderer.sharedMaterial.SetTexture("_DataTex", dataTexture);

        // Renderer and interaction components are added by FinalizeIntensitySetup, called by
        // MainMenuManager once both volumes are ready (or only intensity if label map was skipped).

        return volumeObject;
    }

    /// <summary>
    /// Loads a .nrrd label map file, creates a volume with no XR or UI, parented under the
    /// intensity volume. The MeshRenderer is hidden until texture generation is complete.
    /// </summary>
    public async UniTask<VolumeRenderedObject> CreateAndSetupLabelMapVolumeAsync(string filePath, VolumeRenderedObject intensityVolume, IProgressHandler progressHandler = null)
    {
        if (intensityVolume == null)
        {
            Debug.LogError("intensityVolume is null — cannot parent label map.");
            return null;
        }

        VolumeDataset dataset = await LoadNrrdDatasetAsync(filePath);
        if (dataset == null)
        {
            Debug.LogError($"Failed to load label map dataset from: {filePath}");
            return null;
        }

        // Parented immediately; renderer disabled inside InstantiateVolumeObject.
        VolumeRenderedObject volumeObject = InstantiateVolumeObject(dataset, parent: intensityVolume.transform, normalizeScale: false);

        // Texture generation — renderer stays hidden throughout.
        Texture3D dataTexture = await dataset.GetDataTextureAsync(progressHandler ?? NullProgressHandler.instance);
        volumeObject.meshRenderer.sharedMaterial.SetTexture("_DataTex", dataTexture);

        ApplyLabelMapColor(volumeObject);
        volumeObject.SetSamplingRateMultiplier(_mainMenuManager.CurrentSampleRateMultiplier);

        // Renderer enabling for both volumes is handled by MainMenuManager after this returns,
        // alongside FinalizeIntensitySetup so everything appears on the same frame.
        return volumeObject;
    }

    /// <summary>
    /// Shared factory that replicates VolumeObjectFactory.CreateObjectInternal synchronously,
    /// then immediately disables the MeshRenderer so the volume is invisible until the caller
    /// supplies a data texture and calls meshRenderer.enabled = true.
    ///
    /// <param name="parent">
    ///   When non-null the outer object is parented here with local-identity transform (label map).
    ///   When null the outer object is left unparented at world origin (intensity).
    /// </param>
    /// <param name="normalizeScale">
    ///   When true applies the same localScale = Vector3.one / maxDatasetScale normalisation
    ///   that VolumeObjectFactory uses for the root intensity volume.
    ///   When false the object inherits its parent's already-normalised scale (label map).
    /// </param>
    /// </summary>
    private VolumeRenderedObject InstantiateVolumeObject(VolumeDataset dataset, Transform parent, bool normalizeScale)
    {
        GameObject outerObject = new GameObject("VolumeRenderedObject_" + dataset.datasetName);
        VolumeRenderedObject volObj = outerObject.AddComponent<VolumeRenderedObject>();

        if (parent != null)
        {
            outerObject.transform.SetParent(parent, false);
            outerObject.transform.localPosition = Vector3.zero;
            outerObject.transform.localRotation = Quaternion.identity;
            outerObject.transform.localScale = Vector3.one;
        }

        GameObject meshContainer = GameObject.Instantiate((GameObject)Resources.Load("VolumeContainer"));
        volObj.volumeContainerObject = meshContainer;
        MeshRenderer meshRenderer = meshContainer.GetComponent<MeshRenderer>();

        meshContainer.transform.parent = outerObject.transform;
        meshContainer.transform.localPosition = Vector3.zero;
        meshContainer.transform.localScale = Vector3.one;

        meshRenderer.sharedMaterial = new Material(meshRenderer.sharedMaterial);
        volObj.meshRenderer = meshRenderer;
        volObj.dataset = dataset;

        Texture2D noiseTexture = NoiseTextureGenerator.GenerateNoiseTexture(512, 512);
        TransferFunction tf = TransferFunctionDatabase.CreateTransferFunction();
        TransferFunction2D tf2D = TransferFunctionDatabase.CreateTransferFunction2D();
        volObj.transferFunction = tf;
        volObj.transferFunction2D = tf2D;

        meshRenderer.sharedMaterial.SetTexture("_GradientTex", null);
        meshRenderer.sharedMaterial.SetTexture("_NoiseTex", noiseTexture);
        meshRenderer.sharedMaterial.SetTexture("_TFTex", tf.GetTexture());

        meshRenderer.sharedMaterial.EnableKeyword("MODE_DVR");
        meshRenderer.sharedMaterial.DisableKeyword("MODE_MIP");
        meshRenderer.sharedMaterial.DisableKeyword("MODE_SURF");

        meshContainer.transform.localScale = dataset.scale;
        meshContainer.transform.localRotation = dataset.rotation;

        if (normalizeScale)
        {
            float maxScale = Mathf.Max(dataset.scale.x, dataset.scale.y, dataset.scale.z);
            volObj.transform.localScale = Vector3.one / maxScale;
        }

        // Hide until the caller sets _DataTex and re-enables.
        meshRenderer.enabled = false;

        return volObj;
    }

    /// <summary>
    /// Enables the intensity renderer and adds all interaction components (XR, UI, bounds).
    /// Called by MainMenuManager at every exit point — whether both volumes loaded, label map
    /// was cancelled, or label map failed — so components are never added to an invisible volume.
    /// </summary>
    public async UniTask FinalizeIntensitySetup(VolumeRenderedObject volumeObject)
    {
        volumeObject.meshRenderer.enabled = true;
        await SetupXRInteractivity(volumeObject);   // synchronous AddComponents first, then awaits Rigidbody
        CreateVolumeBoundsVisual(volumeObject);      // needs BoxCollider added by SetupXRInteractivity
        volumeObject.SetSamplingRateMultiplier(_mainMenuManager.CurrentSampleRateMultiplier);
        _uiManager?.SetTargetVolume(volumeObject);  // auto-focus hand menu on the freshly loaded volume
    }

    private void ApplyLabelMapColor(VolumeRenderedObject volObj)
    {
        TransferFunction tf = volObj.transferFunction;
        tf.colourControlPoints.Clear();
        tf.alphaControlPoints.Clear();
        tf.AddControlPoint(new TFColourControlPoint(0f, LabelMapColor));
        tf.AddControlPoint(new TFColourControlPoint(1f, LabelMapColor));
        tf.AddControlPoint(new TFAlphaControlPoint(0f, 0f));
        tf.AddControlPoint(new TFAlphaControlPoint(1f, 1f));
        tf.GenerateTexture();
        volObj.meshRenderer.sharedMaterial.SetTexture("_TFTex", tf.GetTexture());
    }

    private async UniTask SetupXRInteractivity(VolumeRenderedObject volumeObject)
    {
        volumeObject.gameObject.AddComponent<BoxCollider>();
        volumeObject.gameObject.AddComponent<InteractiveVolumeManager>();

        // Add XCTGeneralGrabTransformer BEFORE XRGrabInteractable so that XRGrabInteractable's
        // GetOrAddComponent<XRGeneralGrabTransformer>() finds it (via inheritance) and never
        // adds a plain XRGeneralGrabTransformer on top.
        XCTGeneralGrabTransformer xctTransformer = volumeObject.gameObject.AddComponent<XCTGeneralGrabTransformer>();
        xctTransformer.allowTwoHandedScaling = true;
        xctTransformer.permittedRotationAxes = XCTGeneralGrabTransformer.RotationAxes.All;
        xctTransformer.lockUpToWorldUp = true;

        XRGrabInteractable interactable = volumeObject.gameObject.AddComponent<XRGrabInteractable>();
        interactable.selectMode = InteractableSelectMode.Multiple;
        interactable.movementType = XRBaseInteractable.MovementType.Kinematic;
        interactable.throwOnDetach = false;
        interactable.useDynamicAttach = true;

        // Grabbing this volume re-targets the hand menu, allowing the user to switch between volumes.
        interactable.selectEntered.AddListener(_ => _uiManager?.SetTargetVolume(volumeObject));

        // XRGrabInteractable.Awake adds a Rigidbody; wait for it before configuring kinematic.
        await UniTask.WaitUntil(() => volumeObject.gameObject.GetComponent<Rigidbody>() != null);
        volumeObject.GetComponent<Rigidbody>().isKinematic = true;
    }

    private async UniTask<VolumeDataset> LoadNrrdDatasetAsync(string filePath)
    {
        try
        {
            IImageFileImporter importer = ImporterFactory.CreateImageFileImporter(ImageFileFormat.NRRD);
            if (importer == null)
            {
                Debug.LogError("No NRRD importer available. Ensure UVR_USE_SIMPLEITK is defined and SimpleITK plugin is present.");
                return null;
            }

            VolumeDataset dataset = await importer.ImportAsync(filePath);

            if (dataset != null)
                Debug.Log($"Successfully loaded NRRD dataset from: {filePath} ({dataset.dimX}x{dataset.dimY}x{dataset.dimZ})");

            return dataset;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading NRRD dataset from {filePath}: {ex.Message}");
            return null;
        }
    }

    private void CreateVolumeBoundsVisual(VolumeRenderedObject volumeObject)
    {
        GameObject obj = new GameObject("VolumeObjectBoundsVisual");
        obj.transform.SetParent(volumeObject.transform);
        obj.transform.position = volumeObject.transform.position;
        obj.transform.rotation = volumeObject.transform.rotation;
        obj.AddComponent<MeshRenderer>().sharedMaterial = DataBoundsMaterial;
        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = DataBoundsMesh;
        obj.transform.localScale = volumeObject.GetComponent<Collider>().bounds.size;
    }
}
