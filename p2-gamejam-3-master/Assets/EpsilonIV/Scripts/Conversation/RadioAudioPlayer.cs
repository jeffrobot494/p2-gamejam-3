using UnityEngine;
using System.Runtime.InteropServices;

namespace EpsilonIV
{
    /// <summary>
    /// Manages radio audio playback and effects for NPC responses.
    /// Works with the SDK's built-in audio system by applying effects to NPC AudioSources.
    /// Uses Web Audio API in WebGL builds for proper filter support.
    /// </summary>
    public class RadioAudioPlayer : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Import JavaScript functions for WebGL audio filtering
        [DllImport("__Internal")]
        private static extern void ApplyRadioFiltersWebGL(string audioSourceName, bool enableLowPass, float lowPassCutoff,
            bool enableHighPass, float highPassCutoff, bool enableDistortion, float distortionAmount);

        [DllImport("__Internal")]
        private static extern void RemoveRadioFiltersWebGL(string audioSourceName);
#endif
        [Header("Audio Settings")]
        [Tooltip("Master volume for radio audio playback")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Tooltip("If true, new audio will interrupt currently playing audio")]
        public bool interruptOnNewAudio = true;

        [Tooltip("If true, audio will be 2D (not positional). Recommended for radio.")]
        public bool disable3DAudio = true;

        [Header("Radio Effects")]
        [Tooltip("Enable low-pass filter (cuts high frequencies for radio effect)")]
        public bool enableLowPassFilter = true;

        [Tooltip("Low-pass filter cutoff frequency (Hz)")]
        [Range(500f, 22000f)]
        public float lowPassCutoff = 3000f;

        [Tooltip("Enable high-pass filter (removes low rumble)")]
        public bool enableHighPassFilter = true;

        [Tooltip("High-pass filter cutoff frequency (Hz)")]
        [Range(10f, 1000f)]
        public float highPassCutoff = 300f;

        [Tooltip("Enable distortion filter (adds analog/compression character)")]
        public bool enableDistortion = false;

        [Tooltip("Distortion amount (0 = clean, 1 = heavily distorted)")]
        [Range(0f, 1f)]
        public float distortionLevel = 0.3f;

        [Header("Future Effects (TODO)")]
        [Tooltip("Audio clip for radio static overlay (not yet implemented)")]
        public AudioClip radioStaticClip;

        [Tooltip("Static intensity (0-1) (not yet implemented)")]
        [Range(0f, 1f)]
        public float staticIntensity = 0.3f;

        private AudioSource currentAudioSource;
        private GameObject currentNpcGameObject;

        /// <summary>
        /// Prepare an NPC's audio for playback with radio effects.
        /// Call this when a message is sent to an NPC, before the response arrives.
        /// </summary>
        public void PrepareNpcAudio(GameObject npcGameObject)
        {
            if (npcGameObject == null)
            {
                Debug.LogWarning("RadioAudioPlayer: Cannot prepare audio - npcGameObject is null");
                return;
            }

            Debug.Log($"RadioAudioPlayer: Preparing audio for {npcGameObject.name}");

            // Reset for new response (even if same NPC GameObject)
            // This allows Update() to detect the new AudioSource
            // NOTE: We don't interrupt here because by the time this is called,
            // the SDK has already loaded the NEW audio clip and started playing it.
            // Calling Stop() here would stop the new audio we want to hear!
            currentAudioSource = null;
            currentNpcGameObject = npcGameObject;

            // Get or wait for AudioSource (SDK creates it when audio arrives)
            // We'll apply effects in Update when the AudioSource appears
        }

        void Update()
        {
            // Check if current NPC has an AudioSource now (SDK adds it when audio arrives)
            if (currentNpcGameObject != null && currentAudioSource == null)
            {
                AudioSource npcAudioSource = currentNpcGameObject.GetComponent<AudioSource>();
                if (npcAudioSource != null)
                {
                    Debug.Log($"RadioAudioPlayer: Found AudioSource on {currentNpcGameObject.name}, applying effects");
                    ApplyRadioEffects(npcAudioSource);
                    currentAudioSource = npcAudioSource;
                }
            }
        }

