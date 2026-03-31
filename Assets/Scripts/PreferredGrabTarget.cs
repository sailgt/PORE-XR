using UnityEngine;

[DisallowMultipleComponent]
public sealed class PreferredGrabTarget : MonoBehaviour
{
    [Tooltip("Higher values are preferred over lower values.")]
    public int Priority = 100;
}