using UnityEngine;
using UnityEngine.UI;

public class CameraBackgroundAlphaSlider : MonoBehaviour
{
    private Camera targetCamera;
    private Slider slider;

    void Start()
    {
        // Get the slider on this object
        slider = GetComponent<Slider>();

        // Automatically find the main camera
        targetCamera = Camera.main;

        // Listen for slider changes
        slider.onValueChanged.AddListener(UpdateAlpha);

        // Initialize value
        UpdateAlpha(slider.value);
    }

    void UpdateAlpha(float value)
    {
        Color bg = targetCamera.backgroundColor;
        bg.a = value;
        targetCamera.backgroundColor = bg;
    }
}