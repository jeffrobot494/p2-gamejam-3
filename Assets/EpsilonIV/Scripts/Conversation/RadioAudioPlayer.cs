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

        [Header("Future Effects (TODO)")]
        [Tooltip("Audio clip for radio static overlay (not yet implemented)")]
        public AudioClip radioStaticClip;

        [Tooltip("Static intensity (0-1) (not yet implemented)")]
        [Range(0f, 1f)]
        public float staticIntensity = 0.3f;

        private AudioSource currentAudioSource;
        private GameObject currentNpcGameObject;

        void Awake()
        {
            Debug.Log("RadioAudioPlayer: Awake() called");
        }

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

            // Stop previous audio if interrupt is enabled
            if (interruptOnNewAudio && currentAudioSource != null && currentAudioSource.isPlaying)
            {
                Debug.Log($"RadioAudioPlayer: Interrupting audio from {currentNpcGameObject?.name}");
                currentAudioSource.Stop();
            }

            // Store reference for future interruption
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
                Debug.Log($"RadioAudioPlayer: Applied low-pass filter ({lowPassCutoff} Hz)");
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
                Debug.Log($"RadioAudioPlayer: Applied high-pass filter ({highPassCutoff} Hz)");
            }

            // TODO: Add audio distortion effect
            // AudioDistortionFilter distortion = audioSource.gameObject.AddComponent<AudioDistortionFilter>();
            // distortion.distortionLevel = 0.5f;

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

            Debug.Log("RadioAudioPlayer: Removed all effects");
        }
    }
}
