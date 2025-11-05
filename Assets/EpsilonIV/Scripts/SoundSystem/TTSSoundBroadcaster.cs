using UnityEngine;
using EpsilonIV;

/// <summary>
/// Broadcasts sounds while TTS audio is playing from NPCs.
/// Makes the player detectable by aliens when receiving radio communications.
/// </summary>
[RequireComponent(typeof(SoundEmitter))]
public class TTSSoundBroadcaster : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to RadioAudioPlayer (provides TTS playback state)")]
    [SerializeField] private RadioAudioPlayer radioAudioPlayer;

    [Header("Sound Broadcast Settings")]
    [Tooltip("Loudness of sound broadcast while TTS is playing")]
    [Range(0f, 1f)]
    [SerializeField] private float broadcastLoudness = 0.5f;

    [Tooltip("Quality parameter for sound broadcasts")]
    [SerializeField] private float soundQuality = 1f;

    [Header("Broadcast Interval")]
    [Tooltip("Time between sound broadcasts while TTS is playing")]
    [Range(0.1f, 2f)]
    [SerializeField] private float broadcastInterval = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // State
    private SoundEmitter soundEmitter;
    private bool isBroadcasting = false;
    private float nextBroadcastTime;

    private void Awake()
    {
        soundEmitter = GetComponent<SoundEmitter>();

        if (soundEmitter == null)
        {
            Debug.LogError("[TTSSoundBroadcaster] SoundEmitter component not found!");
            enabled = false;
            return;
        }

        if (radioAudioPlayer == null)
        {
            Debug.LogWarning("[TTSSoundBroadcaster] RadioAudioPlayer not assigned! TTS sound broadcasting will not work.");
        }
    }

    private void OnEnable()
    {
        if (radioAudioPlayer != null)
        {
            radioAudioPlayer.OnTTSStarted.AddListener(OnTTSStarted);
            radioAudioPlayer.OnTTSStopped.AddListener(OnTTSStopped);
        }
    }

    private void OnDisable()
    {
        if (radioAudioPlayer != null)
        {
            radioAudioPlayer.OnTTSStarted.RemoveListener(OnTTSStarted);
            radioAudioPlayer.OnTTSStopped.RemoveListener(OnTTSStopped);
        }

        StopBroadcasting();
    }

    private void Update()
    {
        if (!isBroadcasting) return;

        // Broadcast sounds at regular intervals while TTS is playing
        if (Time.time >= nextBroadcastTime)
        {
            BroadcastSound();
            nextBroadcastTime = Time.time + broadcastInterval;
        }
    }

    /// <summary>
    /// Called when TTS audio starts playing
    /// </summary>
    private void OnTTSStarted()
    {
        if (debugMode)
        {
            Debug.Log("[TTSSoundBroadcaster] TTS started - beginning sound broadcasts");
        }

        isBroadcasting = true;
        nextBroadcastTime = Time.time; // Broadcast immediately
    }

    /// <summary>
    /// Called when TTS audio stops (finished or interrupted)
    /// </summary>
    private void OnTTSStopped()
    {
        if (debugMode)
        {
            Debug.Log("[TTSSoundBroadcaster] TTS stopped - ending sound broadcasts");
        }

        StopBroadcasting();
    }

    private void StopBroadcasting()
    {
        isBroadcasting = false;
    }

    private void BroadcastSound()
    {
        if (soundEmitter == null) return;

        soundEmitter.EmitSound(broadcastLoudness, soundQuality);

        if (debugMode)
        {
            Debug.Log($"[TTSSoundBroadcaster] Broadcasting TTS sound - loudness: {broadcastLoudness:F3}");
        }
    }
}
