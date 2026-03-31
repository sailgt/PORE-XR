using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using UnityVolumeRendering;

public class InteractiveVolumeManager : MonoBehaviour
{
    private VolumeRenderedObject volumeRenderedObject;
    
    void Start()
    {
        // Get reference to the VolumeRenderedObject component
        volumeRenderedObject = GetComponent<VolumeRenderedObject>();
        if (volumeRenderedObject == null)
        {
            Debug.LogError("InteractiveVolumeManager requires a VolumeRenderedObject component!");
        }
    }
    
    /// <summary>
    /// Creates a cross section plane that can be used to view slices of the volume
    /// </summary>
    /// <returns>The created CrossSectionPlane component</returns>
    [Button]
    public async UniTask<CrossSectionPlane> CreateCrossSectionPlane()
    {
        if (volumeRenderedObject == null)
        {
            Debug.LogError("VolumeRenderedObject not found! Cannot create cross section plane.");
            return null;
        }
        
        VolumeObjectFactory.SpawnCrossSectionPlane(volumeRenderedObject);
        
        // Find the created cross section plane (it will be selected in editor)
        CrossSectionPlane[] planes = FindObjectsByType<CrossSectionPlane>(FindObjectsSortMode.None);
        CrossSectionPlane newPlane = null;
        
        // Get the most recently created plane
        foreach (CrossSectionPlane plane in planes)
        {
            if (volumeRenderedObject.GetCrossSectionManager().gameObject.GetComponent<VolumeRenderedObject>() == volumeRenderedObject)
            {
                newPlane = plane;
            }
        }
        
        // Add interactivity to the plane
        if (newPlane != null)
        {
            await SetupPlaneInteractivityAsync(newPlane.gameObject);
        }
        
        return newPlane;
    }
    /// <summary>
    /// Creates a cross section plane at a specific position and rotation
    /// </summary>
    /// <param name="position">World position for the cross section plane</param>
    /// <param name="rotation">World rotation for the cross section plane</param>
    /// <returns>The created CrossSectionPlane component</returns>
    public async UniTask<CrossSectionPlane> CreateCrossSectionPlane(Vector3 position, Quaternion rotation)
    {
        CrossSectionPlane plane = await CreateCrossSectionPlane();
        if (plane != null)
        {
            plane.transform.position = position;
            plane.transform.rotation = rotation;
            plane.transform.SetParent(transform,true);
        }
        return plane;
    }
    
    /// <summary>
    /// Creates a cross section plane oriented along a specific axis
    /// </summary>
    /// <param name="axis">The axis to orient the plane along (0=X, 1=Y, 2=Z)</param>
    /// <param name="normalizedPosition">Position along the axis (0.0 to 1.0)</param>
    /// <returns>The created CrossSectionPlane component</returns>
    [Button]
    public async UniTask<CrossSectionPlane> CreateAxisAlignedCrossSection(int axis, float normalizedPosition = 0.5f)
    {
        if (volumeRenderedObject == null)
        {
            Debug.LogError("VolumeRenderedObject not found! Cannot create cross section plane.");
            return null;
        }
        
        CrossSectionPlane plane = await CreateCrossSectionPlane();
        if (plane == null) return null;
        
        // Calculate position and rotation based on axis
        Vector3 volumePosition = volumeRenderedObject.transform.position;
        Vector3 volumeScale = volumeRenderedObject.transform.lossyScale;
        Quaternion planeRotation = Quaternion.identity;
        Vector3 planePosition = volumePosition;
        
        normalizedPosition = Mathf.Clamp01(normalizedPosition);
        
        switch (axis)
        {
            case 0: // X-axis (YZ plane)
                planeRotation = Quaternion.Euler(0, 0, 90);
                planePosition.x += (normalizedPosition - 0.5f) * volumeScale.x;
                break;
            case 1: // Y-axis (XZ plane)
                planeRotation = Quaternion.Euler(90, 0, 0);
                planePosition.y += (normalizedPosition - 0.5f) * volumeScale.y;
                break;
            case 2: // Z-axis (XY plane)
                planeRotation = Quaternion.Euler(0, 0, 0);
                planePosition.z += (normalizedPosition - 0.5f) * volumeScale.z;
                break;
            default:
                Debug.LogWarning("Invalid axis specified. Using Z-axis (XY plane).");
                planeRotation = Quaternion.Euler(0, 0, 0);
                break;
        }
        
        plane.transform.position = planePosition;
        plane.transform.rotation = planeRotation;
        
        return plane;
    }
    
