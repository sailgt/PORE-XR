using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
#endif

/// <summary>
/// Resolves XR controller pose and buttons for OpenXR / Quest. Always filters devices so Left/Right
/// cannot be mixed up (fixes wrong-hand trigger / button reads).
/// </summary>
public static class XRHandInputBridge
{
    public static bool TryGetHandDevice(bool leftHand, out UnityEngine.XR.InputDevice legacyDevice)
    {
        legacyDevice = default;
        var list = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevice fallback = default;
        bool hasFallback = false;

        InputDevices.GetDevicesAtXRNode(leftHand ? XRNode.LeftHand : XRNode.RightHand, list);
        foreach (UnityEngine.XR.InputDevice d in list)
        {
            if (!d.isValid)
                continue;
            if (MatchesHand(d, leftHand))
            {
                legacyDevice = d;
                return true;
            }
            if (!hasFallback)
            {
                fallback = d;
                hasFallback = true;
            }
        }
        if (hasFallback)
        {
            legacyDevice = fallback;
            return true;
        }

        list.Clear();
        InputDeviceCharacteristics c = InputDeviceCharacteristics.Controller
                                     | InputDeviceCharacteristics.TrackedDevice;
        c |= leftHand ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right;
        InputDevices.GetDevicesWithCharacteristics(c, list);
        foreach (UnityEngine.XR.InputDevice d in list)
        {
            if (!d.isValid)
                continue;
            if (MatchesHand(d, leftHand))
            {
                legacyDevice = d;
                return true;
            }
            if (!hasFallback)
            {
                fallback = d;
                hasFallback = true;
            }
        }
        if (hasFallback)
        {
            legacyDevice = fallback;
            return true;
        }

        return false;
    }

    static bool MatchesHand(UnityEngine.XR.InputDevice d, bool wantLeft)
    {
        if (wantLeft)
            return d.characteristics.HasFlag(InputDeviceCharacteristics.Left);
        return d.characteristics.HasFlag(InputDeviceCharacteristics.Right);
    }