        /// <summary>
        /// Apply radio effects to an AudioSource.
        /// Uses platform-specific implementations: Web Audio API for WebGL, Unity filters for Desktop/Editor.
        /// </summary>
        private void ApplyRadioEffects(AudioSource audioSource)
        {
            if (audioSource == null)
            {
                Debug.LogWarning("RadioAudioPlayer: Cannot apply effects - audioSource is null");
                return;
            }

            // Set volume (works on all platforms)
            audioSource.volume = volume;

            // Disable 3D audio (make it 2D) for radio effect (works on all platforms)
            if (disable3DAudio)
            {
                audioSource.spatialBlend = 0f; // 0 = 2D, 1 = 3D
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL build: Use Web Audio API via JavaScript plugin
            Debug.Log($"RadioAudioPlayer: Applying WebGL filters via Web Audio API");

            // CRITICAL: Must use the NPC ID (from Player2 SDK), not the GameObject name
            // The SDK's PlayWebGLAudio uses the NPC ID as the identifier
            var npc = audioSource.GetComponent<RadioNpc>();
            if (npc == null)
            {
                Debug.LogError($"RadioAudioPlayer: Cannot apply filters - no RadioNpc component found on {audioSource.gameObject.name}");
                return;
            }

            if (string.IsNullOrEmpty(npc.NpcID))
            {
                Debug.LogError($"RadioAudioPlayer: Cannot apply filters - NPC ID is null or empty for {audioSource.gameObject.name}");
                return;
            }

            try
            {
                Debug.Log($"RadioAudioPlayer: Applying filters to NPC ID: {npc.NpcID}");
                ApplyRadioFiltersWebGL(
                    npc.NpcID,  // Use NPC ID instead of GameObject name
                    enableLowPassFilter,
                    lowPassCutoff,
                    enableHighPassFilter,
                    highPassCutoff,
                    enableDistortion,
                    distortionLevel
                );
                Debug.Log($"RadioAudioPlayer: WebGL filters applied successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"RadioAudioPlayer: Failed to apply WebGL filters: {e.Message}");
            }
#else
            // Desktop/Editor build: Use Unity's built-in audio filters
            Debug.Log($"RadioAudioPlayer: Applying Unity audio filters (Desktop/Editor)");

            // Apply low-pass filter (radio frequency range)
            if (enableLowPassFilter)
            {
                AudioLowPassFilter lowPass = audioSource.GetComponent<AudioLowPassFilter>();
                if (lowPass == null)
                {
                    lowPass = audioSource.gameObject.AddComponent<AudioLowPassFilter>();
                }
                lowPass.cutoffFrequency = lowPassCutoff;
                Debug.Log($"RadioAudioPlayer: Applied low-pass filter (cutoff={lowPassCutoff} Hz)");
            }
            else
            {
                // Remove low-pass if disabled
                AudioLowPassFilter lowPass = audioSource.GetComponent<AudioLowPassFilter>();
                if (lowPass != null)
                {
                    Destroy(lowPass);
                    Debug.Log($"RadioAudioPlayer: Removed low-pass filter (disabled)");
                }
            }

            // Apply high-pass filter (remove low rumble)
            if (enableHighPassFilter)
            {
                AudioHighPassFilter highPass = audioSource.GetComponent<AudioHighPassFilter>();
                if (highPass == null)
                {
                    highPass = audioSource.gameObject.AddComponent<AudioHighPassFilter>();
                }
                highPass.cutoffFrequency = highPassCutoff;
                Debug.Log($"RadioAudioPlayer: Applied high-pass filter (cutoff={highPassCutoff} Hz)");
            }
            else
            {
                // Remove high-pass if disabled
                AudioHighPassFilter highPass = audioSource.GetComponent<AudioHighPassFilter>();
                if (highPass != null)
                {
                    Destroy(highPass);
                    Debug.Log($"RadioAudioPlayer: Removed high-pass filter (disabled)");
                }
            }

            // Apply distortion filter (analog radio character)
            if (enableDistortion)
            {
                AudioDistortionFilter distortion = audioSource.GetComponent<AudioDistortionFilter>();
                if (distortion == null)
                {
                    distortion = audioSource.gameObject.AddComponent<AudioDistortionFilter>();
                    Debug.Log($"RadioAudioPlayer: Created new distortion filter");
                }
                distortion.distortionLevel = distortionLevel;
                Debug.Log($"RadioAudioPlayer: Applied distortion filter (level={distortionLevel})");
            }
            else
            {
                // Remove distortion if disabled
                AudioDistortionFilter distortion = audioSource.GetComponent<AudioDistortionFilter>();
                if (distortion != null)
                {
                    Destroy(distortion);
                    Debug.Log($"RadioAudioPlayer: Removed distortion filter (disabled)");
                }
            }
#endif

            // TODO: Mix in radio static overlay
            // This requires more complex audio mixing
        }

        /// <summary>
        /// Stop currently playing audio.
        /// </summary>
        public void StopAudio()
        {
            if (currentAudioSource != null && currentAudioSource.isPlaying)
            {
                currentAudioSource.Stop();
                Debug.Log("RadioAudioPlayer: Stopped audio");
            }
        }

        /// <summary>
        /// Set the master volume for radio audio.
        /// </summary>
        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
            if (currentAudioSource != null)
            {
                currentAudioSource.volume = volume;
            }
            Debug.Log($"RadioAudioPlayer: Volume set to {volume}");
        }

        /// <summary>
        /// Mute/unmute the radio audio.
        /// </summary>
        public void SetMuted(bool muted)
        {
            if (currentAudioSource != null)
            {
                currentAudioSource.mute = muted;
                Debug.Log($"RadioAudioPlayer: Muted = {muted}");
            }
        }

        /// <summary>
        /// Remove all radio effects from an AudioSource.
        /// </summary>
        public void RemoveEffects(AudioSource audioSource)
        {
            if (audioSource == null) return;

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: Remove filters via JavaScript plugin
            var npc = audioSource.GetComponent<RadioNpc>();
            if (npc != null && !string.IsNullOrEmpty(npc.NpcID))
            {
                try
                {
                    RemoveRadioFiltersWebGL(npc.NpcID);  // Use NPC ID instead of GameObject name
                    Debug.Log("RadioAudioPlayer: Removed all WebGL effects");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"RadioAudioPlayer: Failed to remove WebGL filters: {e.Message}");
                }
            }
#else
            // Desktop/Editor: Remove Unity audio filter components
            AudioLowPassFilter lowPass = audioSource.GetComponent<AudioLowPassFilter>();
            if (lowPass != null) Destroy(lowPass);

            AudioHighPassFilter highPass = audioSource.GetComponent<AudioHighPassFilter>();
            if (highPass != null) Destroy(highPass);

            AudioDistortionFilter distortion = audioSource.GetComponent<AudioDistortionFilter>();
            if (distortion != null) Destroy(distortion);

            Debug.Log("RadioAudioPlayer: Removed all Unity effects");
#endif
        }
    }
}