    /// <summary>
    /// Creates a slicing plane for more detailed slice rendering
    /// </summary>
    /// <returns>The created SlicingPlane component</returns>
    [Button]
    public async UniTask<SlicingPlane> CreateSlicingPlane()
    {
        if (volumeRenderedObject == null)
        {
            Debug.LogError("VolumeRenderedObject not found! Cannot create slicing plane.");
            return null;
        }
        
        SlicingPlane slicingPlane = volumeRenderedObject.CreateSlicingPlane();
        
        // Add interactivity to the slicing plane
        if (slicingPlane != null)
        {
            await SetupPlaneInteractivityAsync(slicingPlane.gameObject);
        }
        
        return slicingPlane;
    }

    
    /// <summary>
    /// Creates a slicing plane at a specific position and rotation
    /// </summary>
    /// <param name="localPosition">Local position relative to the volume</param>
    /// <param name="localRotation">Local rotation relative to the volume</param>
    /// <returns>The created SlicingPlane component</returns>
    [Button]
    public async UniTask<SlicingPlane> CreateSlicingPlane(Vector3 localPosition, Quaternion localRotation)
    {
        SlicingPlane plane = await CreateSlicingPlane();
        if (plane != null)
        {
            plane.transform.localPosition = localPosition;
            plane.transform.localRotation = localRotation;
        }
        return plane;
    }
    /// <summary>
    /// Creates multiple cross section planes along a specified axis
    /// </summary>
    /// <param name="axis">The axis to create planes along (0=X, 1=Y, 2=Z)</param>
    /// <param name="count">Number of planes to create</param>
    /// <returns>Array of created CrossSectionPlane components</returns>
    public async UniTask<CrossSectionPlane[]> CreateMultipleCrossSections(int axis, int count)
    {
        if (count <= 0)
        {
            Debug.LogWarning("Count must be greater than 0.");
            return new CrossSectionPlane[0];
        }
        
        CrossSectionPlane[] planes = new CrossSectionPlane[count];
        
        for (int i = 0; i < count; i++)
        {
            float normalizedPosition = (float)(i + 1) / (count + 1);
            planes[i] = await CreateAxisAlignedCrossSection(axis, normalizedPosition);
        }
        
        return planes;
    }

    
    /// <summary>
    /// Removes all cross section planes associated with this volume
    /// </summary>
    [Button]
    public void RemoveAllCrossSections()
    {
        CrossSectionPlane[] planes = FindObjectsByType<CrossSectionPlane>(FindObjectsSortMode.None);
        foreach (CrossSectionPlane plane in planes)
        {
            if (volumeRenderedObject.GetCrossSectionManager().gameObject.GetComponent<VolumeRenderedObject>() == volumeRenderedObject)
            {
                if (Application.isPlaying)
                    Destroy(plane.gameObject);
                else
                    DestroyImmediate(plane.gameObject);
            }
        }
        
        SlicingPlane[] slicingPlanes = GetComponentsInChildren<SlicingPlane>();
        foreach (SlicingPlane plane in slicingPlanes)
        {
            if (Application.isPlaying)
                Destroy(plane.gameObject);
            else
                DestroyImmediate(plane.gameObject);
        }
    }

