using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Handles player interaction with objects implementing IInteractable
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [Tooltip("Maximum distance the player can interact with objects")]
        public float InteractionRange = 3f;

        [Tooltip("LayerMask for interactable objects")]
        public LayerMask InteractionLayer = -1;

        [Header("References")]
        [Tooltip("Camera to raycast from (will auto-find if not set)")]
        public Camera PlayerCamera;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool DebugMode = true;

        [Tooltip("Show raycast visualization in Scene view")]
        public bool ShowDebugRay = true;

        private IInteractable m_CurrentInteractable;
        private PlayerInputHandler m_InputHandler;
        private RaycastHit m_LastHit;
        private bool m_LastRayHit;

        void Start()
        {
            // Get references
            m_InputHandler = GetComponent<PlayerInputHandler>();
            if (m_InputHandler == null)
            {
                Debug.LogError("PlayerInteraction: PlayerInputHandler not found on " + gameObject.name);
            }

            // Auto-find camera if not set
            if (PlayerCamera == null)
            {
                PlayerCharacterController pcc = GetComponent<PlayerCharacterController>();
                if (pcc != null)
                {
                    PlayerCamera = pcc.PlayerCamera;
                }
            }

            if (PlayerCamera == null)
            {
                Debug.LogError("PlayerInteraction: PlayerCamera not assigned or found on " + gameObject.name);
            }
        }

        void Update()
        {
            CheckForInteractable();
            HandleInteractionInput();
        }

        void CheckForInteractable()
        {
            // Raycast from camera center
            Ray ray = new Ray(PlayerCamera.transform.position, PlayerCamera.transform.forward);
            RaycastHit hit;

            m_LastRayHit = Physics.Raycast(ray, out hit, InteractionRange, InteractionLayer);

            if (m_LastRayHit)
            {
                m_LastHit = hit;

                // Try to get IInteractable from hit object
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                // Skip if the component is disabled
                if (interactable != null && interactable is MonoBehaviour mb && !mb.enabled)
                {
                    interactable = null;
                }

                // Skip if the interactable returns null prompt (means not interactable right now)
                if (interactable != null && interactable.GetInteractionPrompt() == null)
                {
                    interactable = null;
                }

                if (interactable != null)
                {
                    // New interactable detected
                    if (m_CurrentInteractable != interactable)
                    {
                        m_CurrentInteractable = interactable;

                        if (DebugMode)
                        {
                            Debug.Log("Looking at interactable: " + hit.collider.gameObject.name);
                        }
                    }
                }
                else
                {
                    // Hit something but it's not interactable
                    if (m_CurrentInteractable != null && DebugMode)
                    {
                        Debug.Log("No longer looking at interactable");
                    }
                    m_CurrentInteractable = null;
                }
            }
            else
            {
                // Not looking at anything
                if (m_CurrentInteractable != null && DebugMode)
                {
                    Debug.Log("No longer looking at interactable");
                }
                m_CurrentInteractable = null;
            }
        }

        void HandleInteractionInput()
        {
            // Check for interaction input using the new Input System
            if (m_CurrentInteractable != null && m_InputHandler.GetInteractInputDown())
            {
                if (DebugMode)
                {
                    Debug.Log("Interacting with: " + m_CurrentInteractable.GetTransform().gameObject.name);
                }

                m_CurrentInteractable.Interact();
            }
        }

        /// <summary>
        /// Gets the currently targeted interactable (null if none)
        /// </summary>
        public IInteractable GetCurrentInteractable()
        {
            return m_CurrentInteractable;
        }

        void OnDrawGizmos()
        {
            if (!ShowDebugRay || PlayerCamera == null)
                return;

            Vector3 rayStart = PlayerCamera.transform.position;
            Vector3 rayDirection = PlayerCamera.transform.forward;

            if (m_LastRayHit)
            {
                // Hit something - draw line to hit point
                Gizmos.color = m_CurrentInteractable != null ? Color.green : Color.yellow;
                Gizmos.DrawLine(rayStart, m_LastHit.point);

                // Draw sphere at hit point
                if (m_CurrentInteractable != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(m_LastHit.point, 0.1f);
                }
                else
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(m_LastHit.point, 0.05f);
                }
            }
            else
            {
                // Nothing hit - draw full range line
                Gizmos.color = Color.red;
                Gizmos.DrawLine(rayStart, rayStart + rayDirection * InteractionRange);
            }
        }
    }
}
