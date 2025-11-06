using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Handles ladder climbing mechanics for the player
    /// Includes rhythmic climbing motion and animation triggers
    /// </summary>
    [RequireComponent(typeof(PlayerCharacterController))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerLadderController : MonoBehaviour
    {
        [Header("Climbing Parameters")]
        [Tooltip("Base climbing speed multiplier")]
        public float ClimbSpeed = 3f;

        [Tooltip("Horizontal movement speed while on ladder")]
        public float LadderHorizontalSpeed = 1f;

        // DISABLED FOR SIMPLICITY - Rhythmic climbing motion
        //[Tooltip("Duration of one climb cycle (one rung) in seconds")]
        //public float ClimbCycleDuration = 0.8f;
        //[Tooltip("Curve defining rhythmic climbing motion (0-1 = slow to fast pull)")]
        //public AnimationCurve ClimbSpeedCurve = AnimationCurve.EaseInOut(0f, 0.2f, 1f, 1f);

        [Tooltip("Small push away from ladder when exiting via jump")]
        public float ExitPushForce = 2f;

        // DISABLED FOR SIMPLICITY - Animation
        //[Header("Animation")]
        //[Tooltip("Animator component (optional)")]
        //public Animator PlayerAnimator;
        //[Tooltip("Name of the IsClimbing boolean parameter in animator")]
        //public string IsClimbingParam = "IsClimbing";
        //[Tooltip("Name of the ClimbSpeed float parameter in animator")]
        //public string ClimbSpeedParam = "ClimbSpeed";

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool DebugMode = false;

        // State
        public bool IsOnLadder { get; private set; }
        public Ladder CurrentLadder { get; private set; }
        public int ClimbDirection { get; private set; } // 1 = up, -1 = down

        // DISABLED FOR SIMPLICITY - Rhythmic climbing
        //private float m_ClimbCycleTime = 0f;

        // Component references
        private PlayerCharacterController m_PlayerController;
        private CharacterController m_CharacterController;
        private PlayerInputHandler m_InputHandler;

        void Start()
        {
            // Get required components
            m_PlayerController = GetComponent<PlayerCharacterController>();
            m_CharacterController = GetComponent<CharacterController>();
            m_InputHandler = GetComponent<PlayerInputHandler>();

            // Validate
            if (m_PlayerController == null)
            {
                Debug.LogError("[PlayerLadderController] PlayerCharacterController not found!");
            }

            if (m_CharacterController == null)
            {
                Debug.LogError("[PlayerLadderController] CharacterController not found!");
            }

            if (m_InputHandler == null)
            {
                Debug.LogError("[PlayerLadderController] PlayerInputHandler not found!");
            }

            // DISABLED FOR SIMPLICITY - Animation and rhythmic curve
            //// Auto-find animator if not assigned
            //if (PlayerAnimator == null)
            //{
            //    PlayerAnimator = GetComponentInChildren<Animator>();
            //}
            //
            //// Initialize curve with default if not set
            //if (ClimbSpeedCurve == null || ClimbSpeedCurve.length == 0)
            //{
            //    ClimbSpeedCurve = new AnimationCurve(
            //        new Keyframe(0f, 1f),      // Fast pull start
            //        new Keyframe(0.25f, 1f),   // Still pulling
            //        new Keyframe(0.5f, 0.2f),  // Slow reach
            //        new Keyframe(0.75f, 0.2f), // Still reaching
            //        new Keyframe(1f, 1f)       // Back to fast pull
            //    );
            //}
        }

        void Update()
        {
            if (IsOnLadder)
            {
                HandleLadderClimbing();
                CheckForLadderExit();
            }
        }

        /// <summary>
        /// Called by Ladder component to enter ladder mode
        /// </summary>
        public void EnterLadder(Ladder ladder)
        {
            if (IsOnLadder) return;

            IsOnLadder = true;
            CurrentLadder = ladder;
            m_PlayerController.CharacterVelocity = Vector3.zero;

            if (DebugMode)
                Debug.Log($"[PlayerLadderController] Entered ladder: {ladder.name}");
        }

        /// <summary>
        /// Exits ladder mode and restores normal movement
        /// </summary>
        public void ExitLadder(bool withPush = false)
        {
            if (!IsOnLadder)
                return;

            IsOnLadder = false;
            Ladder exitedLadder = CurrentLadder;
            CurrentLadder = null;
            //m_ClimbCycleTime = 0f; // DISABLED - rhythmic climbing

            // Apply small push away from ladder if jumping off
            if (withPush && exitedLadder != null)
            {
                Vector3 pushDir = -CurrentLadder.transform.forward + Vector3.up * 0.5f;
                m_PlayerController.CharacterVelocity = pushDir.normalized * ExitPushForce;
            }

            // DISABLED FOR SIMPLICITY - Animation
            //// Clear animation
            //if (PlayerAnimator != null)
            //{
            //    PlayerAnimator.SetBool(IsClimbingParam, false);
            //    PlayerAnimator.SetFloat(ClimbSpeedParam, 0f);
            //}

            if (DebugMode)
            {
                Debug.Log("[PlayerLadderController] Exited ladder");
            }
        }

        /// <summary>
        /// Handles climbing movement (SIMPLIFIED - constant speed)
        /// </summary>
        private void HandleLadderClimbing()
        {
            Vector3 input = m_InputHandler.GetMoveInput();
            float verticalInput = input.z;
            float horizontalInput = input.x;

            // Smooth climbing motion
            Vector3 climbVelocity = Vector3.up * verticalInput * ClimbSpeed;
            Vector3 horizontalVelocity = transform.right * horizontalInput * 2;
            Vector3 finalVelocity = climbVelocity + horizontalVelocity;

            // Move the player
            m_CharacterController.Move(finalVelocity * Time.deltaTime);

            // --- Smooth top/bottom auto exit ---
            if (CurrentLadder)
            {
                float topY = CurrentLadder.TopY;
                float bottomY = CurrentLadder.BottomY;
                float playerFeetY = transform.position.y;
                float playerHeadY = playerFeetY + m_CharacterController.height;

                // 🟩 Exit slightly *before* colliding with the floor
                if (playerHeadY > topY - 0.1f && verticalInput > 0f)
                {
                    ExitLadder(withPush: true);
                    return;
                }

                // 🟦 Exit slightly before dropping below the ladder bottom
                if (playerFeetY < bottomY + 0.1f && verticalInput < 0f)
                {
                    ExitLadder();
                    return;
                }
            }
        }


        /// <summary>
        /// Checks for player input to exit ladder
        /// </summary>
        void CheckForLadderExit()
        {
            // Exit on jump input
            if (m_InputHandler.GetJumpInputDown())
            {
                ExitLadder(withPush: true);
            }

            // Exit on crouch input (alternative exit)
            if (m_InputHandler.GetCrouchInputDown())
            {
                ExitLadder(withPush: true);
            }
        }

        /// <summary>
        /// Gets current climbing velocity (for external systems)
        /// </summary>
        public Vector3 GetClimbVelocity()
        {
            if (!IsOnLadder)
                return Vector3.zero;

            Vector3 moveInput = m_InputHandler.GetMoveInput();
            float verticalInput = moveInput.z;

            // SIMPLIFIED - Constant speed
            return Vector3.up * verticalInput * ClimbSpeed;
        }
    }
}
