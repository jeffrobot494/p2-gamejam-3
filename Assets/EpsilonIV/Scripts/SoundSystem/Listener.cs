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
        // --- Runtime label (TMP) ---
        var label = GetComponent<ListenerDebugLabel>();
        if (label) label.ShowChecking(loudness, quality);

        // --- Scene view label (Editor-only) ---
        #if UNITY_EDITOR
        var giz = GetComponent<ListenerDebugGizmo>();
        if (giz)
        {
            giz.lastCheckTime = Time.time;
            giz.lastLoudness = loudness;
            giz.lastQuality  = quality;
        }
        #endif

        // Threshold logic, events, etc…
        if (loudness >= hearingThreshold)
        {
            onHeard?.Invoke(loudness, sourcePos, quality);
        }
    }


    // Optional: expose a way to adjust threshold at runtime.
    public void SetHearingThreshold(float value) => hearingThreshold = Mathf.Clamp01(value);
    public float GetHearingThreshold() => hearingThreshold;
}
