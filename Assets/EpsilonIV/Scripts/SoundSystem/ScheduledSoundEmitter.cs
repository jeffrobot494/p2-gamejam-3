using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace EpsilonIV
{
    public enum SoundTriggerMode
    {
        OnStart,        // Plays on Start() with delay
        OnTriggerEnter, // Plays when something enters collider
        OnTriggerExit,  // Plays when something exits collider
        OnInteract,     // Plays when player presses E (implements IInteractable)
        Manual          // Only plays when manually called
    }

    /// <summary>
    /// Scheduled sound emission system for level design.
    /// Plays AudioClips and broadcasts to the sound system (for alien listeners).
    /// Supports multiple trigger modes including interaction, collision, and timed sequences.
    /// </summary>
    public class ScheduledSoundEmitter : MonoBehaviour, IInteractable
    {
        [Header("Trigger Settings")]
        [Tooltip("How this sound emitter is triggered")]
        [SerializeField] private SoundTriggerMode triggerMode = SoundTriggerMode.OnStart;

        [Tooltip("Tag filter for trigger modes (e.g., 'Player'). Leave empty for any tag.")]
        [SerializeField] private string triggerTag = "Player";

        [Header("Scheduled Sounds")]
        [Tooltip("List of sounds to play when triggered")]
        [SerializeField] private List<ScheduledSound> scheduledSounds = new List<ScheduledSound>();

        [Header("Interaction Settings (OnInteract mode only)")]
        [Tooltip("Text shown to player when looking at this object")]
        [SerializeField] private string interactionPrompt = "[E] ACTIVATE";

        [Tooltip("Can only be interacted with once")]
        [SerializeField] private bool singleUse = false;

        [Tooltip("Cooldown time between interactions (0 = no cooldown)")]
        [SerializeField] private float interactionCooldown = 0f;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // State tracking
        private bool hasBeenTriggered = false;
        private float lastTriggerTime = -Mathf.Infinity;
        private bool isPlaying = false;

        // Internal components
        private SoundEmitter soundEmitter;
        private AudioSource audioSource;

        #region Unity Lifecycle

        void Awake()
        {
            // Auto-find or create SoundEmitter
            if (soundEmitter == null)
            {
                soundEmitter = GetComponent<SoundEmitter>();
                if (soundEmitter == null)
                {
                    soundEmitter = gameObject.AddComponent<SoundEmitter>();
                    if (debugMode)
                        Debug.Log($"[ScheduledSoundEmitter] Created SoundEmitter on {gameObject.name}");
                }
            }

            // Auto-find or create AudioSource
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 1f; // 3D sound
                    if (debugMode)
                        Debug.Log($"[ScheduledSoundEmitter] Created AudioSource on {gameObject.name}");
                }
            }

            // Set layer to Interactable if using OnInteract mode
            if (triggerMode == SoundTriggerMode.OnInteract)
            {
                int interactableLayer = LayerMask.NameToLayer("Interactable");
                if (interactableLayer != -1)
                {
                    gameObject.layer = interactableLayer;
                    if (debugMode)
                        Debug.Log($"[ScheduledSoundEmitter] Set layer to Interactable on {gameObject.name}");
                }
                else
                {
                    Debug.LogWarning($"[ScheduledSoundEmitter] 'Interactable' layer not found! Please create it in Project Settings > Tags and Layers");
                }
            }
        }

        void Start()
        {
            if (triggerMode == SoundTriggerMode.OnStart)
            {
                TriggerSounds();
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (triggerMode != SoundTriggerMode.OnTriggerEnter)
                return;

            if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag))
                return;

            if (debugMode)
                Debug.Log($"[ScheduledSoundEmitter] OnTriggerEnter: {other.gameObject.name}");

            TriggerSounds();
        }

        void OnTriggerExit(Collider other)
        {
            if (triggerMode != SoundTriggerMode.OnTriggerExit)
                return;

            if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag))
                return;

            if (debugMode)
                Debug.Log($"[ScheduledSoundEmitter] OnTriggerExit: {other.gameObject.name}");

            TriggerSounds();
        }

        #endregion

        #region IInteractable Implementation

        public void Interact()
        {
            if (triggerMode != SoundTriggerMode.OnInteract)
            {
                if (debugMode)
                    Debug.LogWarning($"[ScheduledSoundEmitter] Interact called but trigger mode is {triggerMode}");
                return;
            }

            TriggerSounds();
        }

        public Transform GetTransform()
        {
            return transform;
        }

        public string GetInteractionPrompt()
        {
            if (triggerMode != SoundTriggerMode.OnInteract)
                return null;

            // Don't show prompt if single use and already triggered
            if (singleUse && hasBeenTriggered)
                return null;

            // Don't show prompt if on cooldown
            if (interactionCooldown > 0f && Time.time - lastTriggerTime < interactionCooldown)
                return null;

            return interactionPrompt;
        }

        #endregion

        #region Sound Triggering

        /// <summary>
        /// Manually trigger the scheduled sounds (for Manual mode or UnityEvents)
        /// </summary>
        public void TriggerSounds()
        {
            // Check single use
            if (singleUse && hasBeenTriggered)
            {
                if (debugMode)
                    Debug.Log($"[ScheduledSoundEmitter] Single use - already triggered");
                return;
            }

            // Check cooldown
            if (interactionCooldown > 0f && Time.time - lastTriggerTime < interactionCooldown)
            {
                if (debugMode)
                    Debug.Log($"[ScheduledSoundEmitter] On cooldown ({interactionCooldown - (Time.time - lastTriggerTime):F1}s remaining)");
                return;
            }

            // Check if already playing
            if (isPlaying)
            {
                if (debugMode)
                    Debug.Log($"[ScheduledSoundEmitter] Already playing sounds");
                return;
            }

            if (scheduledSounds.Count == 0)
            {
                Debug.LogWarning($"[ScheduledSoundEmitter] No scheduled sounds configured on {gameObject.name}");
                return;
            }

            if (debugMode)
                Debug.Log($"[ScheduledSoundEmitter] Triggering {scheduledSounds.Count} scheduled sounds");

            hasBeenTriggered = true;
            lastTriggerTime = Time.time;

            StartCoroutine(PlayScheduledSounds());
        }

        /// <summary>
        /// Coroutine that plays all scheduled sounds with their delays
        /// </summary>
        private IEnumerator PlayScheduledSounds()
        {
            isPlaying = true;

            // Track all coroutines for looping sounds
            List<Coroutine> loopingCoroutines = new List<Coroutine>();

            foreach (ScheduledSound sound in scheduledSounds)
            {
                if (sound.clip == null)
                {
                    Debug.LogWarning($"[ScheduledSoundEmitter] Null AudioClip in scheduled sounds on {gameObject.name}");
                    continue;
                }

                if (sound.loop)
                {
                    // Start looping sound as separate coroutine
                    Coroutine loopCoroutine = StartCoroutine(PlayLoopingSound(sound));
                    loopingCoroutines.Add(loopCoroutine);
                }
                else
                {
                    // Wait for delay, then play once
                    yield return new WaitForSeconds(sound.delay);
                    PlaySoundOnce(sound);
                }
            }

            // If no looping sounds, we're done
            if (loopingCoroutines.Count == 0)
            {
                isPlaying = false;

                if (debugMode)
                    Debug.Log($"[ScheduledSoundEmitter] All sounds complete");
            }
            else
            {
                // Looping sounds continue indefinitely
                if (debugMode)
                    Debug.Log($"[ScheduledSoundEmitter] {loopingCoroutines.Count} looping sounds started");
            }
        }

        /// <summary>
        /// Plays a single sound once (audio + broadcast)
        /// </summary>
        private void PlaySoundOnce(ScheduledSound sound)
        {
            if (debugMode)
                Debug.Log($"[ScheduledSoundEmitter] Playing {sound.clip.name} (Vol:{sound.volume:F2}, L:{sound.loudness:F2}, Q:{sound.quality:F2})");

            // Play audio clip with volume
            if (audioSource != null)
            {
                audioSource.PlayOneShot(sound.clip, sound.volume);
            }

            // Broadcast to sound system (for aliens to hear)
            if (soundEmitter != null)
            {
                soundEmitter.EmitSound(sound.loudness, sound.quality);
            }
        }

        /// <summary>
        /// Coroutine for looping sounds with repeat interval
        /// </summary>
        private IEnumerator PlayLoopingSound(ScheduledSound sound)
        {
            // Initial delay
            yield return new WaitForSeconds(sound.delay);

            // Loop indefinitely
            while (true)
            {
                PlaySoundOnce(sound);

                // Wait for repeat interval
                yield return new WaitForSeconds(sound.repeatInterval);
            }
        }

        /// <summary>
        /// Stops all currently playing sounds
        /// </summary>
        public void StopSounds()
        {
            StopAllCoroutines();
            isPlaying = false;

            if (audioSource != null)
            {
                audioSource.Stop();
            }

            if (debugMode)
                Debug.Log($"[ScheduledSoundEmitter] Stopped all sounds");
        }

        /// <summary>
        /// Resets the emitter (useful for testing or reusable objects)
        /// </summary>
        public void Reset()
        {
            StopSounds();
            hasBeenTriggered = false;
            lastTriggerTime = -Mathf.Infinity;

            if (debugMode)
                Debug.Log($"[ScheduledSoundEmitter] Reset");
        }

        #endregion

        #region Debug / Testing

        [ContextMenu("Test: Trigger Sounds")]
        void TestTrigger()
        {
            TriggerSounds();
        }

        [ContextMenu("Test: Stop Sounds")]
        void TestStop()
        {
            StopSounds();
        }

        [ContextMenu("Test: Reset")]
        void TestReset()
        {
            Reset();
        }

        #endregion
    }

    /// <summary>
    /// Configuration for a scheduled sound emission
    /// </summary>
    [System.Serializable]
    public class ScheduledSound
    {
        [Tooltip("Audio clip to play")]
        public AudioClip clip;

        [Tooltip("Audio volume for player to hear (0-1)")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Tooltip("Loudness for alien sound broadcast (0-1)")]
        [Range(0f, 1f)]
        public float loudness = 1f;

        [Tooltip("Quality/tone of the sound (can be used for AI decision making)")]
        public float quality = 0f;

        [Tooltip("Delay before playing this sound (seconds)")]
        [Min(0f)]
        public float delay = 0f;

        [Tooltip("Should this sound loop indefinitely?")]
        public bool loop = false;

        [Tooltip("For looping sounds: time between repeats (seconds)")]
        [Min(0.1f)]
        public float repeatInterval = 5f;
    }
}
