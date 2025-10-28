using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Base door component
    /// Handles opening/closing and animation
    /// Doesn't care about unlock mechanisms - that's handled by IDoorUnlocker components
    /// </summary>
    public class Door : MonoBehaviour
    {
        [Header("Door State")]
        [Tooltip("Is the door currently locked?")]
        public bool IsLocked = true;

        [Tooltip("Is the door currently open?")]
        public bool IsOpen = false;

        [Header("Animation")]
        [Tooltip("Door animation type")]
        public DoorAnimationType AnimationType = DoorAnimationType.Slide;

        [Tooltip("Direction/axis for sliding doors")]
        public Vector3 SlideDirection = Vector3.right;

        [Tooltip("Distance to slide/rotate")]
        public float OpenDistance = 2f;

        [Tooltip("Rotation axis for rotating doors (local space)")]
        public Vector3 RotationAxis = Vector3.up;

        [Tooltip("Rotation angle for rotating doors")]
        public float RotationAngle = 90f;

        [Tooltip("Speed of door animation")]
        public float OpenSpeed = 2f;

        [Header("Audio")]
        [Tooltip("Sound played when door opens")]
        public AudioClip OpenSound;

        [Tooltip("Sound played when door closes")]
        public AudioClip CloseSound;

        [Tooltip("Sound played when trying to open locked door")]
        public AudioClip LockedSound;

        [Header("Events")]
        public UnityEvent OnDoorOpened;
        public UnityEvent OnDoorClosed;
        public UnityEvent OnDoorLocked;
        public UnityEvent OnDoorUnlocked;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool DebugMode = false;

        // State
        private Vector3 m_ClosedPosition;
        private Quaternion m_ClosedRotation;
        private Vector3 m_OpenPosition;
        private Quaternion m_OpenRotation;
        private bool m_IsAnimating = false;
        private AudioSource m_AudioSource;

        void Start()
        {
            // Store initial closed state
            m_ClosedPosition = transform.localPosition;
            m_ClosedRotation = transform.localRotation;

            // Calculate open state based on animation type
            if (AnimationType == DoorAnimationType.Slide)
            {
                m_OpenPosition = m_ClosedPosition + SlideDirection.normalized * OpenDistance;
                m_OpenRotation = m_ClosedRotation;
            }
            else // Rotate
            {
                m_OpenPosition = m_ClosedPosition;
                m_OpenRotation = m_ClosedRotation * Quaternion.AngleAxis(RotationAngle, RotationAxis);
            }

            // Get or create audio source
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null && (OpenSound != null || CloseSound != null || LockedSound != null))
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
                m_AudioSource.playOnAwake = false;
                m_AudioSource.spatialBlend = 1f; // 3D sound
            }
        }

        /// <summary>
        /// Opens the door (if unlocked)
        /// </summary>
        public void Open()
        {
            if (IsOpen)
            {
                if (DebugMode)
                {
                    Debug.Log($"[Door] {gameObject.name} is already open");
                }
                return;
            }

            if (IsLocked)
            {
                if (DebugMode)
                {
                    Debug.Log($"[Door] {gameObject.name} is locked, cannot open");
                }
                PlaySound(LockedSound);
                return;
            }

            if (m_IsAnimating)
            {
                if (DebugMode)
                {
                    Debug.Log($"[Door] {gameObject.name} is currently animating");
                }
                return;
            }

            if (DebugMode)
            {
                Debug.Log($"[Door] Opening {gameObject.name}");
            }

            IsOpen = true;
            PlaySound(OpenSound);
            StartCoroutine(AnimateDoor(m_OpenPosition, m_OpenRotation));
            OnDoorOpened?.Invoke();
        }

        /// <summary>
        /// Closes the door
        /// </summary>
        public void Close()
        {
            if (!IsOpen)
            {
                if (DebugMode)
                {
                    Debug.Log($"[Door] {gameObject.name} is already closed");
                }
                return;
            }

            if (m_IsAnimating)
            {
                if (DebugMode)
                {
                    Debug.Log($"[Door] {gameObject.name} is currently animating");
                }
                return;
            }

            if (DebugMode)
            {
                Debug.Log($"[Door] Closing {gameObject.name}");
            }

            IsOpen = false;
            PlaySound(CloseSound);
            StartCoroutine(AnimateDoor(m_ClosedPosition, m_ClosedRotation));
            OnDoorClosed?.Invoke();
        }

        /// <summary>
        /// Unlocks the door
        /// </summary>
        public void Unlock()
        {
            if (!IsLocked)
                return;

            IsLocked = false;

            if (DebugMode)
            {
                Debug.Log($"[Door] {gameObject.name} unlocked");
            }

            OnDoorUnlocked?.Invoke();
        }

        /// <summary>
        /// Locks the door
        /// </summary>
        public void Lock()
        {
            if (IsLocked)
                return;

            IsLocked = true;

            if (DebugMode)
            {
                Debug.Log($"[Door] {gameObject.name} locked");
            }

            OnDoorLocked?.Invoke();
        }

        /// <summary>
        /// Toggles door open/closed
        /// </summary>
        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        /// <summary>
        /// Animates door to target position/rotation
        /// </summary>
        System.Collections.IEnumerator AnimateDoor(Vector3 targetPosition, Quaternion targetRotation)
        {
            m_IsAnimating = true;

            float elapsed = 0f;
            Vector3 startPosition = transform.localPosition;
            Quaternion startRotation = transform.localRotation;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * OpenSpeed;
                float t = Mathf.Clamp01(elapsed);

                transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
                transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);

                yield return null;
            }

            // Ensure final state
            transform.localPosition = targetPosition;
            transform.localRotation = targetRotation;

            m_IsAnimating = false;
        }

        /// <summary>
        /// Plays a sound clip
        /// </summary>
        void PlaySound(AudioClip clip)
        {
            if (clip != null && m_AudioSource != null)
            {
                m_AudioSource.PlayOneShot(clip);
            }
        }

        void OnDrawGizmosSelected()
        {
            // Visualize open position
            if (AnimationType == DoorAnimationType.Slide)
            {
                Vector3 openPos = transform.position + transform.TransformDirection(SlideDirection.normalized * OpenDistance);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, openPos);
                Gizmos.DrawWireCube(openPos, transform.lossyScale * 0.5f);
            }
        }
    }

    public enum DoorAnimationType
    {
        Slide,
        Rotate
    }
}
