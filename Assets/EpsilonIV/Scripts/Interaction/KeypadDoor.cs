using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Door unlocker that checks if a KeypadComputer is unlocked
    /// Player must enter correct code on keypad before door will open
    /// </summary>
    public class KeypadDoor : MonoBehaviour, IDoorUnlocker
    {
        [Header("Required Keypad")]
        [Tooltip("The KeypadComputer that controls this door")]
        public KeypadComputer RequiredKeypad;

        [Header("Auto Re-lock")]
        [Tooltip("Automatically re-lock keypad when door closes (only if keypad is not set to stay unlocked permanently)")]
        public bool RelockOnDoorClose = true;

        [Header("Audio")]
        [Tooltip("Sound played when player tries to open locked door")]
        public AudioClip DeniedSound;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool DebugMode = false;

        private AudioSource m_AudioSource;
        private Door m_Door;

        void Start()
        {
            // Get door component
            m_Door = GetComponent<Door>();
            if (m_Door == null)
            {
                Debug.LogError($"[KeypadDoor] No Door component found on {gameObject.name}!");
            }

            // Setup audio source
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null && DeniedSound != null)
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
                m_AudioSource.playOnAwake = false;
                m_AudioSource.spatialBlend = 1f; // 3D sound
            }

            // Subscribe to door events if auto re-lock is enabled
            if (RelockOnDoorClose && m_Door != null)
            {
                m_Door.OnDoorClosed.AddListener(OnDoorClosed);
            }

            // Validate keypad reference
            if (RequiredKeypad == null)
            {
                Debug.LogError($"[KeypadDoor] No KeypadComputer assigned to {gameObject.name}!");
            }
        }

        void OnDestroy()
        {
            // Unsubscribe from door events
            if (m_Door != null)
            {
                m_Door.OnDoorClosed.RemoveListener(OnDoorClosed);
            }
        }

        public bool CanUnlock(GameObject player)
        {
            if (RequiredKeypad == null)
            {
                if (DebugMode)
                {
                    Debug.LogWarning("[KeypadDoor] No keypad assigned, cannot unlock");
                }
                return false;
            }

            bool isUnlocked = RequiredKeypad.IsUnlocked;

            if (DebugMode)
            {
                Debug.Log($"[KeypadDoor] Keypad unlock status: {isUnlocked}");
            }

            return isUnlocked;
        }

        public void OnUnlockAttempt(GameObject player, Door door)
        {
            // If keypad is not unlocked, play denied sound
            if (!CanUnlock(player))
            {
                PlaySound(DeniedSound);

                if (DebugMode)
                {
                    Debug.Log($"[KeypadDoor] Access denied - keypad not unlocked");
                }
            }
            else
            {
                if (DebugMode)
                {
                    Debug.Log($"[KeypadDoor] Access granted - keypad unlocked");
                }
            }
        }

        /// <summary>
        /// Called when the door closes
        /// Re-locks the keypad if configured to do so
        /// </summary>
        void OnDoorClosed()
        {
            if (RequiredKeypad != null && RelockOnDoorClose && !RequiredKeypad.StayUnlockedPermanently)
            {
                RequiredKeypad.ResetUnlock();

                if (DebugMode)
                {
                    Debug.Log($"[KeypadDoor] Door closed, keypad re-locked");
                }
            }
        }

        void PlaySound(AudioClip clip)
        {
            if (clip != null && m_AudioSource != null)
            {
                m_AudioSource.PlayOneShot(clip);
            }
        }
    }
}
