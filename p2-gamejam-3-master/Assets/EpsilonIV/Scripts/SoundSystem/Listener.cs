// Listener.cs
using UnityEngine;
using UnityEngine.Events;

public class Listener : MonoBehaviour
{
    [Header("Hearing")]
    [Range(0f, 1f)]
    [SerializeField] private float hearingThreshold = 0.2f;

    /// <summary>
    /// C# event for code-based subscriptions. Args: loudness, source position, quality, source velocity
    /// </summary>
    public UnityAction<float, Vector3, float, Vector3> OnSoundHeard;

    /// <summary>
    /// Called by the Sound system. Decides whether to react based on hearingThreshold.
    /// </summary>
    public void CheckSound(float loudness, Vector3 sourcePos, float quality, Vector3 sourceVelocity)
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
            // --- Debug flash (heard) ---
            var flash = GetComponent<ListenerDebugFlash>();
            if (flash) flash.FlashHeard();

            OnSoundHeard?.Invoke(loudness, sourcePos, quality, sourceVelocity);
        }
        else
        {
            // --- Debug flash (checked but not heard) ---
            var flash = GetComponent<ListenerDebugFlash>();
            if (flash) flash.FlashChecked();
        }
    }


    // Optional: expose a way to adjust threshold at runtime.
    public void SetHearingThreshold(float value) => hearingThreshold = Mathf.Clamp01(value);
    public float GetHearingThreshold() => hearingThreshold;
}
