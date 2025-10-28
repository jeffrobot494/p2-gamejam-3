using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Ladder component with dual entry/exit system
    /// Allows player to climb up or down with rhythmic motion
    /// </summary>
    public class Ladder : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("Layer mask for player detection")]
        public LayerMask PlayerLayer = -1;

        [Tooltip("How directly player must face ladder to enter (0.7 = ~45 degree cone)")]
        [Range(0f, 1f)]
        public float DotThresholdToEnter = 0.7f;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool DebugMode = false;

        [Tooltip("Show gizmos in editor")]
        public bool ShowGizmos = true;

        [Tooltip("Color for bottom trigger gizmo")]
        public Color BottomGizmoColor = Color.blue;

        [Tooltip("Color for top trigger gizmo")]
        public Color TopGizmoColor = Color.green;

        // State tracking
        private PlayerLadderController m_CurrentPlayer;
        private bool m_PlayerInBottomTrigger = false;
        private bool m_PlayerInTopTrigger = false;

        /// <summary>
        /// Called by LadderTrigger when player enters a trigger
        /// </summary>
        public void OnTriggerEntered(Collider other, bool isTopTrigger)
        {
            if (DebugMode)
            {
                Debug.Log($"[Ladder] OnTriggerEntered - Object: {other.gameObject.name}, IsTopTrigger: {isTopTrigger}");
            }

            // Check if it's the player
            if (!IsPlayerLayer(other.gameObject.layer))
            {
                if (DebugMode)
                {
                    Debug.Log($"[Ladder] Not player layer. Object layer: {other.gameObject.layer}");
                }
                return;
            }

            // Get player ladder controller
            PlayerLadderController player = other.GetComponent<PlayerLadderController>();
            if (player == null)
            {
                if (DebugMode)
                {
                    Debug.LogWarning($"[Ladder] No PlayerLadderController found on {other.gameObject.name}");
                }
                return;
            }

            if (DebugMode)
            {
                Debug.Log($"[Ladder] Player detected in {(isTopTrigger ? "TOP" : "BOTTOM")} trigger");
            }

            // Track which trigger player is in
            if (isTopTrigger)
            {
                m_PlayerInTopTrigger = true;
            }
            else
            {
                m_PlayerInBottomTrigger = true;

                // Special case: If climbing down and entering bottom trigger = reached the bottom, exit immediately
                if (player.IsOnLadder && player.CurrentLadder == this && player.ClimbDirection < 0)
                {
                    if (DebugMode)
                    {
                        Debug.Log("[Ladder] Player climbing DOWN entered BOTTOM trigger - exiting ladder (reached bottom)");
                    }
                    player.ExitLadder();
                    return;
                }
            }

            // If player is not on any ladder, check if they can enter
            if (!player.IsOnLadder)
            {
                m_CurrentPlayer = player;
                // We'll check for entry in Update when we can check input
            }
        }

        /// <summary>
        /// Called by LadderTrigger when player exits a trigger
        /// </summary>
        public void OnTriggerExited(Collider other, bool isTopTrigger)
        {
            if (!IsPlayerLayer(other.gameObject.layer))
                return;

            PlayerLadderController player = other.GetComponent<PlayerLadderController>();
            if (player == null)
                return;

            // Track which trigger player left
            if (isTopTrigger)
            {
                m_PlayerInTopTrigger = false;

                // Only exit if climbing UP and leaving the TOP trigger (reached destination)
                if (player.IsOnLadder && player.CurrentLadder == this && player.ClimbDirection > 0)
                {
                    if (DebugMode)
                    {
                        Debug.Log("[Ladder] Player climbing UP left TOP trigger - exiting ladder (reached top)");
                    }
                    player.ExitLadder();
                }
            }
            else
            {
                m_PlayerInBottomTrigger = false;

                // Only exit if climbing DOWN and leaving the BOTTOM trigger (reached destination)
                if (player.IsOnLadder && player.CurrentLadder == this && player.ClimbDirection < 0)
                {
                    if (DebugMode)
                    {
                        Debug.Log("[Ladder] Player climbing DOWN left BOTTOM trigger - exiting ladder (reached bottom)");
                    }
                    player.ExitLadder();
                }
            }

            // Clear current player reference if they left
            if (player == m_CurrentPlayer && !m_PlayerInBottomTrigger && !m_PlayerInTopTrigger)
            {
                m_CurrentPlayer = null;
            }
        }

        void Update()
        {
            // Check if player can enter ladder
            if (m_CurrentPlayer != null && !m_CurrentPlayer.IsOnLadder)
            {
                TryEnterLadder();
            }
        }

        /// <summary>
        /// Attempts to enter ladder based on player input and facing direction
        /// </summary>
        void TryEnterLadder()
        {
            if (m_CurrentPlayer == null)
                return;

            // Get player input
            PlayerInputHandler inputHandler = m_CurrentPlayer.GetComponent<PlayerInputHandler>();
            if (inputHandler == null)
            {
                if (DebugMode)
                {
                    Debug.LogWarning("[Ladder] PlayerInputHandler not found!");
                }
                return;
            }

            Vector3 moveInput = inputHandler.GetMoveInput();

            // Check if player is pressing forward or backward
            bool pressingForward = moveInput.z > 0.1f;
            bool pressingBackward = moveInput.z < -0.1f;

            if (DebugMode && (pressingForward || pressingBackward))
            {
                Debug.Log($"[Ladder] Input detected - Forward: {pressingForward}, Backward: {pressingBackward}");
            }

            if (!pressingForward && !pressingBackward)
                return;

            // Check if player is facing the ladder
            Vector3 playerForward = m_CurrentPlayer.transform.forward;
            Vector3 ladderForward = transform.forward;
            float dot = Vector3.Dot(playerForward, ladderForward);

            if (DebugMode)
            {
                Debug.Log($"[Ladder] Facing check:");
                Debug.Log($"  Player Forward: {playerForward}");
                Debug.Log($"  Ladder Forward: {ladderForward}");
                Debug.Log($"  Dot product: {dot}");
                Debug.Log($"  Abs(Dot): {Mathf.Abs(dot)}");
                Debug.Log($"  Threshold: {DotThresholdToEnter}");
            }

            // Player must face ladder (either direction)
            bool facingLadder = Mathf.Abs(dot) > DotThresholdToEnter;

            if (!facingLadder)
            {
                if (DebugMode)
                {
                    Debug.Log("[Ladder] Player not facing ladder!");
                }
                return;
            }

            // Determine entry based on which trigger player is in and input
            if (m_PlayerInBottomTrigger && pressingForward)
            {
                if (DebugMode)
                {
                    Debug.Log("[Ladder] ENTERING ladder from BOTTOM - climbing UP");
                }
                // Enter from bottom, climb up
                m_CurrentPlayer.EnterLadder(this, 1); // 1 = climbing up
            }
            else if (m_PlayerInTopTrigger && pressingBackward)
            {
                if (DebugMode)
                {
                    Debug.Log("[Ladder] ENTERING ladder from TOP - climbing DOWN");
                }
                // Enter from top, climb down
                m_CurrentPlayer.EnterLadder(this, -1); // -1 = climbing down
            }
        }

        /// <summary>
        /// Checks if a layer matches the player layer mask
        /// </summary>
        bool IsPlayerLayer(int layer)
        {
            return ((1 << layer) & PlayerLayer) != 0;
        }

        void OnDrawGizmos()
        {
            if (!ShowGizmos)
                return;

            // Draw facing direction from ladder center
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * 1f);

            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "LADDER");
            #endif
        }
    }
}
