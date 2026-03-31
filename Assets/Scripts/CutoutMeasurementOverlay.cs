using System.Collections.Generic;
using UnityEngine;
using UnityVolumeRendering;

public class CutoutMeasurementOverlay : MonoBehaviour
{
    [Header("Label")]
    public Color textColor = Color.yellow;
    public float textScale = 0.035f;
    public int decimals = 1;

    [Header("Placement")]
    public float sphereHeightFactor = 0.65f;
    public float boxCornerOffsetFactor = 0.55f;

    [Header("Formatting")]
    public float uniformBoxTolerance = 0.001f;

    private readonly Dictionary<Component, TextMesh> labels = new();

    private void Update()
    {
        Camera cam = Camera.main;

        foreach (CutoutSphere s in FindObjectsByType<CutoutSphere>(FindObjectsSortMode.None))
        {
            TextMesh text = GetOrCreateLabel(s);
            float diameter = s.transform.lossyScale.x;
            Vector3 position = s.transform.position + s.transform.up * (diameter * sphereHeightFactor);
            UpdateLabel(text, $"D {diameter.ToString($"F{decimals}")}", position, cam);
        }

        foreach (CutoutBox b in FindObjectsByType<CutoutBox>(FindObjectsSortMode.None))
        {
            TextMesh text = GetOrCreateLabel(b);
            Vector3 scale = b.transform.lossyScale;
            Vector3 corner = b.transform.position
                             + b.transform.right * scale.x * boxCornerOffsetFactor
                             + b.transform.up * scale.y * boxCornerOffsetFactor
                             + b.transform.forward * scale.z * boxCornerOffsetFactor;

            UpdateLabel(
                text,
                GetBoxLabel(scale),
                corner,
                cam
            );
        }

        Cleanup();
    }

    private string GetBoxLabel(Vector3 scale)
    {
        bool uniform = Mathf.Abs(scale.x - scale.y) <= uniformBoxTolerance
                       && Mathf.Abs(scale.y - scale.z) <= uniformBoxTolerance;
        if (uniform)
            return $"D {scale.x.ToString($"F{decimals}")}";

        return $"{scale.x.ToString($"F{decimals}")} x {scale.y.ToString($"F{decimals}")} x {scale.z.ToString($"F{decimals}")}";
    }

    private TextMesh GetOrCreateLabel(Component key)
    {
        if (labels.TryGetValue(key, out TextMesh existing) && existing != null)
            return existing;

        GameObject go = new("MeasurementLabel");
        TextMesh text = go.AddComponent<TextMesh>();
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = textScale;
        text.fontSize = 64;
        text.color = textColor;

        labels[key] = text;
        return text;
    }

    private static void UpdateLabel(TextMesh text, string content, Vector3 worldPos, Camera cam)
    {
        text.text = content;
        text.transform.position = worldPos;

        if (cam != null)
            text.transform.rotation = Quaternion.LookRotation(text.transform.position - cam.transform.position);
    }

    private void Cleanup()
    {
        List<Component> dead = null;
        foreach (KeyValuePair<Component, TextMesh> entry in labels)
        {
            if (entry.Key != null)
                continue;

            if (entry.Value != null)
                Destroy(entry.Value.gameObject);

            dead ??= new List<Component>();
            dead.Add(entry.Key);
        }

        if (dead == null)
            return;

        foreach (Component key in dead)
            labels.Remove(key);
    }
}
