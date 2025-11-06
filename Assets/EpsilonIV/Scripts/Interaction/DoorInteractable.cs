using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Makes a door interactable
    /// Checks all IDoorUnlocker components to determine if player can open the door
    /// </summary>
    [RequireComponent(typeof(Door))]
    public class DoorInteractable : MonoBehaviour, IInteractable
    {
        [Header("Interaction")]
        [Tooltip("Text shown when door is closed and unlocked")]
        public string OpenPrompt = "[E] OPEN DOOR";

        [Tooltip("Text shown when door is open")]
        public string ClosePrompt = "[E] CLOSE DOOR";

        [Tooltip("Text shown when door is locked")]
        public string LockedPrompt = "[E] LOCKED";

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool DebugMode = false;

        private Door m_Door;
        private IDoorUnlocker[] m_Unlockers;

        void Start()
        {
            m_Door = GetComponent<Door>();
            if (m_Door == null)
            {
                Debug.LogError($"[DoorInteractable] No Door component found on {gameObject.name}!");
            }

            // Get all unlocker components (optional - door can be unlocked by default)
            m_Unlockers = GetComponents<IDoorUnlocker>();
        }

        public void Interact()
        {
            if (m_Door == null)
                return;

            // If door is already open, close it
            if (m_Door.IsOpen)
            {
                m_Door.Close();
                return;
            }

            // If door is unlocked, just open it
            if (!m_Door.IsLocked)
            {
                m_Door.Open();
                return;
            }

            // Door is locked - check if any unlocker can unlock it
            GameObject player = FindFirstObjectByType<PlayerCharacterController>()?.gameObject;
            if (player == null)
            {
                Debug.LogWarning("[DoorInteractable] Could not find player!");
                return;
            }

            bool canUnlock = false;

            foreach (IDoorUnlocker unlocker in m_Unlockers)
            {
                // Let unlocker handle the attempt (may show UI, play sounds, etc.)
                unlocker.OnUnlockAttempt(player, m_Door);

                // Check if this unlocker can unlock
                if (unlocker.CanUnlock(player))
                {
                    canUnlock = true;

                    if (DebugMode)
                    {
                        Debug.Log($"[DoorInteractable] {unlocker.GetType().Name} can unlock door");
                    }
                }
            }

            // If any unlocker succeeded, unlock and open the door
            if (canUnlock)
            {
                m_Door.Unlock();
                m_Door.Open();

                if (DebugMode)
                {
                    Debug.Log($"[DoorInteractable] Door unlocked and opened");
                }
            }
            else
            {
                if (DebugMode)
                {
                    Debug.Log($"[DoorInteractable] No unlocker could unlock the door");
                }
            }
        }

        public Transform GetTransform()
        {
            return transform;
        }

        public string GetInteractionPrompt()
        {
            if (m_Door == null)
                return "";

            // Show different prompt based on door state
            if (m_Door.IsOpen)
            {
                return ClosePrompt;
            }
            else if (m_Door.IsLocked)
            {
                return LockedPrompt;
            }
            else
            {
                return OpenPrompt;
            }
        }
    }
}
