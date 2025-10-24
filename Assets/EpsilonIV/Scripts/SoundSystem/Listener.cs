// Listener.cs
using UnityEngine;
using UnityEngine.Events;

public class Listener : MonoBehaviour
{
    [Header("Hearing")]
    [Range(0f, 1f)]
    [SerializeField] private float hearingThreshold = 0.2f;

    /// <summary>
    /// C# event for code-based subscriptions. Args: loudness, source position, quality
    /// </summary>
    public UnityAction<float, Vector3, float> OnSoundHeard;

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
        Debug.Log("CheckSound: " + loudness + " " + sourcePos + " " + quality);
        // Threshold logic, events, etc…
        if (loudness >= hearingThreshold)
        {
            // --- Debug flash (heard) ---
            Debug.Log("Heard");
            var flash = GetComponent<ListenerDebugFlash>();
            if (flash) flash.FlashHeard();

            OnSoundHeard?.Invoke(loudness, sourcePos, quality);
        }
        else
        {
            Debug.Log("Not Heard");
            // --- Debug flash (checked but not heard) ---
            var flash = GetComponent<ListenerDebugFlash>();
            if (flash) flash.FlashChecked();
        }
    }


    // Optional: expose a way to adjust threshold at runtime.
    public void SetHearingThreshold(float value) => hearingThreshold = Mathf.Clamp01(value);
    public float GetHearingThreshold() => hearingThreshold;
}
