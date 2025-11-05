using UnityEngine;
using player2_sdk;

/// <summary>
/// Monitors microphone input during push-to-talk and broadcasts sounds based on voice volume.
/// Louder voice = louder broadcast sound (encourages whispering for stealth).
/// Uses Player2STT's audio data - works in both WebGL and Editor/Standalone builds.
/// </summary>
[RequireComponent(typeof(SoundEmitter))]
public class VoiceSoundBroadcaster : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to Player2STT component (provides audio data from microphone)")]
    [SerializeField] private Player2STT player2STT;

    [Header("Volume Detection")]
    [Tooltip("Minimum volume threshold to trigger sound (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float minVolumeThreshold = 0.01f;

    [Header("Sound Broadcast Settings")]
    [Tooltip("Minimum loudness to broadcast (when at minVolumeThreshold)")]
    [Range(0f, 1f)]
    [SerializeField] private float minLoudness = 0.1f;

    [Tooltip("Maximum loudness to broadcast (when at max volume)")]
    [Range(0f, 1f)]
    [SerializeField] private float maxLoudness = 0.8f;

    [Tooltip("Multiplier applied to final loudness (allows normal speech to broadcast further)")]
    [Range(1f, 10f)]
    [SerializeField] private float loudnessMultiplier = 2f;

    [Tooltip("Quality parameter for sound broadcasts")]
    [SerializeField] private float soundQuality = 1f;

    [Header("Cooldown")]
    [Tooltip("Minimum time between sound broadcasts (prevents spam)")]
    [Range(0f, 1f)]
    [SerializeField] private float broadcastCooldown = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // State
    private SoundEmitter soundEmitter;
    private bool isListening = false;
    private float lastBroadcastTime;

    private void Awake()
    {
        soundEmitter = GetComponent<SoundEmitter>();

        if (soundEmitter == null)
        {
            Debug.LogError("[VoiceSoundBroadcaster] SoundEmitter component not found!");
            enabled = false;
            return;
        }

        if (player2STT == null)
        {
            Debug.LogWarning("[VoiceSoundBroadcaster] Player2STT not assigned! Voice broadcasting will not work.");
        }
    }

    private void OnEnable()
    {
        if (player2STT != null)
        {
            player2STT.OnAudioData.AddListener(OnAudioDataReceived);
        }
    }

    private void OnDisable()
    {
        if (player2STT != null)
        {
            player2STT.OnAudioData.RemoveListener(OnAudioDataReceived);
        }

        StopListening();
    }

    /// <summary>
    /// Start listening to microphone input. Call this when push-to-talk is pressed.
    /// </summary>
    public void StartListening()
    {
        if (isListening)
        {
            if (debugMode)
            {
                Debug.LogWarning("[VoiceSoundBroadcaster] Already listening!");
            }
            return;
        }

        if (player2STT == null)
        {
            Debug.LogError("[VoiceSoundBroadcaster] Cannot start listening - Player2STT not assigned!");
            return;
        }

        isListening = true;

        if (debugMode)
        {
            Debug.Log("[VoiceSoundBroadcaster] Listening started");
        }
    }

    /// <summary>
    /// Stop listening to microphone input. Call this when push-to-talk is released.
    /// </summary>
    public void StopListening()
    {
        if (!isListening) return;

        isListening = false;

        if (debugMode)
        {
            Debug.Log("[VoiceSoundBroadcaster] Listening stopped");
        }
    }

    /// <summary>
    /// Callback from Player2STT when audio data is received from microphone
    /// </summary>
    private void OnAudioDataReceived(float[] audioData)
    {
        if (!isListening) return;
        if (audioData == null || audioData.Length == 0) return;

        // Calculate volume (RMS - Root Mean Square)
        float volume = CalculateRMS(audioData);

        if (debugMode)
        {
            Debug.Log($"[VoiceSoundBroadcaster] Mic volume: {volume:F3}");
        }

        // Only broadcast if volume exceeds threshold
        if (volume < minVolumeThreshold) return;

        // Check cooldown
        if (Time.time - lastBroadcastTime < broadcastCooldown) return;

        // Map volume to loudness
        float normalizedVolume = Mathf.Clamp01((volume - minVolumeThreshold) / (1f - minVolumeThreshold));
        float loudness = Mathf.Lerp(minLoudness, maxLoudness, normalizedVolume);

        // Apply multiplier to allow normal speech to broadcast further
        loudness *= loudnessMultiplier;

        BroadcastSound(loudness);
    }

    private float CalculateRMS(float[] samples)
    {
        if (samples == null || samples.Length == 0) return 0f;

        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }

        return Mathf.Sqrt(sum / samples.Length);
    }

    private void BroadcastSound(float loudness)
    {
        if (soundEmitter == null) return;

        soundEmitter.EmitSound(loudness, soundQuality);
        lastBroadcastTime = Time.time;

        if (debugMode)
        {
            Debug.Log($"[VoiceSoundBroadcaster] Broadcasting sound - loudness: {loudness:F3}");
        }
    }
}
