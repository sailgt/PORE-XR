using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

/// <summary>
/// Extends <see cref="XRGeneralGrabTransformer"/> with per-axis rotation constraints and an
/// optional up-vector lock that keeps the object's local up aligned with world up during grabs.
///
/// Axis filtering works by computing the rotational delta from the pose captured at grab-start,
/// stripping disallowed axes via swing-twist decomposition, then reapplying the filtered delta.
/// <see cref="lockUpToWorldUp"/> is applied last and overrides any tilt that would move
/// local-up away from world-up.
/// </summary>
[AddComponentMenu("XR/Transformers/XCT General Grab Transformer")]
public class XCTGeneralGrabTransformer : XRGeneralGrabTransformer
{
    // ─── Rotation Axes Enum ──────────────────────────────────────────────────

    /// <summary>
    /// Flags controlling which world-space axes the object may rotate around during a grab.
    /// Mirrors <see cref="XRGeneralGrabTransformer.ManipulationAxes"/> but for rotation.
    /// </summary>
    [Flags]
    public enum RotationAxes
    {
        /// <summary>No rotation permitted on any axis.</summary>
        None = 0,

        /// <summary>Rotation around the world X axis (pitch — tilt forward/back) is permitted.</summary>
        X = 1 << 0,

        /// <summary>Rotation around the world Y axis (yaw — horizontal spin) is permitted.</summary>
        Y = 1 << 1,

        /// <summary>Rotation around the world Z axis (roll — tilt left/right) is permitted.</summary>
        Z = 1 << 2,

        /// <summary>
        /// All rotation axes permitted.
        /// Shortcut for <c>RotationAxes.X | RotationAxes.Y | RotationAxes.Z</c>.
        /// </summary>
        All = X | Y | Z,
    }

    // ─── Serialized Fields ───────────────────────────────────────────────────

    [Header("XCT Rotation Constraints")]

    [SerializeField]
    [Tooltip("Which world-space axes the object may rotate around during a grab. " +
             "Disallowed axis components are removed via swing-twist decomposition.")]
    RotationAxes m_PermittedRotationAxes = RotationAxes.All;

    [SerializeField]
    [Tooltip("When enabled, the object's local up vector is forced to equal world up after every " +
             "rotation update — overriding any X/Z tilt and constraining to yaw (Y axis) only.")]
    bool m_LockUpToWorldUp;

    // ─── Public Properties ───────────────────────────────────────────────────

    /// <summary>
    /// Which world-space axes the object may rotate around during a grab.
    /// </summary>
    public RotationAxes permittedRotationAxes
    {
        get => m_PermittedRotationAxes;
        set => m_PermittedRotationAxes = value;
    }

    /// <summary>
    /// When <c>true</c>, the object's local up vector is always aligned with world up during
    /// grabs — this effectively restricts rotation to yaw (Y axis) only, regardless of
    /// <see cref="permittedRotationAxes"/>.
    /// </summary>
    public bool lockUpToWorldUp
    {
        get => m_LockUpToWorldUp;
        set => m_LockUpToWorldUp = value;
    }

    // ─── Private State ───────────────────────────────────────────────────────

    /// <summary>World rotation of the grabbed object at the moment OnGrab fires.
    /// Used as the reference frame for axis filtering each frame.</summary>
    Quaternion m_RotationAtGrabStart;

    // ─── XRBaseGrabTransformer Overrides ─────────────────────────────────────

    /// <inheritdoc/>
    public override void OnGrab(XRGrabInteractable grabInteractable)
    {
        base.OnGrab(grabInteractable);
        // Capture the world rotation at grab-start as the delta reference frame.
        m_RotationAtGrabStart = grabInteractable.transform.rotation;
    }

    /// <inheritdoc/>
    public override void Process(
        XRGrabInteractable grabInteractable,
        XRInteractionUpdateOrder.UpdatePhase updatePhase,
        ref Pose targetPose,
        ref Vector3 localScale)
    {
        // Let the base transformer compute the full, unconstrained pose first.
        base.Process(grabInteractable, updatePhase, ref targetPose, ref localScale);

        switch (updatePhase)
        {
            case XRInteractionUpdateOrder.UpdatePhase.Dynamic:
            case XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender:
                targetPose.rotation = FilterRotation(targetPose.rotation);
                break;
        }
    }

    // ─── Rotation Filtering ──────────────────────────────────────────────────

    /// <summary>
    /// Applies axis constraints and the optional up-lock to <paramref name="targetRotation"/>.
    /// </summary>
    Quaternion FilterRotation(Quaternion targetRotation)
    {
        bool allowX = (m_PermittedRotationAxes & RotationAxes.X) != 0;
        bool allowY = (m_PermittedRotationAxes & RotationAxes.Y) != 0;
        bool allowZ = (m_PermittedRotationAxes & RotationAxes.Z) != 0;

        // Fast-path: no filtering required.
        if (allowX && allowY && allowZ && !m_LockUpToWorldUp)
            return targetRotation;

        // Compute the rotational delta from the pose captured at grab-start so that
        // swing-twist decomposition operates on the change, not the absolute orientation.
        Quaternion delta = targetRotation * Quaternion.Inverse(m_RotationAtGrabStart);

        // Strip each disallowed world-space axis component from the delta.
        if (!allowY) delta = StripTwistAroundAxis(delta, Vector3.up);
        if (!allowX) delta = StripTwistAroundAxis(delta, Vector3.right);
        if (!allowZ) delta = StripTwistAroundAxis(delta, Vector3.forward);

        Quaternion filtered = delta * m_RotationAtGrabStart;

        // Up-lock: project the filtered forward onto the horizontal plane and rebuild
        // the rotation from it — this keeps local up == world up.
        if (m_LockUpToWorldUp)
        {
            Vector3 forward = filtered * Vector3.forward;
            forward.y = 0f;

            // Fallback: use the grab-start forward if the projected vector is degenerate.
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = m_RotationAtGrabStart * Vector3.forward;
                forward.y = 0f;
            }
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;

            filtered = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        return filtered;
    }

    /// <summary>
    /// Removes the twist component of <paramref name="delta"/> that rotates around
    /// <paramref name="worldAxis"/> (swing-twist decomposition), returning only the swing.
    /// </summary>
    static Quaternion StripTwistAroundAxis(Quaternion delta, Vector3 worldAxis)
    {
        // Project the rotation vector component onto the twist axis.
        Vector3 ra   = new Vector3(delta.x, delta.y, delta.z);
        Vector3 proj = Vector3.Project(ra, worldAxis);

        // Reconstruct the twist quaternion and normalise it.
        Quaternion twist = new Quaternion(proj.x, proj.y, proj.z, delta.w);
        float mag = Mathf.Sqrt(twist.x * twist.x + twist.y * twist.y +
                               twist.z * twist.z + twist.w * twist.w);

        // If the twist magnitude is negligible there is nothing to strip.
        if (mag < 0.0001f)
            return delta;

        twist = new Quaternion(twist.x / mag, twist.y / mag, twist.z / mag, twist.w / mag);

        // swing = Inverse(twist) * delta  →  the rotation with twist removed.
        return Quaternion.Inverse(twist) * delta;
    }
}