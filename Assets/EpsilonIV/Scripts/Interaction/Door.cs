using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace EpsilonIV
{
    public enum DoorAnimationType
    {
        Slide,
        Rotate
    }

    [System.Serializable]
    public class DoorPart
    {
        [Tooltip("The transform that will actually slide/rotate.")]
        public Transform MovingObject;

        [Tooltip("The animation type for this part of the door.")]
        public DoorAnimationType AnimationType;

        [Tooltip("Direction/axis for sliding (local space of the moving object).")]
        public Vector3 SlideDirection;
        [Tooltip("Distance to slide.")]
        public float OpenDistance;

        [Tooltip("Rotation axis for rotating (local space of the moving object).")]
        public Vector3 RotationAxis;
        [Tooltip("Rotation angle for rotating.")]
        public float RotationAngle;

        // Internal state, not shown in inspector
        [System.NonSerialized] public Vector3 ClosedPosition;
        [System.NonSerialized] public Quaternion ClosedRotation;
        [System.NonSerialized] public Vector3 OpenPosition;
        [System.NonSerialized] public Quaternion OpenRotation;
    }

    /// <summary>
    /// Base door component
    /// Handles opening/closing and animation for one or more moving parts.
    /// Doesn't care about unlock mechanisms - that's handled by IDoorUnlocker components.
    /// </summary>
    public class Door : MonoBehaviour
    {
        [Header("Door State")]
        [Tooltip("Is the door currently locked?")]
        public bool IsLocked = true;

        [Tooltip("Is the door currently open?")]
        public bool IsOpen = false;

        [Header("Animation")]
        [Tooltip("Speed of door animation")]
        public float OpenSpeed = 2f;

        [Header("Door Parts")]
        [Tooltip("A list of all the parts that make up this door.")]
        public List<DoorPart> DoorParts = new List<DoorPart>();

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
        private bool m_IsAnimating = false;
        private AudioSource m_AudioSource;

        void Awake()
        {
            // Get or create audio source
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null && (OpenSound != null || CloseSound != null || LockedSound != null))
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
                m_AudioSource.playOnAwake = false;
                m_AudioSource.spatialBlend = 1f; // 3D sound
            }

            // Initialize all door parts
            if (DebugMode)
            {
                Debug.Log($"[Door] Awake() called. DoorParts.Count = {DoorParts.Count}");
            }

            foreach (var part in DoorParts)
            {
                if (part.MovingObject != null)
                {
                    CacheAndComputePoses(part);
                }
            }
        }

        void Start()
        {
            // Start is now empty, initialization is in Awake
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Keep open pose previews consistent in the editor as you tweak fields
            foreach (var part in DoorParts)
            {
                if (part.MovingObject != null)
                {
                    CacheAndComputePoses(part);
                }
            }
        }
