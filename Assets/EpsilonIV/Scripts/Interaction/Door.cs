using UnityEngine;
using UnityEngine.Events;

namespace EpsilonIV
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

        [Tooltip("Direction/axis for sliding doors (local space of the moving object)")]
        public Vector3 SlideDirection = Vector3.right;

        [Tooltip("Distance to slide/rotate")]
        public float OpenDistance = 2f;

        [Tooltip("Rotation axis for rotating doors (local space of the moving object)")]
        public Vector3 RotationAxis = Vector3.up;

        [Tooltip("Rotation angle for rotating doors")]
        public float RotationAngle = 90f;

        [Tooltip("Speed of door animation")]
        public float OpenSpeed = 2f;

        [Header("Target")]
        [Tooltip("Optional: the transform that will actually slide/rotate. If not set, this component's transform will be used.")]
        public Transform MovingPart;

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
        private Transform m_Target;                 // The transform that actually moves
        private Vector3 m_ClosedPosition;           // local position of target when closed
        private Quaternion m_ClosedRotation;        // local rotation of target when closed
        private Vector3 m_OpenPosition;             // local position of target when open
        private Quaternion m_OpenRotation;          // local rotation of target when open
        private bool m_IsAnimating = false;
        private AudioSource m_AudioSource;

        void Awake()
        {
            m_Target = MovingPart != null ? MovingPart : transform;
        }

        void Start()
        {
            CacheClosedPose();
            ComputeOpenPose();

            // Get or create audio source
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null && (OpenSound != null || CloseSound != null || LockedSound != null))
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
                m_AudioSource.playOnAwake = false;
                m_AudioSource.spatialBlend = 1f; // 3D sound
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Keep target up-to-date in the editor
            m_Target = MovingPart != null ? MovingPart : transform;

            // If we have a target, keep open pose preview consistent as you tweak fields
            if (m_Target != null)
            {
                CacheClosedPose();
                ComputeOpenPose();
            }
        }
#endif

        /// <summary>
        /// Opens the door (if unlocked)
        /// </summary>
        public void Open()
        {
            if (IsOpen)
            {
                if (DebugMode) Debug.Log($"[Door] {gameObject.name} is already open");
                return;
            }

            if (IsLocked)
            {
                if (DebugMode) Debug.Log($"[Door] {gameObject.name} is locked, cannot open");
                PlaySound(LockedSound);
                return;
            }

            if (m_IsAnimating)
            {
                if (DebugMode) Debug.Log($"[Door] {gameObject.name} is currently animating");
                return;
            }

            if (DebugMode) Debug.Log($"[Door] Opening {gameObject.name}");

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
                if (DebugMode) Debug.Log($"[Door] {gameObject.name} is already closed");
                return;
            }

            if (m_IsAnimating)
            {
                if (DebugMode) Debug.Log($"[Door] {gameObject.name} is currently animating");
                return;
            }

            if (DebugMode) Debug.Log($"[Door] Closing {gameObject.name}");

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
            if (!IsLocked) return;

            IsLocked = false;

            if (DebugMode) Debug.Log($"[Door] {gameObject.name} unlocked");

            OnDoorUnlocked?.Invoke();
        }

        /// <summary>
        /// Locks the door
        /// </summary>
        public void Lock()
        {
            if (IsLocked) return;

            IsLocked = true;

            if (DebugMode) Debug.Log($"[Door] {gameObject.name} locked");

            OnDoorLocked?.Invoke();
        }

        /// <summary>
        /// Toggles door open/closed
        /// </summary>
        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        /// <summary>
        /// Animates the moving part to target local position/rotation
        /// </summary>
        System.Collections.IEnumerator AnimateDoor(Vector3 targetPosition, Quaternion targetRotation)
        {
            m_IsAnimating = true;

            float elapsed = 0f;
            Vector3 startPosition = m_Target.localPosition;
            Quaternion startRotation = m_Target.localRotation;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * OpenSpeed;
                float t = Mathf.Clamp01(elapsed);

                m_Target.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
                m_Target.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);

                yield return null;
            }

            // Ensure final state
            m_Target.localPosition = targetPosition;
            m_Target.localRotation = targetRotation;

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
            var target = MovingPart != null ? MovingPart : transform;

            // Visualize open position for sliding doors
            if (AnimationType == DoorAnimationType.Slide && target != null)
            {
                Vector3 worldClosed = target.position;
                Vector3 worldOpen = worldClosed + target.TransformDirection(SlideDirection.normalized * OpenDistance);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(worldClosed, worldOpen);
                Gizmos.DrawWireCube(worldOpen, target.lossyScale * 0.5f);
            }
        }

        // ---- Helpers ----

        private void CacheClosedPose()
        {
            m_ClosedPosition = m_Target.localPosition;
            m_ClosedRotation = m_Target.localRotation;
        }

        private void ComputeOpenPose()
        {
            if (AnimationType == DoorAnimationType.Slide)
            {
                m_OpenPosition = m_ClosedPosition + (SlideDirection.normalized * OpenDistance);
                m_OpenRotation = m_ClosedRotation;
            }
            else // Rotate
            {
                m_OpenPosition = m_ClosedPosition;
                m_OpenRotation = m_ClosedRotation * Quaternion.AngleAxis(RotationAngle, RotationAxis);
            }
        }
    }

    public enum DoorAnimationType
    {
        Slide,
        Rotate
    }
}
