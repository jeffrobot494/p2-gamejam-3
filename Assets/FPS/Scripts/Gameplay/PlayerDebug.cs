using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.FPS.Gameplay
{
    [RequireComponent(typeof(PlayerCharacterController))]
    public class PlayerDebug : MonoBehaviour
    {
        [Header("Debug Visualization")]
        [Tooltip("Show debug information in Scene view")]
        public bool ShowDebugInfo = true;

        [Tooltip("Show ground normal arrow")]
        public bool ShowGroundNormal = true;

        [Tooltip("Show gravity direction arrow")]
        public bool ShowGravityDirection = true;

        [Tooltip("Height above player to display debug text")]
        public float LabelHeight = 3f;

        [Header("Reset Options")]
        [Tooltip("Spawn point to reset player to")]
        public GameObject SpawnPoint;

        private PlayerCharacterController m_PlayerController;
        private CharacterController m_CharacterController;

        void Start()
        {
            m_PlayerController = GetComponent<PlayerCharacterController>();
            m_CharacterController = GetComponent<CharacterController>();
        }

        public void ResetToSpawnPoint()
        {
            if (SpawnPoint == null)
            {
                Debug.LogWarning("PlayerDebug: SpawnPoint not assigned!");
                return;
            }

            // Disable CharacterController to teleport
            if (m_CharacterController != null)
                m_CharacterController.enabled = false;

            // Set position and rotation to spawn point
            transform.position = SpawnPoint.transform.position;
            transform.rotation = SpawnPoint.transform.rotation;

            // Reset velocity
            if (m_PlayerController != null)
                m_PlayerController.CharacterVelocity = Vector3.zero;

            // Re-enable CharacterController
            if (m_CharacterController != null)
                m_CharacterController.enabled = true;

            Debug.Log("Player reset to spawn point");
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!Application.isPlaying || !ShowDebugInfo) return;
            if (m_PlayerController == null) return;

            // Draw debug label above player
            Vector3 labelPosition = transform.position + transform.up * LabelHeight;

            string debugText = $"Grounded: {m_PlayerController.IsGrounded}\n";
            debugText += $"Velocity: {m_PlayerController.CharacterVelocity.magnitude:F1} m/s\n";
            debugText += $"Crouching: {m_PlayerController.IsCrouching}";

            Handles.Label(labelPosition, debugText, new GUIStyle()
            {
                fontSize = 12,
                normal = new GUIStyleState()
                {
                    textColor = m_PlayerController.IsGrounded ? Color.green : Color.yellow
                },
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            });

            // Draw ground normal arrow when grounded
            if (ShowGroundNormal && m_PlayerController.IsGrounded)
            {
                Gizmos.color = Color.green;
                Vector3 start = transform.position;
                Vector3 end = start + transform.up * 2f;
                Gizmos.DrawLine(start, end);
                // Draw arrow head
                DrawArrowHead(end, transform.up, Color.green, 0.3f);
            }

            // Draw gravity direction arrow
            if (ShowGravityDirection)
            {
                PlayerCharacterController controller = GetComponent<PlayerCharacterController>();
                if (controller != null && controller.Asteroid != null)
                {
                    Vector3 gravityDir = (controller.Asteroid.transform.position - transform.position).normalized;
                    Gizmos.color = Color.red;
                    Vector3 start = transform.position;
                    Vector3 end = start + gravityDir * 1.5f;
                    Gizmos.DrawLine(start, end);
                    // Draw arrow head
                    DrawArrowHead(end, gravityDir, Color.red, 0.3f);
                }
            }
        }

        void DrawArrowHead(Vector3 tip, Vector3 direction, Color color, float size)
        {
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            if (right.magnitude < 0.1f)
                right = Vector3.Cross(direction, Vector3.forward).normalized;

            Vector3 up = Vector3.Cross(right, direction).normalized;

            Gizmos.color = color;
            Gizmos.DrawLine(tip, tip - direction * size + right * size * 0.5f);
            Gizmos.DrawLine(tip, tip - direction * size - right * size * 0.5f);
            Gizmos.DrawLine(tip, tip - direction * size + up * size * 0.5f);
            Gizmos.DrawLine(tip, tip - direction * size - up * size * 0.5f);
        }
#endif
    }
}