    /// <summary>
    /// Sets up XR interactivity for a plane GameObject
    /// </summary>
    /// <param name="planeGameObject">The plane GameObject to make interactive</param>
    private async UniTask SetupPlaneInteractivityAsync(GameObject planeGameObject)
    {
        if (planeGameObject == null) return;
        
        // Add BoxCollider if it doesn't exist
        if (planeGameObject.GetComponent<BoxCollider>() == null)
        {
           BoxCollider box = planeGameObject.AddComponent<BoxCollider>();
           box.size = new Vector3(1.1f, 1.1f, 1.1f);
        }
        
        // Add Rigidbody if it doesn't exist
        if (planeGameObject.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = planeGameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }
        
        // Add XR Grab Interactable if it doesn't exist
        if (planeGameObject.GetComponent<XRGrabInteractable>() == null)
        {
            XRGrabInteractable interactable = planeGameObject.AddComponent<XRGrabInteractable>();
            interactable.selectMode = InteractableSelectMode.Multiple;
            interactable.throwOnDetach = false;
            interactable.useDynamicAttach = true;
            
            // Wait for the grab transformer to be added automatically
            await UniTask.WaitUntil(() => planeGameObject.GetComponent<XRGeneralGrabTransformer>() != null);
            
            XRGeneralGrabTransformer grabTransformer = planeGameObject.GetComponent<XRGeneralGrabTransformer>();
            if (grabTransformer != null)
            {
                grabTransformer.allowTwoHandedScaling = true;
            }
            PreferredGrabTarget grabTarget = planeGameObject.AddComponent<PreferredGrabTarget>();
            grabTarget.Priority = 1;
        }
    }
    /// <summary>
    /// Creates an interactive cutout box for volume cross sections
    /// </summary>
    /// <returns>The created CutoutBox component</returns>
    [Button]
    public async UniTask<CutoutBox> CreateCutoutBox()
    {
        if (volumeRenderedObject == null)
        {
            Debug.LogError("VolumeRenderedObject not found! Cannot create cutout box.");
            return null;
        }
        
        VolumeObjectFactory.SpawnCutoutBox(volumeRenderedObject);
        
        // Find the created cutout box (it will be selected in editor)
        CutoutBox[] boxes = FindObjectsByType<CutoutBox>(FindObjectsSortMode.None);
        CutoutBox newBox = null;
        
        // Get the most recently created box that targets our volume
        foreach (CutoutBox box in boxes)
        {
            if (box.TargetObject == volumeRenderedObject)
            {
                newBox = box;
            }
        }
        
        // Add interactivity to the box
        if (newBox != null)
        {
            await SetupVolumeInteractivityAsync(newBox.gameObject);
        }
        
        return newBox;
    }
    
    /// <summary>
    /// Creates an interactive cutout box at a specific position and rotation
    /// </summary>
    /// <param name="position">World position for the cutout box</param>
    /// <param name="rotation">World rotation for the cutout box</param>
    /// <returns>The created CutoutBox component</returns>
    public async UniTask<CutoutBox> CreateCutoutBox(Vector3 position, Quaternion rotation)
    {
        CutoutBox box = await CreateCutoutBox();
        if (box != null)
        {
            box.transform.position = position;
            box.transform.rotation = rotation;
            box.transform.parent = volumeRenderedObject.transform;
        }
        await SetupVolumeInteractivityAsync(box.gameObject);
        return box;
    }
    
    /// <summary>
    /// Creates an interactive cutout box with specific scale
    /// </summary>
    /// <param name="position">World position for the cutout box</param>
    /// <param name="rotation">World rotation for the cutout box</param>
    /// <param name="scale">Local scale for the cutout box</param>
    /// <returns>The created CutoutBox component</returns>
    [Button]
    public async UniTask<CutoutBox> CreateCutoutBox(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        CutoutBox box = await CreateCutoutBox(position, rotation);
        if (box != null)
        {
            box.transform.position = position;
            box.transform.rotation = rotation;
            box.transform.localScale = scale;
            box.transform.parent = volumeRenderedObject.transform;
        }

        await SetupVolumeInteractivityAsync(box.gameObject);
        return box;
    }
    
    /// <summary>
    /// Creates an interactive cutout sphere for volume cross sections
    /// </summary>
    /// <returns>The created CutoutSphere component</returns>
    [Button]
    public async UniTask<CutoutSphere> CreateCutoutSphere()
    {
        if (volumeRenderedObject == null)
        {
            Debug.LogError("VolumeRenderedObject not found! Cannot create cutout sphere.");
            return null;
        }
        
        VolumeObjectFactory.SpawnCutoutSphere(volumeRenderedObject);
        
        // Find the created cutout sphere (it will be selected in editor)
        CutoutSphere[] spheres = FindObjectsByType<CutoutSphere>(FindObjectsSortMode.None);
        CutoutSphere newSphere = null;
        
        // Get the most recently created sphere that targets our volume
        foreach (CutoutSphere sphere in spheres)
        {
            if (sphere.GetComponent<CutoutSphere>().TargetObject == volumeRenderedObject)
            {
                newSphere = sphere;
            }
        }
        
        // Add interactivity to the sphere
        if (newSphere != null)
        {
            await SetupVolumeInteractivityAsync(newSphere.gameObject);
        }
        
        return newSphere;
    }
    
