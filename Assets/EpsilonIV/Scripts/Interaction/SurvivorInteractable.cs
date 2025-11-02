using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Makes a survivor interactable so the player can rescue them by pressing E
    /// </summary>
    public class SurvivorInteractable : MonoBehaviour, IInteractable
    {
        [Header("Interaction")]
        [Tooltip("Text shown when player can rescue this survivor")]
        public string rescuePrompt = "[E] RESCUE SURVIVOR";

        [Header("References")]
        [Tooltip("Survivor component (auto-found if not assigned)")]
        public Survivor survivor;

        [Header("Feedback")]
        [Tooltip("Sound played when survivor is rescued")]
        public AudioClip rescueSound;

        [Tooltip("Disable this component after rescue (prevents re-interaction)")]
        public bool disableAfterRescue = true;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool debugMode = true;

        private AudioSource m_AudioSource;

        void Start()
        {
            // Auto-find Survivor component if not assigned
            if (survivor == null)
            {
                survivor = GetComponent<Survivor>();
                if (survivor == null)
                {
                    Debug.LogError($"[SurvivorInteractable] {gameObject.name} has no Survivor component!");
                    return;
                }
            }

            // Setup audio source if needed
            if (rescueSound != null)
            {
                m_AudioSource = GetComponent<AudioSource>();
                if (m_AudioSource == null)
                {
                    m_AudioSource = gameObject.AddComponent<AudioSource>();
                    m_AudioSource.playOnAwake = false;
                    m_AudioSource.spatialBlend = 1f; // 3D sound
                }
            }

            if (debugMode)
            {
                Debug.Log($"[SurvivorInteractable] {gameObject.name} initialized");
            }
        }

        /// <summary>
        /// Called when player presses E while looking at this survivor
        /// </summary>
        public void Interact()
        {
            if (!enabled)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[SurvivorInteractable] {gameObject.name} is disabled, ignoring interaction");
                }
                return;
            }

            if (survivor == null)
            {
                Debug.LogError($"[SurvivorInteractable] {gameObject.name} cannot rescue - Survivor component is null!");
                return;
            }

            // Check if survivor is already rescued
            if (survivor.IsRescued())
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[SurvivorInteractable] {gameObject.name} is already rescued!");
                }
                return;
            }

            // Check if survivor is active (player shouldn't be able to rescue if not active yet)
            if (!survivor.IsActive())
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[SurvivorInteractable] {gameObject.name} is not active yet, cannot rescue");
                }
                return;
            }

            if (debugMode)
            {
                Debug.Log($"[SurvivorInteractable] Player rescuing {gameObject.name}!");
            }

            // Play rescue sound
            if (rescueSound != null && m_AudioSource != null)
            {
                m_AudioSource.PlayOneShot(rescueSound);
            }

            // Trigger rescue
            survivor.Rescue();

            // Disable this component to prevent re-interaction
            if (disableAfterRescue)
            {
                enabled = false;

                if (debugMode)
                {
                    Debug.Log($"[SurvivorInteractable] {gameObject.name} disabled after rescue");
                }
            }
        }

        public Transform GetTransform()
        {
            return transform;
        }

        public string GetInteractionPrompt()
        {
            // Don't show prompt if this component is disabled
            if (!enabled)
                return null;

            // Only show prompt if survivor is active and not rescued
            if (survivor == null)
                return null;

            if (!survivor.IsActive() || survivor.IsRescued())
                return null;

            return rescuePrompt;
        }
    }
}
