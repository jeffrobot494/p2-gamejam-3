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

        [Header("Emission Intervals")]
        [Tooltip("Time between footstep sounds when walking")]
        [SerializeField] private float walkingInterval = 1f;

        [Tooltip("Time between footstep sounds when running")]
        [SerializeField] private float runningInterval = 0.5f;

        [Tooltip("Time between footstep sounds when crouch walking")]
        [SerializeField] private float crouchWalkingInterval = 2f;

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
        private Coroutine m_EmissionCoroutine;
        private MovementState m_CurrentState = MovementState.Idle;

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

        private void OnMovementStateChanged(MovementState newState)
        {
            m_CurrentState = newState;

            // Stop any existing emission coroutine
            if (m_EmissionCoroutine != null)
            {
                StopCoroutine(m_EmissionCoroutine);
                m_EmissionCoroutine = null;
            }

            // Start appropriate emission pattern based on state
            switch (newState)
            {
                case MovementState.Walking:
                    m_EmissionCoroutine = StartCoroutine(EmitSoundLoop(walkingLoudness, walkingInterval));
                    break;

                case MovementState.Running:
                    m_EmissionCoroutine = StartCoroutine(EmitSoundLoop(runningLoudness, runningInterval));
                    break;

                case MovementState.CrouchWalking:
                    m_EmissionCoroutine = StartCoroutine(EmitSoundLoop(crouchWalkingLoudness, crouchWalkingInterval));
                    break;

                case MovementState.Idle:
                case MovementState.InAir:
                    // No sound emission for these states
                    break;
            }
        }

        private IEnumerator EmitSoundLoop(float loudness, float interval)
        {
            // Emit initial sound immediately
            EmitSound(loudness);

            // Then emit at regular intervals
            while (true)
            {
                yield return new WaitForSeconds(interval);
                EmitSound(loudness);
            }
        }

        private void EmitSound(float loudness)
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