    /// <summary>
    /// Creates an interactive cutout sphere at a specific position
    /// </summary>
    /// <param name="position">World position for the cutout sphere</param>
    /// <returns>The created CutoutSphere component</returns>
    public async UniTask<CutoutSphere> CreateCutoutSphere(Vector3 position)
    {
        CutoutSphere sphere = await CreateCutoutSphere();
        if (sphere != null)
        {
            sphere.transform.position = position;
            sphere.transform.parent = volumeRenderedObject.transform;
        }
        await SetupVolumeInteractivityAsync(sphere.gameObject);
        return sphere;
    }
    
    /// <summary>
    /// Creates an interactive cutout sphere with specific radius
    /// </summary>
    /// <param name="position">World position for the cutout sphere</param>
    /// <param name="radius">Radius of the cutout sphere</param>
    /// <returns>The created CutoutSphere component</returns>
    [Button]
    public async UniTask<CutoutSphere> CreateCutoutSphere(Vector3 position, float radius)
    {
        CutoutSphere sphere = await CreateCutoutSphere(position);
        if (sphere != null)
        {
            sphere.transform.localScale = Vector3.one * radius * 2.0f; // Scale to match radius
        }
        await SetupVolumeInteractivityAsync(sphere.gameObject);
        return sphere;
    }
    
    /// <summary>
    /// Removes all cutout volumes (boxes and spheres) associated with this volume
    /// </summary>
    [Button]
    public void RemoveAllCutoutVolumes()
    {
        // Remove cutout boxes
        CutoutBox[] boxes = FindObjectsByType<CutoutBox>(FindObjectsSortMode.None);
        foreach (CutoutBox box in boxes)
        {
            if (box.TargetObject == volumeRenderedObject)
            {
               Destroy(box.gameObject);
            }
        }
        
        // Remove cutout spheres
        CutoutSphere[] spheres = FindObjectsByType<CutoutSphere>(FindObjectsSortMode.None);
        foreach (CutoutSphere sphere in spheres)
        {
            if (sphere.TargetObject ==  volumeRenderedObject)
            {
                Destroy(sphere.gameObject);
            }
        }
    }
    /// <summary>
    /// Sets up XR interactivity for volume objects (boxes and spheres)
    /// </summary>
    /// <param name="volumeGameObject">The volume GameObject to make interactive</param>
    private async UniTask SetupVolumeInteractivityAsync(GameObject volumeGameObject)
    {
        if (volumeGameObject == null) return;
        
        // Add appropriate collider based on object type
        if (volumeGameObject.GetComponent<Collider>() == null)
        {
            // Check if it's a sphere or box and add appropriate collider
            if (volumeGameObject.GetComponent<CutoutSphere>() != null)
            {
                volumeGameObject.AddComponent<SphereCollider>();
            }
            else if (volumeGameObject.GetComponent<CutoutBox>() != null)
            {
                volumeGameObject.AddComponent<BoxCollider>();
            }
            else
            {
                volumeGameObject.AddComponent<BoxCollider>(); // Default fallback
            }
        }
        
        // Add Rigidbody if it doesn't exist
        if (volumeGameObject.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = volumeGameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }
        
        // Add XR Grab Interactable if it doesn't exist
        if (volumeGameObject.GetComponent<XRGrabInteractable>() == null)
        {
            XRGrabInteractable interactable = volumeGameObject.AddComponent<XRGrabInteractable>();
            interactable.selectMode = InteractableSelectMode.Multiple;
            interactable.throwOnDetach = false;
            interactable.useDynamicAttach = true;
            
            // Wait for the grab transformer to be added automatically
            await UniTask.WaitUntil(() => volumeGameObject.GetComponent<XRGeneralGrabTransformer>() != null);
            
            XRGeneralGrabTransformer grabTransformer = volumeGameObject.GetComponent<XRGeneralGrabTransformer>();
            if (grabTransformer != null)
            {
                grabTransformer.allowTwoHandedScaling = true;
            }
        }

        if (volumeGameObject.GetComponent<PreferredGrabTarget>() == null)
        {
            PreferredGrabTarget grabTarget = volumeGameObject.AddComponent<PreferredGrabTarget>();
            grabTarget.Priority = 1;
        }
       
    }
}