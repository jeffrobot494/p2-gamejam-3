using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace EpsilonIV
{
    public enum DoorAnimationType
    {
        Slide,
        Rotate
    }

    /// <summary>
    /// Base door component
    /// Handles opening/closing and animation for one or two moving parts.
    /// </summary>
    public class Door : MonoBehaviour
    {
        [Header("Door State")]
        [Tooltip("Is the door currently locked?")]
        public bool IsLocked = true;

        [Tooltip("Is the door currently open?")]
        public bool IsOpen = false;

        [Header("VFX")]
        [SerializeField] private ParticleSystem openParticles;

        [Header("Animation")]
        [Tooltip("Speed of door animation")]
        public float OpenSpeed = 2f;

        [Header("Part 1 Settings")]
        [Tooltip("The first transform that will slide/rotate.")]
        public Transform MovingPart1;
        public DoorAnimationType AnimationType1 = DoorAnimationType.Slide;
        public Vector3 SlideDirection1 = Vector3.left;
        public float OpenDistance1 = 1.5f;
        public Vector3 RotationAxis1 = Vector3.up;
        public float RotationAngle1 = 90f;

        [Header("Part 2 Settings (Optional)")]
        [Tooltip("The second transform that will slide/rotate.")]
        public Transform MovingPart2;
        public DoorAnimationType AnimationType2 = DoorAnimationType.Slide;
        public Vector3 SlideDirection2 = Vector3.right;
        public float OpenDistance2 = 1.5f;
        public Vector3 RotationAxis2 = Vector3.up;
        public float RotationAngle2 = 90f;

        [Header("Audio")]
        public AudioClip OpenSound;
        public AudioClip CloseSound;
        public AudioClip LockedSound;

        [Header("Sound Propagation")]
        [Tooltip("Loudness of door sounds for AI (0-1). 0 = silent, 1 = very loud")]
        [Range(0f, 1f)]
        public float DoorSoundLoudness = 0.5f;

        [Tooltip("Quality of door sounds (used by AI for sound identification)")]
        public float DoorSoundQuality = 0f;

        [Header("Events")]
        public UnityEvent OnDoorOpened;
        public UnityEvent OnDoorClosed;
        public UnityEvent OnDoorLocked;
        public UnityEvent OnDoorUnlocked;

        [Header("Debug")]
        public bool DebugMode = false;

        private bool m_IsAnimating = false;
        private AudioSource m_AudioSource;
        private SoundEmitter m_SoundEmitter;

        private Vector3 m_ClosedPosition1, m_OpenPosition1;
        private Quaternion m_ClosedRotation1, m_OpenRotation1;

        private Vector3 m_ClosedPosition2, m_OpenPosition2;
        private Quaternion m_ClosedRotation2, m_OpenRotation2;

        void Awake()
        {
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null && (OpenSound != null || CloseSound != null || LockedSound != null))
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
            }

            // Get SoundEmitter component (optional)
            m_SoundEmitter = GetComponent<SoundEmitter>();

            if (MovingPart1 != null)
            {
                m_ClosedPosition1 = MovingPart1.localPosition;
                m_ClosedRotation1 = MovingPart1.localRotation;
                if (AnimationType1 == DoorAnimationType.Slide)
                {
                    m_OpenPosition1 = m_ClosedPosition1 + (SlideDirection1.normalized * OpenDistance1);
                    m_OpenRotation1 = m_ClosedRotation1;
                }
                else
                {
                    m_OpenPosition1 = m_ClosedPosition1;
                    m_OpenRotation1 = m_ClosedRotation1 * Quaternion.AngleAxis(RotationAngle1, RotationAxis1);
                }
            }

            if (MovingPart2 != null)
            {
                m_ClosedPosition2 = MovingPart2.localPosition;
                m_ClosedRotation2 = MovingPart2.localRotation;
                if (AnimationType2 == DoorAnimationType.Slide)
                {
                    m_OpenPosition2 = m_ClosedPosition2 + (SlideDirection2.normalized * OpenDistance2);
                    m_OpenRotation2 = m_ClosedRotation2;
                }
                else
                {
                    m_OpenPosition2 = m_ClosedPosition2;
                    m_OpenRotation2 = m_ClosedRotation2 * Quaternion.AngleAxis(RotationAngle2, RotationAxis2);
                }
            }
        }

        public void Open()
        {
            if (IsOpen || m_IsAnimating) return;
            if (IsLocked)
            {
                PlaySound(LockedSound);
                return;
            }
            StartCoroutine(Animate(true));
        }

        public void Close()
        {
            if (!IsOpen || m_IsAnimating) return;
            StartCoroutine(Animate(false));
        }

        public void Toggle() => _ = IsOpen ? Animate(false) : Animate(true);
        public void Unlock() { IsLocked = false; OnDoorUnlocked?.Invoke(); }
        public void Lock() { IsLocked = true; OnDoorLocked?.Invoke(); }

        private IEnumerator Animate(bool opening)
        {
            m_IsAnimating = true;
            IsOpen = opening;

            PlaySound(opening ? OpenSound : CloseSound);
            
            if (opening && openParticles != null)      // NEW
                openParticles.Play(true);

            Vector3 startPos1 = MovingPart1 != null ? MovingPart1.localPosition : Vector3.zero;
            Quaternion startRot1 = MovingPart1 != null ? MovingPart1.localRotation : Quaternion.identity;
            Vector3 endPos1 = opening ? m_OpenPosition1 : m_ClosedPosition1;
            Quaternion endRot1 = opening ? m_OpenRotation1 : m_ClosedRotation1;

            Vector3 startPos2 = MovingPart2 != null ? MovingPart2.localPosition : Vector3.zero;
            Quaternion startRot2 = MovingPart2 != null ? MovingPart2.localRotation : Quaternion.identity;
            Vector3 endPos2 = opening ? m_OpenPosition2 : m_ClosedPosition2;
            Quaternion endRot2 = opening ? m_OpenRotation2 : m_ClosedRotation2;

            float time = 0;
            while (time < 1)
            {
                time += Time.deltaTime * OpenSpeed;
                float t = Mathf.Clamp01(time);

                if (MovingPart1 != null) 
                {
                    MovingPart1.localPosition = Vector3.Lerp(startPos1, endPos1, t);
                    MovingPart1.localRotation = Quaternion.Slerp(startRot1, endRot1, t);
                }
                if (MovingPart2 != null) 
                {
                    MovingPart2.localPosition = Vector3.Lerp(startPos2, endPos2, t);
                    MovingPart2.localRotation = Quaternion.Slerp(startRot2, endRot2, t);
                }
                yield return null;
            }

            if (opening) OnDoorOpened?.Invoke();
            else OnDoorClosed?.Invoke();

            m_IsAnimating = false;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && m_AudioSource != null)
            {
                // Play audio locally
                m_AudioSource.PlayOneShot(clip);

                // Emit sound for AI detection (if SoundEmitter is present)
                if (m_SoundEmitter != null)
                {
                    if (DebugMode)
                    {
                        Debug.Log($"[Door] About to emit sound: Loudness={DoorSoundLoudness}, Quality={DoorSoundQuality}");
                    }

                    m_SoundEmitter.EmitSound(DoorSoundLoudness, DoorSoundQuality);

                    if (DebugMode)
                    {
                        Debug.Log($"[Door] Sound emitted. SoundEmitter.lastEmitLoudness={m_SoundEmitter.lastEmitLoudness}");
                    }
                }
            }
        }
    }
}
