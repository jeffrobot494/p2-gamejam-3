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

            // Get detailed velocity breakdown
            Vector3 velocity = m_PlayerController.CharacterVelocity;
            PlayerCharacterController controller = m_PlayerController;

            // Calculate velocity components relative to gravity
            Vector3 gravityDir = Vector3.zero;
            Vector3 upDir = Vector3.zero;
            float verticalVel = 0f;
            float horizontalVel = 0f;

            if (controller.Asteroid != null)
            {
                gravityDir = (controller.Asteroid.transform.position - transform.position).normalized;
                upDir = -gravityDir;
                verticalVel = Vector3.Dot(velocity, upDir);
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(velocity, upDir);
                horizontalVel = horizontalVelocity.magnitude;
            }

            // Draw debug info in top-left corner
            Handles.BeginGUI();

            string debugText = "=== MOVEMENT ===\n";
            debugText += $"Grounded: {m_PlayerController.IsGrounded}\n";
            debugText += $"Crouching: {m_PlayerController.IsCrouching}\n";
            debugText += $"HasJumped: {m_PlayerController.HasJumpedThisFrame}\n";
            debugText += "\n=== VELOCITY ===\n";
            debugText += $"Total: {velocity.magnitude:F2} m/s\n";
            debugText += $"Horizontal: {horizontalVel:F2} m/s\n";
            debugText += $"Vertical: {verticalVel:F2} m/s\n";
            debugText += $"X: {velocity.x:F2} Y: {velocity.y:F2} Z: {velocity.z:F2}";

            GUIStyle labelStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = m_PlayerController.IsGrounded ? Color.green : Color.yellow },
                padding = new RectOffset(10, 10, 10, 10)
            };

            GUI.Box(new Rect(10, 80, 250, 200), debugText, labelStyle);

            Handles.EndGUI();

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
            if (ShowGravityDirection && controller != null && controller.Asteroid != null)
            {
                Gizmos.color = Color.red;
                Vector3 start = transform.position;
                Vector3 end = start + gravityDir * 1.5f;
                Gizmos.DrawLine(start, end);
                // Draw arrow head
                DrawArrowHead(end, gravityDir, Color.red, 0.3f);
            }

            // Draw velocity vector
            Vector3 velocityVec = m_PlayerController.CharacterVelocity;
            if (velocityVec.magnitude > 0.1f)
            {
                Gizmos.color = Color.cyan;
                Vector3 start = transform.position + transform.up * 0.5f;
                Vector3 end = start + velocityVec.normalized * Mathf.Min(velocityVec.magnitude * 0.1f, 3f);
                Gizmos.DrawLine(start, end);
                DrawArrowHead(end, velocityVec.normalized, Color.cyan, 0.2f);

                // Draw velocity magnitude as a sphere
                Gizmos.color = new Color(0, 1, 1, 0.3f);
                Gizmos.DrawSphere(end, 0.1f);
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