#endif

        /// <summary>
        /// Opens the door (if unlocked)
        /// </summary>
        public void Open()
        {
            if (DebugMode) Debug.Log($"[Door] Open() called. IsOpen: {IsOpen}, IsLocked: {IsLocked}, IsAnimating: {m_IsAnimating}");

            if (IsOpen) return;
            if (IsLocked)
            {
                if (DebugMode) Debug.Log($"[Door] {gameObject.name} is locked, cannot open");
                PlaySound(LockedSound);
                return;
            }
            if (m_IsAnimating) return;

            if (DebugMode) Debug.Log($"[Door] Opening {gameObject.name}");

            IsOpen = true;
            PlaySound(OpenSound);
            StartCoroutine(Animate(true));
            OnDoorOpened?.Invoke();
        }

        /// <summary>
        /// Closes the door
        /// </summary>
        public void Close()
        {
            if (!IsOpen) return;
            if (m_IsAnimating) return;

            if (DebugMode) Debug.Log($"[Door] Closing {gameObject.name}");

            IsOpen = false;
            PlaySound(CloseSound);
            StartCoroutine(Animate(false));
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
        /// Animates all door parts to their target open or closed state.
        /// </summary>
        System.Collections.IEnumerator Animate(bool opening)
        {
            m_IsAnimating = true;
            if (DebugMode) Debug.Log($"[Door] Animate coroutine started. Opening: {opening}");

            float elapsed = 0f;

            // Store start/end poses for all parts before the loop
            var startPositions = new Vector3[DoorParts.Count];
            var startRotations = new Quaternion[DoorParts.Count];
            var endPositions = new Vector3[DoorParts.Count];
            var endRotations = new Quaternion[DoorParts.Count];

            for (int i = 0; i < DoorParts.Count; i++)
            {
                var part = DoorParts[i];
                if (part.MovingObject == null) continue;

                startPositions[i] = part.MovingObject.localPosition;
                startRotations[i] = part.MovingObject.localRotation;
                endPositions[i] = opening ? part.OpenPosition : part.ClosedPosition;
                endRotations[i] = opening ? part.OpenRotation : part.ClosedRotation;

                if (DebugMode) Debug.Log($"[Door] Part {i} ({part.MovingObject.name}): StartPos={startPositions[i]}, EndPos={endPositions[i]}");
            }

            bool hasLoggedTime = false;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * OpenSpeed;
                float t = Mathf.Clamp01(elapsed);

                if (DebugMode && !hasLoggedTime)
                {
                    Debug.Log($"[Door] Animation frame. t = {t}");
                    hasLoggedTime = true;
                }

                for (int i = 0; i < DoorParts.Count; i++)
                {
                    var part = DoorParts[i];
                    if (part.MovingObject == null) continue;

                    part.MovingObject.localPosition = Vector3.Lerp(startPositions[i], endPositions[i], t);
                    part.MovingObject.localRotation = Quaternion.Slerp(startRotations[i], endRotations[i], t);
                }

                yield return null;
            }

            // Ensure final state for all parts
            for (int i = 0; i < DoorParts.Count; i++)
            {
                var part = DoorParts[i];
                if (part.MovingObject == null) continue;

                part.MovingObject.localPosition = endPositions[i];
                part.MovingObject.localRotation = endRotations[i];
            }

            m_IsAnimating = false;
        }

        void PlaySound(AudioClip clip)
        {
            if (clip != null && m_AudioSource != null)
            {
                m_AudioSource.PlayOneShot(clip);
            }
        }

        void OnDrawGizmosSelected()
        {
            // Visualize the open pose for all door parts
            foreach (var part in DoorParts)
            {
                if (part == null || part.MovingObject == null) continue;

                // Re-calculate poses in case they changed in editor without OnValidate firing
                Vector3 closedPos = part.MovingObject.position;
                Quaternion closedRot = part.MovingObject.rotation;
                Vector3 openPos;
                Quaternion openRot;

                if (part.AnimationType == DoorAnimationType.Slide)
                {
                    openPos = closedPos + part.MovingObject.TransformDirection(part.SlideDirection.normalized * part.OpenDistance);
                    openRot = closedRot;
                }
                else // Rotate
                {
                    openPos = closedPos;
                    openRot = closedRot * Quaternion.AngleAxis(part.RotationAngle, part.RotationAxis);
                }

                Gizmos.color = Color.green;
                Gizmos.DrawLine(closedPos, openPos);
                
                // To properly draw the rotated cube, we need to set the gizmo matrix
                Matrix4x4 originalMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(openPos, openRot, part.MovingObject.lossyScale);
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                Gizmos.matrix = originalMatrix;
            }
        }

        // ---- Helpers ----

        private void CacheAndComputePoses(DoorPart part)
        {
            if (part.MovingObject == null) return;

            // 1. Cache the closed pose
            part.ClosedPosition = part.MovingObject.localPosition;
            part.ClosedRotation = part.MovingObject.localRotation;

            // 2. Compute the open pose based on the closed pose
            if (part.AnimationType == DoorAnimationType.Slide)
            {
                part.OpenPosition = part.ClosedPosition + (part.SlideDirection.normalized * part.OpenDistance);
                part.OpenRotation = part.ClosedRotation;
            }
            else // Rotate
            {
                part.OpenPosition = part.ClosedPosition;
                part.OpenRotation = part.ClosedRotation * Quaternion.AngleAxis(part.RotationAngle, part.RotationAxis);
            }

            if (DebugMode)
            {
                Debug.Log($"[Door] Caching poses for {part.MovingObject.name}: OpenDistance={part.OpenDistance}, SlideDirection={part.SlideDirection}, Calculated OpenPos={part.OpenPosition}");
            }
        }
    }
}
