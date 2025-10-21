// Listener.cs
using UnityEngine;
using UnityEngine.Events;

public class Listener : MonoBehaviour
{
    [Header("Hearing")]
    [Range(0f, 1f)]
    [SerializeField] private float hearingThreshold = 0.2f;

    [Tooltip("Invoked when a sound at or above threshold is heard. Args: loudness [0-1], source position, quality")]
    [SerializeField] private UnityEvent<float, Vector3, float> onHeard;

    /// <summary>
    /// Called by the Sound system. Decides whether to react based on hearingThreshold.
    /// </summary>
    public void CheckSound(float loudness, Vector3 sourcePos, float quality)
    {
        if (loudness >= hearingThreshold)
        {
            // Default reaction hook; implement gameplay in the UnityEvent or by subclassing.
            onHeard?.Invoke(loudness, sourcePos, quality);
            // Example (optional): turn to face sound
            // var dir = (sourcePos - transform.position).normalized;
            // if (dir.sqrMagnitude > 0.0001f) transform.forward = Vector3.Lerp(transform.forward, dir, 0.5f);
        }
    }

    // Optional: expose a way to adjust threshold at runtime.
    public void SetHearingThreshold(float value) => hearingThreshold = Mathf.Clamp01(value);
    public float GetHearingThreshold() => hearingThreshold;
}
