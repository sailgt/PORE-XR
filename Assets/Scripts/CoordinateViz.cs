using System.Collections.Generic;
using UnityEngine;
using UnityVolumeRendering;

/// <summary>
/// Poke-style: controller position → volume-local coords. Hold primary for text; press secondary for sphere.
/// </summary>
public class CoordinateViz : MonoBehaviour
{
    public enum ControllerHand
    {
        Left,
        Right
    }

    [Header("Controller")]
    public ControllerHand controller = ControllerHand.Right;

    [Header("Volume Targeting")]
    public VolumeRenderedObject explicitTargetVolume;

    [Tooltip("When several volumes exist, pick the one whose origin is closest to the controller.")]
    public bool useClosestVolumeWhenMultiple = true;

    [Tooltip("If any volume has InteractiveVolumeManager (your main grab volume), only those are considered when choosing closest. Avoids mixing up intensity vs label-map child volumes.")]
    public bool preferInteractiveVolumes = true;

    [Header("Hold primary (A / X) — coordinates only")]
    public Color textColor = Color.yellow;
    public float textScale = 0.02f;
    public Vector3 labelOffset = new(0f, 0.03f, 0f);
    public int decimals = 2;

    [Header("Press secondary (B / Y) — sphere annotation")]
    public Color annotationSphereColor = Color.cyan;
    public float annotationSphereLocalScale = 0.012f;

    private TextMesh coordLabel;
    private readonly List<GameObject> pinnedSpheres = new();
    private bool wasSecondaryPressed;

    private bool IsLeftHand => controller == ControllerHand.Left;

    private void Update()
    {
        bool hasPosition = XRHandInputBridge.TryGetDevicePosition(IsLeftHand, out Vector3 pokeWorld);
        bool primaryHeld = XRHandInputBridge.TryGetPrimaryButton(IsLeftHand, out bool primary) && primary;

        if (!hasPosition)
        {
            SetLabelVisible(false);
            wasSecondaryPressed = false;
            return;
        }

        VolumeRenderedObject volForPoke = ResolveVolume(pokeWorld);

        if (primaryHeld && volForPoke != null)
        {
            Vector3 local = volForPoke.transform.InverseTransformPoint(pokeWorld);
            EnsureLabel();
            coordLabel.text =
                $"({local.x.ToString($"F{decimals}")}, {local.y.ToString($"F{decimals}")}, {local.z.ToString($"F{decimals}")})";
            coordLabel.transform.position = pokeWorld + labelOffset;
            BillboardToCamera(coordLabel.transform);
            SetLabelVisible(true);
        }
        else
        {
            SetLabelVisible(false);
        }

        bool secondaryNow = XRHandInputBridge.TryGetSecondaryButton(IsLeftHand, out bool sec) && sec;
        if (secondaryNow && !wasSecondaryPressed && volForPoke != null)
            AddAnnotationSphere(pokeWorld, volForPoke);
        wasSecondaryPressed = secondaryNow;

    }

    private void AddAnnotationSphere(Vector3 pokeWorld, VolumeRenderedObject volume)
    {
        Vector3 localPos = volume.transform.InverseTransformPoint(pokeWorld);

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "VolumeAnnotationSphere";
        sphere.transform.SetParent(volume.transform, false);
        sphere.transform.localPosition = localPos;
        sphere.transform.localRotation = Quaternion.identity;
        sphere.transform.localScale = Vector3.one * annotationSphereLocalScale;

        Collider col = sphere.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        Renderer r = sphere.GetComponent<Renderer>();
        if (r != null && r.sharedMaterial != null)
        {
            r.material = new Material(r.sharedMaterial);
            r.material.color = annotationSphereColor;
        }

        pinnedSpheres.Add(sphere);
    }

    private VolumeRenderedObject ResolveVolume(Vector3 pokeWorldPosition)
    {
        if (explicitTargetVolume != null)
            return explicitTargetVolume;

        VolumeRenderedObject[] all = FindObjectsByType<VolumeRenderedObject>(FindObjectsSortMode.None);
        if (all.Length == 0)
            return null;

        VolumeRenderedObject[] candidates = all;
        if (preferInteractiveVolumes)
        {
            var list = new List<VolumeRenderedObject>();
            foreach (VolumeRenderedObject v in all)
            {
                if (v != null && v.GetComponent<InteractiveVolumeManager>() != null)
                    list.Add(v);
            }

            if (list.Count > 0)
                candidates = list.ToArray();
        }

        if (candidates.Length == 1)
            return candidates[0];

        if (!useClosestVolumeWhenMultiple)
        {
            System.Array.Sort(candidates, (a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
            return candidates[0];
        }

        VolumeRenderedObject best = null;
        float bestSqr = float.PositiveInfinity;
        int bestId = int.MaxValue;

        foreach (VolumeRenderedObject v in candidates)
        {
            if (v == null)
                continue;
            float sqr = (v.transform.position - pokeWorldPosition).sqrMagnitude;
            int id = v.GetInstanceID();
            if (sqr < bestSqr || (Mathf.Approximately(sqr, bestSqr) && id < bestId))
            {
                bestSqr = sqr;
                best = v;
                bestId = id;
            }
        }

        return best;
    }

    private void EnsureLabel()
    {
        if (coordLabel != null)
            return;
        GameObject go = new("VolumeCoordinateLabel");
        coordLabel = go.AddComponent<TextMesh>();
        coordLabel.anchor = TextAnchor.MiddleCenter;
        coordLabel.alignment = TextAlignment.Center;
        coordLabel.characterSize = textScale;
        coordLabel.fontSize = 64;
        coordLabel.color = textColor;
        coordLabel.gameObject.SetActive(false);
    }

    private void SetLabelVisible(bool visible)
    {
        if (coordLabel != null)
            coordLabel.gameObject.SetActive(visible);
    }

    private static void BillboardToCamera(Transform t)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;
        t.forward = cam.transform.forward;
    }

    private void OnDestroy()
    {
        foreach (GameObject go in pinnedSpheres)
        {
            if (go != null)
                Destroy(go);
        }
        pinnedSpheres.Clear();
    }
}
