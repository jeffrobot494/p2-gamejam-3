using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Unity.FPS.Gameplay;
using EpsilonIV;

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

        [Header("Audio SFX Frequencies")]
        [Tooltip("Footstep SFX played per meter when walking")]
        [SerializeField] private float walkingSfxFrequency = 1f;

        [Tooltip("Footstep SFX played per meter when running")]
        [SerializeField] private float runningSfxFrequency = 2f;

        [Tooltip("Footstep SFX played per meter when crouch walking")]
        [SerializeField] private float crouchWalkingSfxFrequency = 0.5f;

        [Header("Sound Broadcast Frequencies")]
        [Tooltip("Sound broadcasts per meter when walking (can be higher than SFX for better AI tracking)")]
        [SerializeField] private float walkingBroadcastFrequency = 3f;

        [Tooltip("Sound broadcasts per meter when running")]
        [SerializeField] private float runningBroadcastFrequency = 5f;

        [Tooltip("Sound broadcasts per meter when crouch walking")]
        [SerializeField] private float crouchWalkingBroadcastFrequency = 2f;

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
        private float m_FootstepSfxDistanceCounter;
        private float m_SoundBroadcastDistanceCounter;

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
                // Get the appropriate frequencies and loudness for current state
                float sfxFrequency = GetSfxFrequencyForState(m_CurrentState);
                float broadcastFrequency = GetBroadcastFrequencyForState(m_CurrentState);
                float loudness = GetLoudnessForState(m_CurrentState);

                // Calculate distance traveled this frame
                float distanceTraveled = m_PlayerController.CharacterVelocity.magnitude * Time.deltaTime;
                m_FootstepSfxDistanceCounter += distanceTraveled;
                m_SoundBroadcastDistanceCounter += distanceTraveled;

                // Check if we've traveled far enough to play footstep SFX
                if (sfxFrequency > 0f && m_FootstepSfxDistanceCounter >= 1f / sfxFrequency)
                {
                    m_FootstepSfxDistanceCounter = 0f;
                    PlayFootstepSfx();
                }

                // Check if we've traveled far enough to broadcast sound (independent of SFX)
                if (broadcastFrequency > 0f && m_SoundBroadcastDistanceCounter >= 1f / broadcastFrequency)
                {
                    m_SoundBroadcastDistanceCounter = 0f;
                    BroadcastSound(loudness);
                }
            }
            else
            {
                // Reset counters when not moving
                m_FootstepSfxDistanceCounter = 0f;
                m_SoundBroadcastDistanceCounter = 0f;
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
                // Emit initial footstep SFX and broadcast immediately when starting to move
                float loudness = GetLoudnessForState(newState);
                PlayFootstepSfx();
                BroadcastSound(loudness);
            }

            // Reset distance counters on state change
            m_FootstepSfxDistanceCounter = 0f;
            m_SoundBroadcastDistanceCounter = 0f;
        }

        private float GetSfxFrequencyForState(MovementState state)
        {
            switch (state)
            {
                case MovementState.Walking:
                    return walkingSfxFrequency;
                case MovementState.Running:
                    return runningSfxFrequency;
                case MovementState.CrouchWalking:
                    return crouchWalkingSfxFrequency;
                default:
                    return 0f;
            }
        }

        private float GetBroadcastFrequencyForState(MovementState state)
        {
            switch (state)
            {
                case MovementState.Walking:
                    return walkingBroadcastFrequency;
                case MovementState.Running:
                    return runningBroadcastFrequency;
                case MovementState.CrouchWalking:
                    return crouchWalkingBroadcastFrequency;
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

        private void PlayFootstepSfx()
        {
            // Play the footstep SFX
            if (audioSource != null && footstepSfx != null)
            {
                audioSource.PlayOneShot(footstepSfx);
            }
        }

        private void BroadcastSound(float loudness)
        {
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
