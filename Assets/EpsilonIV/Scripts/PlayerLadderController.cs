using UnityEngine;

namespace Unity.FPS.Gameplay
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
        public void EnterLadder(Ladder ladder, int climbDirection)
        {
            if (IsOnLadder)
            {
                if (DebugMode)
                {
                    Debug.LogWarning("[PlayerLadderController] Already on a ladder!");
                }
                return;
            }

            IsOnLadder = true;
            CurrentLadder = ladder;
            ClimbDirection = climbDirection;
            //m_ClimbCycleTime = 0f; // DISABLED - rhythmic climbing

            // Reset velocity
            m_PlayerController.CharacterVelocity = Vector3.zero;

            // DISABLED FOR SIMPLICITY - Animation
            //// Set animation
            //if (PlayerAnimator != null)
            //{
            //    PlayerAnimator.SetBool(IsClimbingParam, true);
            //}

            if (DebugMode)
            {
                Debug.Log($"[PlayerLadderController] Entered ladder. Direction: {(ClimbDirection > 0 ? "UP" : "DOWN")}");
            }
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
                Vector3 pushDirection = -exitedLadder.transform.forward;
                m_PlayerController.CharacterVelocity = pushDirection * ExitPushForce;
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
        void HandleLadderClimbing()
        {
            // Get input
            Vector3 moveInput = m_InputHandler.GetMoveInput();

            // Forward/backward input controls vertical movement (climbing)
            float verticalInput = moveInput.z;

            // Update climb direction based on input
            if (verticalInput > 0.1f)
            {
                ClimbDirection = 1; // Climbing up
            }
            else if (verticalInput < -0.1f)
            {
                ClimbDirection = -1; // Climbing down
            }

            // SIMPLIFIED - Constant climb speed (no rhythmic motion)
            float verticalSpeed = verticalInput * ClimbSpeed;
            Vector3 climbVelocity = Vector3.up * verticalSpeed;

            // Add slight horizontal movement (left/right adjustments)
            float horizontalInput = moveInput.x;
            Vector3 horizontalVelocity = transform.right * horizontalInput * LadderHorizontalSpeed;

            // Combine velocities
            Vector3 finalVelocity = climbVelocity + horizontalVelocity;

            // Apply movement
            m_CharacterController.Move(finalVelocity * Time.deltaTime);

            // DISABLED FOR SIMPLICITY - Animation updates
            //// Update animation
            //if (PlayerAnimator != null)
            //{
            //    float animSpeed = Mathf.Abs(verticalInput);
            //    PlayerAnimator.SetFloat(ClimbSpeedParam, animSpeed);
            //}
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
