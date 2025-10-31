using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Manages radio audio playback and effects for NPC responses.
    /// Works with the SDK's built-in audio system by applying effects to NPC AudioSources.
    /// </summary>
    public class RadioAudioPlayer : MonoBehaviour
    {
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
        /// </summary>
        private void ApplyRadioEffects(AudioSource audioSource)
        {
            if (audioSource == null)
            {
                Debug.LogWarning("RadioAudioPlayer: Cannot apply effects - audioSource is null");
                return;
            }

            // Set volume
            audioSource.volume = volume;

            // Disable 3D audio (make it 2D) for radio effect
            if (disable3DAudio)
            {
                audioSource.spatialBlend = 0f; // 0 = 2D, 1 = 3D
            }

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

            AudioLowPassFilter lowPass = audioSource.GetComponent<AudioLowPassFilter>();
            if (lowPass != null) Destroy(lowPass);

            AudioHighPassFilter highPass = audioSource.GetComponent<AudioHighPassFilter>();
            if (highPass != null) Destroy(highPass);

            AudioDistortionFilter distortion = audioSource.GetComponent<AudioDistortionFilter>();
            if (distortion != null) Destroy(distortion);

            Debug.Log("RadioAudioPlayer: Removed all effects");
        }
    }
}
