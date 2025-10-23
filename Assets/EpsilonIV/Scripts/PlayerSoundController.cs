using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Unity.FPS.Gameplay;

/// <summary>
/// Emits sounds based on player movement state using the Sound system.
/// Subscribes to PlayerCharacterController movement state changes.
/// </summary>
[RequireComponent(typeof(PlayerCharacterController))]
[RequireComponent(typeof(SoundEmitter))]
public class PlayerSoundController : MonoBehaviour
    {
        [Header("Audio")]
        [Tooltip("Audio source for playing footstep SFX")]
        [SerializeField] private AudioSource audioSource;

        [Tooltip("Sound played for footsteps")]
        [SerializeField] private AudioClip footstepSfx;

        [Tooltip("Sound played when jumping")]
        [SerializeField] private AudioClip jumpSfx;

        [Tooltip("Sound played when landing")]
        [SerializeField] private AudioClip landSfx;

        [Tooltip("Sound played when taking damage from a fall")]
        [SerializeField] private AudioClip fallDamageSfx;

        [Header("Sound Settings")]
        [Tooltip("Loudness of footstep sounds [0-1]")]
        [Range(0f, 1f)]
        [SerializeField] private float walkingLoudness = 0.3f;

        [Range(0f, 1f)]
        [SerializeField] private float runningLoudness = 0.6f;

        [Range(0f, 1f)]
        [SerializeField] private float crouchWalkingLoudness = 0.15f;

        [Header("Emission Frequencies")]
        [Tooltip("Footstep sounds played per meter when walking")]
        [SerializeField] private float walkingFrequency = 1f;

        [Tooltip("Footstep sounds played per meter when running")]
        [SerializeField] private float runningFrequency = 2f;

        [Tooltip("Footstep sounds played per meter when crouch walking")]
        [SerializeField] private float crouchWalkingFrequency = 0.5f;

        [Header("Sound Quality")]
        [Tooltip("Quality parameter passed to Sound system")]
        [SerializeField] private float soundQuality = 1f;

        [Header("Jump/Land Sound Settings")]
        [Tooltip("Loudness of jump sound [0-1]")]
        [Range(0f, 1f)]
        [SerializeField] private float jumpLoudness = 0.4f;

        [Tooltip("Loudness of landing sound [0-1]")]
        [Range(0f, 1f)]
        [SerializeField] private float landLoudness = 0.5f;

        [Tooltip("Loudness of fall damage sound [0-1]")]
        [Range(0f, 1f)]
        [SerializeField] private float fallDamageLoudness = 0.7f;

        private PlayerCharacterController m_PlayerController;
        private SoundEmitter m_SoundEmitter;
        private MovementState m_CurrentState = MovementState.Idle;
        private float m_FootstepDistanceCounter;

        private void Awake()
        {
            m_PlayerController = GetComponent<PlayerCharacterController>();
            m_SoundEmitter = GetComponent<SoundEmitter>();
        }

        private void Start()
        {
            // Subscribe to movement state changes
            m_PlayerController.OnMovementStateChanged += OnMovementStateChanged;

            // Subscribe to jump/land/fall events
            m_PlayerController.OnJumped += OnJumped;
            m_PlayerController.OnLanded += OnLanded;
            m_PlayerController.OnFallDamage += OnFallDamage;
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            if (m_PlayerController != null)
            {
                m_PlayerController.OnMovementStateChanged -= OnMovementStateChanged;
                m_PlayerController.OnJumped -= OnJumped;
                m_PlayerController.OnLanded -= OnLanded;
                m_PlayerController.OnFallDamage -= OnFallDamage;
            }
        }

        private void Update()
        {
            // Only track footsteps when in a moving state
            if (m_CurrentState == MovementState.Walking ||
                m_CurrentState == MovementState.Running ||
                m_CurrentState == MovementState.CrouchWalking)
            {
                // Get the appropriate frequency and loudness for current state
                float frequency = GetFrequencyForState(m_CurrentState);
                float loudness = GetLoudnessForState(m_CurrentState);

                // Calculate distance traveled this frame
                float distanceTraveled = m_PlayerController.CharacterVelocity.magnitude * Time.deltaTime;
                m_FootstepDistanceCounter += distanceTraveled;

                // Check if we've traveled far enough to emit a footstep
                if (frequency > 0f && m_FootstepDistanceCounter >= 1f / frequency)
                {
                    m_FootstepDistanceCounter = 0f;
                    EmitFootstepSound(loudness);
                }
            }
            else
            {
                // Reset counter when not moving
                m_FootstepDistanceCounter = 0f;
            }
        }

        private void OnMovementStateChanged(MovementState newState)
        {
            MovementState previousState = m_CurrentState;
            m_CurrentState = newState;

            // Check if transitioning from non-moving to moving state
            bool wasNotMoving = previousState == MovementState.Idle || previousState == MovementState.InAir;
            bool isNowMoving = newState == MovementState.Walking || newState == MovementState.Running || newState == MovementState.CrouchWalking;

            if (wasNotMoving && isNowMoving)
            {
                // Emit initial footstep immediately when starting to move
                float loudness = GetLoudnessForState(newState);
                EmitFootstepSound(loudness);
            }

            // Reset distance counter on state change
            m_FootstepDistanceCounter = 0f;
        }

        private float GetFrequencyForState(MovementState state)
        {
            switch (state)
            {
                case MovementState.Walking:
                    return walkingFrequency;
                case MovementState.Running:
                    return runningFrequency;
                case MovementState.CrouchWalking:
                    return crouchWalkingFrequency;
                default:
                    return 0f;
            }
        }

        private float GetLoudnessForState(MovementState state)
        {
            switch (state)
            {
                case MovementState.Walking:
                    return walkingLoudness;
                case MovementState.Running:
                    return runningLoudness;
                case MovementState.CrouchWalking:
                    return crouchWalkingLoudness;
                default:
                    return 0f;
            }
        }

        private void EmitFootstepSound(float loudness)
        {
            // Play the footstep SFX
            if (audioSource != null && footstepSfx != null)
            {
                audioSource.PlayOneShot(footstepSfx);
            }

            // Broadcast sound to hearing system
            if (m_SoundEmitter != null)
            {
                m_SoundEmitter.EmitSound(loudness, soundQuality);
            }
        }

        private void OnJumped()
        {
            // Play jump SFX
            if (audioSource != null && jumpSfx != null)
            {
                audioSource.PlayOneShot(jumpSfx);
            }

            // Broadcast jump sound to hearing system
            if (m_SoundEmitter != null)
            {
                m_SoundEmitter.EmitSound(jumpLoudness, soundQuality);
            }
        }

        private void OnLanded()
        {
            // Play land SFX
            if (audioSource != null && landSfx != null)
            {
                audioSource.PlayOneShot(landSfx);
            }

            // Broadcast land sound to hearing system
            if (m_SoundEmitter != null)
            {
                m_SoundEmitter.EmitSound(landLoudness, soundQuality);
            }
        }

        private void OnFallDamage()
        {
            // Play fall damage SFX
            if (audioSource != null && fallDamageSfx != null)
            {
                audioSource.PlayOneShot(fallDamageSfx);
            }

            // Broadcast fall damage sound to hearing system
            if (m_SoundEmitter != null)
            {
                m_SoundEmitter.EmitSound(fallDamageLoudness, soundQuality);
            }
        }
    }
