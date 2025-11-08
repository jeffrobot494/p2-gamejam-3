using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;

namespace EpsilonIV
{
    public enum MovementState
    {
        Idle,
        Walking,
        Running,
        CrouchWalking,
        InAir
    }

    [RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler))]
    public class PlayerCharacterController : MonoBehaviour
    {
        [Header("References")] [Tooltip("Reference to the main camera used for the player")]
        public Camera PlayerCamera;

        [Header("General")] [Tooltip("Force applied downward when in the air")]
        public float GravityDownForce = 20f;

        [Tooltip("Physic layers checked to consider the player grounded")]
        public LayerMask GroundCheckLayers = -1;

        [Tooltip("distance from the bottom of the character controller capsule to test for grounded")]
        public float GroundCheckDistance = 0.05f;

        [Header("Movement")] [Tooltip("Max movement speed when grounded (when not sprinting)")]
        public float MaxSpeedOnGround = 10f;

        [Tooltip(
            "Sharpness for the movement when grounded, a low value will make the player accelerate and decelerate slowly, a high value will do the opposite")]
        public float MovementSharpnessOnGround = 15;

        [Tooltip("Max movement speed when crouching")] [Range(0, 1)]
        public float MaxSpeedCrouchedRatio = 0.5f;

        [Tooltip("Max movement speed when not grounded")]
        public float MaxSpeedInAir = 10f;

        [Tooltip("Acceleration speed when in the air")]
        public float AccelerationSpeedInAir = 25f;

        [Tooltip("Multiplicator for the sprint speed (based on grounded speed)")]
        public float SprintSpeedModifier = 2f;

        [Tooltip("Height at which the player dies instantly when falling off the map")]
        public float KillHeight = -50f;

        [Header("Rotation")] [Tooltip("Rotation speed for moving the camera")]
        public float RotationSpeed = 200f;

        [Range(0.1f, 1f)] [Tooltip("Rotation speed multiplier when aiming")]
        public float AimingRotationMultiplier = 0.4f;

        [Header("Jump")] [Tooltip("Force applied upward when jumping")]
        public float JumpForce = 9f;

        [Header("Stance")] [Tooltip("Ratio (0-1) of the character height where the camera will be at")]
        public float CameraHeightRatio = 0.9f;

        [Tooltip("Height of character when standing")]
        public float CapsuleHeightStanding = 1.6f;

        [Tooltip("Height of character when crouching")]
        public float CapsuleHeightCrouching = 0.9f;

        [Tooltip("Speed of crouching transitions")]
        public float CrouchingSharpness = 10f;

        [Header("Fall Damage")]
        [Tooltip("Whether the player will recieve damage when hitting the ground at high speed")]
        public bool RecievesFallDamage;

        [Tooltip("Minimun fall speed for recieving fall damage")]
        public float MinSpeedForFallDamage = 10f;

        [Tooltip("Fall speed for recieving the maximum amount of fall damage")]
        public float MaxSpeedForFallDamage = 30f;

        [Tooltip("Damage recieved when falling at the mimimum speed")]
        public float FallDamageAtMinSpeed = 10f;

        [Tooltip("Damage recieved when falling at the maximum speed")]
        public float FallDamageAtMaxSpeed = 50f;

        public UnityAction<bool> OnStanceChanged;
        public UnityAction<MovementState> OnMovementStateChanged;
        public UnityAction OnJumped;
        public UnityAction OnLanded;
        public UnityAction OnFallDamage;

        public Vector3 CharacterVelocity { get; set; }
        public bool IsGrounded { get; private set; }
        public bool HasJumpedThisFrame { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsCrouching { get; private set; }

        public float RotationMultiplier => 1f;

        Health m_Health;
        PlayerInputHandler m_InputHandler;
        CharacterController m_Controller;
        PlayerLadderController m_LadderController;
        SprintStaminaManager m_StaminaManager;
        Vector3 m_GroundNormal;
        Vector3 m_CharacterVelocity;
        Vector3 m_LatestImpactSpeed;
        float m_LastTimeJumped = 0f;
        float m_CameraVerticalAngle = 0f;
        float m_TargetCharacterHeight;

        private MovementState m_CurrentMovementState = MovementState.Idle;
        private bool m_IsSprinting = false;

        const float k_JumpGroundingPreventionTime = 0.2f;
        const float k_GroundCheckDistanceInAir = 0.07f;

        void Awake() { }

        void Start()
        {
            m_Controller = GetComponent<CharacterController>();
            if (m_Controller == null)
                Debug.LogError($"Missing CharacterController component on {gameObject.name}");

            m_InputHandler = GetComponent<PlayerInputHandler>();
            if (m_InputHandler == null)
                Debug.LogError($"Missing PlayerInputHandler component on {gameObject.name}");

            m_Health = GetComponent<Health>();
            if (m_Health == null)
                Debug.LogError($"Missing Health component on {gameObject.name}");

            m_LadderController = GetComponent<PlayerLadderController>();
            m_StaminaManager = GetComponent<SprintStaminaManager>();

            m_Controller.enableOverlapRecovery = true;

            m_Health.OnDie += OnDie;

            SetCrouchingState(false, true);
            UpdateCharacterHeight(true);
        }

        void Update()
        {
            if (!IsDead && transform.position.y < KillHeight)
            {
                m_Health.Kill();
            }

            HasJumpedThisFrame = false;

            bool wasGrounded = IsGrounded;
            if (m_LadderController == null || !m_LadderController.IsOnLadder)
            {
                GroundCheck();
            }

            if (IsGrounded && !wasGrounded)
            {
                float fallSpeed = -Mathf.Min(CharacterVelocity.y, m_LatestImpactSpeed.y);
                float fallSpeedRatio = (fallSpeed - MinSpeedForFallDamage) /
                                       (MaxSpeedForFallDamage - MinSpeedForFallDamage);
                if (RecievesFallDamage && fallSpeedRatio > 0f)
                {
                    float dmgFromFall = Mathf.Lerp(FallDamageAtMinSpeed, FallDamageAtMaxSpeed, fallSpeedRatio);
                    m_Health.TakeDamage(dmgFromFall, null);
                    OnFallDamage?.Invoke();
                }
                else
                {
                    OnLanded?.Invoke();
                }
            }

            if (m_InputHandler.GetCrouchInputDown())
            {
                SetCrouchingState(!IsCrouching, false);
            }

            UpdateCharacterHeight(false);
            HandleCharacterMovement();
            UpdateMovementState();
        }

        void OnDie()
        {
            IsDead = true;
        }

        public void ResetDeathState()
        {
            IsDead = false;
        }

        void GroundCheck()
        {
            float chosenGroundCheckDistance =
                IsGrounded ? (m_Controller.skinWidth + GroundCheckDistance) : k_GroundCheckDistanceInAir;

            IsGrounded = false;
            m_GroundNormal = Vector3.up;

            if (Time.time >= m_LastTimeJumped + k_JumpGroundingPreventionTime)
            {
                if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(m_Controller.height),
                    m_Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, GroundCheckLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    m_GroundNormal = hit.normal;

                    if (Vector3.Dot(hit.normal, transform.up) > 0f &&
                        IsNormalUnderSlopeLimit(m_GroundNormal))
                    {
                        IsGrounded = true;

                        if (hit.distance > m_Controller.skinWidth)
                        {
                            m_Controller.Move(Vector3.down * hit.distance);
                        }
                    }
                }
            }
        }

        void HandleCharacterMovement()
        {
            if (m_LadderController != null && m_LadderController.IsOnLadder)
            {
                HandleCameraRotation();
                return;
            }

            HandleCameraRotation();

            bool isSprinting = m_InputHandler.GetSprintInputHeld();
            {
                if (isSprinting)
                {
                    Vector3 moveInput = m_InputHandler.GetMoveInput();
                    bool isMovingForward = moveInput.z > 0f;
                    if (!isMovingForward)
                    {
                        isSprinting = false;
                    }
                    else
                    {
                        isSprinting = SetCrouchingState(false, false);
                    }

                    if (isSprinting && m_StaminaManager != null)
                    {
                        if (!m_IsSprinting && !m_StaminaManager.CanSprint())
                        {
                            isSprinting = false;
                        }
                        else if (m_IsSprinting && m_StaminaManager.GetStaminaPercent() <= 0f)
                        {
                            isSprinting = false;
                        }
                    }
                }

                if (m_StaminaManager != null)
                {
                    if (isSprinting && !m_IsSprinting)
                        m_StaminaManager.StartSprinting();
                    else if (!isSprinting && m_IsSprinting)
                        m_StaminaManager.StopSprinting();
                }

                m_IsSprinting = isSprinting;
                float speedModifier = isSprinting ? SprintSpeedModifier : 1f;

                Vector3 worldspaceMoveInput = transform.TransformVector(m_InputHandler.GetMoveInput());

                if (IsGrounded)
                {
                    Vector3 targetVelocity = worldspaceMoveInput * MaxSpeedOnGround * speedModifier;
                    if (IsCrouching)
                        targetVelocity *= MaxSpeedCrouchedRatio;

                    targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, m_GroundNormal) *
                                     targetVelocity.magnitude;

                    CharacterVelocity = Vector3.Lerp(CharacterVelocity, targetVelocity,
                        MovementSharpnessOnGround * Time.deltaTime);

                    if (IsGrounded && m_InputHandler.GetJumpInputDown())
                    {
                        if (SetCrouchingState(false, false))
                        {
                            CharacterVelocity = new Vector3(CharacterVelocity.x, 0f, CharacterVelocity.z);
                            CharacterVelocity += Vector3.up * JumpForce;
                            OnJumped?.Invoke();
                            m_LastTimeJumped = Time.time;
                            HasJumpedThisFrame = true;
                            IsGrounded = false;
                            m_GroundNormal = Vector3.up;
                        }
                    }
                }
                else
                {
                    CharacterVelocity += worldspaceMoveInput * AccelerationSpeedInAir * Time.deltaTime;
                    float verticalVelocity = CharacterVelocity.y;
                    Vector3 horizontalVelocity = Vector3.ProjectOnPlane(CharacterVelocity, Vector3.up);
                    horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, MaxSpeedInAir * speedModifier);
                    CharacterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);
                    CharacterVelocity += Vector3.down * GravityDownForce * Time.deltaTime;
                }
            }

            Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
            Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(m_Controller.height);
            m_Controller.Move(CharacterVelocity * Time.deltaTime);

            m_LatestImpactSpeed = Vector3.zero;
            if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, m_Controller.radius,
                CharacterVelocity.normalized, out RaycastHit hit, CharacterVelocity.magnitude * Time.deltaTime, -1,
                QueryTriggerInteraction.Ignore))
            {
                m_LatestImpactSpeed = CharacterVelocity;
                CharacterVelocity = Vector3.ProjectOnPlane(CharacterVelocity, hit.normal);
            }
        }

        void HandleCameraRotation()
        {
            m_CameraVerticalAngle += m_InputHandler.GetLookInputsVertical() * RotationSpeed * RotationMultiplier;
            m_CameraVerticalAngle = Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);
            PlayerCamera.transform.localEulerAngles = new Vector3(m_CameraVerticalAngle, 0, 0);
            transform.Rotate(new Vector3(0f, (m_InputHandler.GetLookInputsHorizontal() * RotationSpeed * RotationMultiplier), 0f), Space.Self);
        }

        bool IsNormalUnderSlopeLimit(Vector3 normal)
        {
            return Vector3.Angle(transform.up, normal) <= m_Controller.slopeLimit;
        }

        Vector3 GetCapsuleBottomHemisphere()
        {
            return transform.position + (transform.up * m_Controller.radius);
        }

        Vector3 GetCapsuleTopHemisphere(float atHeight)
        {
            return transform.position + (transform.up * (atHeight - m_Controller.radius));
        }

        public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
        {
            Vector3 directionRight = Vector3.Cross(direction, transform.up);
            return Vector3.Cross(slopeNormal, directionRight).normalized;
        }

        void UpdateCharacterHeight(bool force)
        {
            if (force)
            {
                m_Controller.height = m_TargetCharacterHeight;
                m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
                PlayerCamera.transform.localPosition = Vector3.up * m_TargetCharacterHeight * CameraHeightRatio;
            }
            else if (m_Controller.height != m_TargetCharacterHeight)
            {
                m_Controller.height = Mathf.Lerp(m_Controller.height, m_TargetCharacterHeight,
                    CrouchingSharpness * Time.deltaTime);
                m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
                PlayerCamera.transform.localPosition = Vector3.Lerp(PlayerCamera.transform.localPosition,
                    Vector3.up * m_TargetCharacterHeight * CameraHeightRatio, CrouchingSharpness * Time.deltaTime);
            }
        }

        void UpdateMovementState()
        {
            MovementState newState;
            if (!IsGrounded)
                newState = MovementState.InAir;
            else if (CharacterVelocity.magnitude < 0.1f)
                newState = MovementState.Idle;
            else if (IsCrouching)
                newState = MovementState.CrouchWalking;
            else if (m_IsSprinting)
                newState = MovementState.Running;
            else
                newState = MovementState.Walking;

            if (newState != m_CurrentMovementState)
            {
                m_CurrentMovementState = newState;
                OnMovementStateChanged?.Invoke(newState);
            }
        }

        bool SetCrouchingState(bool crouched, bool ignoreObstructions)
        {
            if (crouched)
            {
                m_TargetCharacterHeight = CapsuleHeightCrouching;
            }
            else
            {
                if (!ignoreObstructions)
                {
                    Collider[] standingOverlaps = Physics.OverlapCapsule(
                        GetCapsuleBottomHemisphere(),
                        GetCapsuleTopHemisphere(CapsuleHeightStanding),
                        m_Controller.radius,
                        -1,
                        QueryTriggerInteraction.Ignore);
                    foreach (Collider c in standingOverlaps)
                    {
                        if (c != m_Controller)
                        {
                            return false;
                        }
                    }
                }

                m_TargetCharacterHeight = CapsuleHeightStanding;
            }

            OnStanceChanged?.Invoke(crouched);
            IsCrouching = crouched;
            return true;
        }
    }
}
