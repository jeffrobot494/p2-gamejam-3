using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Defines a respawn point for a room
    /// Automatically registers with CheckpointManager
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RoomCheckpoint : MonoBehaviour
    {
        [Header("Checkpoint Settings")]
        [Tooltip("Transform that defines respawn position/rotation (uses this transform if null)")]
        public Transform RespawnPoint;

        [Tooltip("Set this checkpoint as default on game start")]
        public bool SetAsDefaultOnStart = false;

        [Header("Detection")]
        [Tooltip("Layer mask for player detection")]
        public LayerMask PlayerLayer = -1;

        [Header("Debug Visualization")]
        [Tooltip("Show respawn point gizmo in editor")]
        public bool ShowGizmo = true;

        [Tooltip("Color of the gizmo")]
        public Color GizmoColor = Color.green;

        [Tooltip("Size of the gizmo sphere")]
        public float GizmoSize = 0.5f;

        // State
        private bool m_IsCurrentCheckpoint = false;
        private Collider m_Trigger;

        /// <summary>
        /// Gets whether this is the currently active checkpoint
        /// </summary>
        public bool IsCurrentCheckpoint => m_IsCurrentCheckpoint;

        void Start()
        {
            // Get or create trigger collider
            m_Trigger = GetComponent<Collider>();
            if (m_Trigger == null)
            {
                Debug.LogError($"[RoomCheckpoint] {gameObject.name} needs a Collider component!");
                return;
            }

            // Ensure trigger is enabled
            if (!m_Trigger.isTrigger)
            {
                Debug.LogWarning($"[RoomCheckpoint] {gameObject.name} collider should be set as trigger! Auto-fixing.");
                m_Trigger.isTrigger = true;
            }

            // Use this transform as respawn point if none specified
            if (RespawnPoint == null)
            {
                RespawnPoint = transform;
            }

            // Register with CheckpointManager
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.RegisterCheckpoint(this);
            }
            else
            {
                Debug.LogWarning($"[RoomCheckpoint] No CheckpointManager found in scene for {gameObject.name}!");
            }
        }

        void OnDestroy()
        {
            // Unregister from CheckpointManager
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.UnregisterCheckpoint(this);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            // Check if player entered
            if (IsPlayerLayer(other.gameObject.layer))
            {
                ActivateCheckpoint();
            }
        }

        /// <summary>
        /// Activates this checkpoint as the current respawn point
        /// </summary>
        public void ActivateCheckpoint()
        {
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.SetCurrentCheckpoint(this);
            }
        }

        /// <summary>
        /// Called by CheckpointManager to mark this as active/inactive
        /// </summary>
        public void SetActive(bool active)
        {
            m_IsCurrentCheckpoint = active;
        }

        /// <summary>
        /// Gets the respawn position
        /// </summary>
        public Vector3 GetRespawnPosition()
        {
            return RespawnPoint != null ? RespawnPoint.position : transform.position;
        }

        /// <summary>
        /// Gets the respawn rotation
        /// </summary>
        public Quaternion GetRespawnRotation()
        {
            return RespawnPoint != null ? RespawnPoint.rotation : transform.rotation;
        }

        /// <summary>
        /// Checks if a layer matches the player layer mask
        /// </summary>
        private bool IsPlayerLayer(int layer)
        {
            return ((1 << layer) & PlayerLayer) != 0;
        }

        void OnDrawGizmos()
        {
            if (!ShowGizmo)
                return;

            // Determine gizmo color based on state
            Color color = m_IsCurrentCheckpoint ? Color.cyan : GizmoColor;
            Gizmos.color = color;

            // Draw sphere at respawn point
            Vector3 position = RespawnPoint != null ? RespawnPoint.position : transform.position;
            Gizmos.DrawWireSphere(position, GizmoSize);

            // Draw direction arrow
            if (RespawnPoint != null)
            {
                Vector3 forward = RespawnPoint.forward;
                Gizmos.DrawRay(position, forward * (GizmoSize * 2f));
            }
            else
            {
                Vector3 forward = transform.forward;
                Gizmos.DrawRay(position, forward * (GizmoSize * 2f));
            }

            // Draw label in editor
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(position + Vector3.up * (GizmoSize + 0.2f),
                m_IsCurrentCheckpoint ? "ACTIVE CHECKPOINT" : "Checkpoint");
            #endif
        }

        void OnDrawGizmosSelected()
        {
            if (!ShowGizmo)
                return;

            // Draw trigger bounds when selected
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Gizmos.matrix = transform.localToWorldMatrix;

                if (col is BoxCollider box)
                {
                    Gizmos.DrawCube(box.center, box.size);
                }
                else if (col is SphereCollider sphere)
                {
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                }
            }
        }
    }
}
