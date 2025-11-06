using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Door unlocker that requires player to be holding a specific keycard
    /// Player must have the keycard in hand when pressing E on the door
    /// </summary>
    public class KeyCardDoor : MonoBehaviour, IDoorUnlocker
    {
        [Header("Required Keycard")]
        [Tooltip("ID of the keycard required to unlock this door")]
        public string RequiredKeycardID = "DefaultKeycard";

        [Header("Audio")]
        [Tooltip("Sound played when player has correct keycard")]
        public AudioClip UnlockSound;

        [Tooltip("Sound played when player doesn't have correct keycard")]
        public AudioClip DeniedSound;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool DebugMode = false;

        private AudioSource m_AudioSource;

        void Start()
        {
            // Get or create audio source
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null && (UnlockSound != null || DeniedSound != null))
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
                m_AudioSource.playOnAwake = false;
                m_AudioSource.spatialBlend = 1f; // 3D sound
            }
        }

        public bool CanUnlock(GameObject player)
        {
            if (player == null)
                return false;

            // Find the PlayerThrowController to check what they're holding
            PlayerThrowController throwController = player.GetComponent<PlayerThrowController>();
            if (throwController == null)
            {
                if (DebugMode)
                {
                    Debug.LogWarning("[KeyCardDoor] No PlayerThrowController found on player");
                }
                return false;
            }

            // Check if player is holding a keycard with the required ID
            KeyCard heldKeycard = throwController.GetHeldKeyCard();
            if (heldKeycard != null)
            {
                if (heldKeycard.GetKeycardID() == RequiredKeycardID)
                {
                    if (DebugMode)
                    {
                        Debug.Log($"[KeyCardDoor] Player is holding correct keycard: {RequiredKeycardID}");
                    }
                    return true;
                }
                else
                {
                    if (DebugMode)
                    {
                        Debug.Log($"[KeyCardDoor] Player holding wrong keycard. Required: {RequiredKeycardID}, Held: {heldKeycard.GetKeycardID()}");
                    }
                }
            }
            else
            {
                if (DebugMode)
                {
                    Debug.Log($"[KeyCardDoor] Player not holding any keycard");
                }
            }

            return false;
        }

        public void OnUnlockAttempt(GameObject player, Door door)
        {
            // Play appropriate sound based on whether player has correct keycard
            if (CanUnlock(player))
            {
                PlaySound(UnlockSound);
            }
            else
            {
                PlaySound(DeniedSound);
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