    public static bool TryGetDevicePosition(bool leftHand, out Vector3 worldPosition)
    {
        worldPosition = default;
        if (TryGetHandDevice(leftHand, out UnityEngine.XR.InputDevice legacy) && legacy.isValid)
        {
            if (legacy.TryGetFeatureValue(CommonUsages.devicePosition, out worldPosition))
                return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (TryGetInputSystemXRController(leftHand, out XRController xr))
        {
            if (TryReadVector3Control(xr, new[] { "devicePosition", "position" }, out worldPosition))
                return true;
        }
#endif
        return false;
    }

    public static bool TryGetDeviceRotation(bool leftHand, out Quaternion worldRotation)
    {
        worldRotation = Quaternion.identity;
        if (TryGetHandDevice(leftHand, out UnityEngine.XR.InputDevice legacy) && legacy.isValid)
        {
            if (legacy.TryGetFeatureValue(CommonUsages.deviceRotation, out worldRotation))
                return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (TryGetInputSystemXRController(leftHand, out XRController xr))
        {
            if (TryReadQuaternionControl(xr, new[] { "deviceRotation", "pointerRotation", "rotation" }, out worldRotation))
                return true;
        }
#endif
        return false;
    }

    public static bool TryGetPrimaryButton(bool leftHand, out bool pressed)
    {
        pressed = false;
        if (TryGetHandDevice(leftHand, out UnityEngine.XR.InputDevice legacy) && legacy.isValid)
        {
            if (legacy.TryGetFeatureValue(CommonUsages.primaryButton, out pressed))
                return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (TryGetInputSystemXRController(leftHand, out XRController xr))
        {
            if (TryReadButtonControl(xr, new[] { "primaryButton", "buttonA", "xButton" }, out pressed))
                return true;
        }
#endif
        return false;
    }

    public static bool TryGetSecondaryButton(bool leftHand, out bool pressed)
    {
        pressed = false;
        if (TryGetHandDevice(leftHand, out UnityEngine.XR.InputDevice legacy) && legacy.isValid)
        {
            if (legacy.TryGetFeatureValue(CommonUsages.secondaryButton, out pressed))
                return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (TryGetInputSystemXRController(leftHand, out XRController xr))
        {
            if (TryReadButtonControl(xr, new[] { "secondaryButton", "buttonB", "yButton" }, out pressed))
                return true;
        }
#endif
        return false;
    }

    public static bool TryGetGripButton(bool leftHand, out bool pressed)
    {
        pressed = false;
        if (TryGetHandDevice(leftHand, out UnityEngine.XR.InputDevice legacy) && legacy.isValid)
        {
            if (legacy.TryGetFeatureValue(CommonUsages.gripButton, out pressed))
                return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (TryGetInputSystemXRController(leftHand, out XRController xr))
        {
            if (TryReadButtonControl(xr, new[] { "gripButton", "gripPressed" }, out pressed))
                return true;
            if (TryReadGripAsAxis(xr, out pressed))
                return true;
        }
#endif
        return false;
    }

    /// <summary>
    /// Index trigger held past threshold (Quest: front trigger under grip).
    /// </summary>
    public static bool TryGetTriggerPressed(bool leftHand, float axisThreshold, out bool pressed)
    {
        pressed = false;
        if (TryGetHandDevice(leftHand, out UnityEngine.XR.InputDevice legacy) && legacy.isValid)
        {
            if (legacy.TryGetFeatureValue(CommonUsages.trigger, out float v))
            {
                pressed = v >= axisThreshold;
                return true;
            }
        }

#if ENABLE_INPUT_SYSTEM
        if (TryGetInputSystemXRController(leftHand, out XRController xr))
        {
            AxisControl t = xr.TryGetChildControl<AxisControl>("trigger");
            if (t != null)
            {
                pressed = t.ReadValue() >= axisThreshold;
                return true;
            }
        }
#endif
        return false;
    }

#if ENABLE_INPUT_SYSTEM
    static bool TryReadVector3Control(XRController xr, string[] paths, out Vector3 value)
    {
        value = default;
        foreach (string path in paths)
        {
            Vector3Control c = xr.TryGetChildControl<Vector3Control>(path);
            if (c != null)
            {
                value = c.ReadValue();
                return true;
            }
        }

        return false;
    }

    static bool TryReadQuaternionControl(XRController xr, string[] paths, out Quaternion value)
    {
        value = default;
        foreach (string path in paths)
        {
            QuaternionControl c = xr.TryGetChildControl<QuaternionControl>(path);
            if (c != null)
            {
                value = c.ReadValue();
                return true;
            }
        }

        return false;
    }

    static bool TryReadButtonControl(XRController xr, string[] paths, out bool pressed)
    {
        pressed = false;
        foreach (string path in paths)
        {
            ButtonControl c = xr.TryGetChildControl<ButtonControl>(path);
            if (c != null)
            {
                pressed = c.isPressed;
                return true;
            }
        }

        return false;
    }

    static bool TryReadGripAsAxis(XRController xr, out bool pressed)
    {
        pressed = false;
        AxisControl grip = xr.TryGetChildControl<AxisControl>("grip");
        if (grip == null)
            return false;
        pressed = grip.ReadValue() > 0.5f;
        return true;
    }

    static bool TryGetInputSystemXRController(bool leftHand, out XRController controller)
    {
        controller = null;
        XRController fallback = null;
        foreach (UnityEngine.InputSystem.InputDevice device in UnityEngine.InputSystem.InputSystem.devices)
        {
            if (device is not XRController xr)
                continue;
            if (!xr.enabled || !xr.added)
                continue;

            if (fallback == null)
                fallback = xr;

            bool usageLeft = HasUsage(xr, UnityEngine.InputSystem.CommonUsages.LeftHand);
            bool usageRight = HasUsage(xr, UnityEngine.InputSystem.CommonUsages.RightHand);
            if (leftHand && usageLeft)
            {
                controller = xr;
                return true;
            }
            if (!leftHand && usageRight)
            {
                controller = xr;
                return true;
            }

            string n = xr.name;
            bool nameLeft = n.IndexOf("Left", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool nameRight = n.IndexOf("Right", System.StringComparison.OrdinalIgnoreCase) >= 0;

            if (leftHand && nameLeft)
            {
                controller = xr;
                return true;
            }

            if (!leftHand && nameRight)
            {
                controller = xr;
                return true;
            }
        }

        controller = fallback;
        if (controller != null)
            return true;

        return false;
    }

    static bool HasUsage(UnityEngine.InputSystem.InputDevice device, UnityEngine.InputSystem.Utilities.InternedString usage)
    {
        var usages = device.usages;
        for (int i = 0; i < usages.Count; i++)
        {
            if (usages[i] == usage)
                return true;
        }

        return false;
    }
#endif
}
