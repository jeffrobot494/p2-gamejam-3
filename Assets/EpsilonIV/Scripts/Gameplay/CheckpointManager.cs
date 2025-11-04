using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using EpsilonIV;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Manages all checkpoints in the scene (Singleton)
    /// Tracks current active checkpoint for respawn system
    /// </summary>
    public class CheckpointManager : MonoBehaviour
    {
        public static CheckpointManager Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("Default checkpoint to use if none is set")]
        public RoomCheckpoint DefaultCheckpoint;

        [Header("Events")]
        [Tooltip("Fired when the current checkpoint is changed.")]
        public UnityEvent<RoomCheckpoint> OnCheckpointChanged;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool DebugMode = true;

        // Tracking
        private RoomCheckpoint m_CurrentCheckpoint;
        private List<RoomCheckpoint> m_AllCheckpoints = new List<RoomCheckpoint>();

        /// <summary>
        /// Gets the currently active checkpoint
        /// </summary>
        public RoomCheckpoint CurrentCheckpoint => m_CurrentCheckpoint;

        /// <summary>
        /// Gets all registered checkpoints
        /// </summary>
        public IReadOnlyList<RoomCheckpoint> AllCheckpoints => m_AllCheckpoints.AsReadOnly();

        void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[CheckpointManager] Multiple CheckpointManagers detected! Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void Start()
        {
            // Set default checkpoint if specified
            if (DefaultCheckpoint != null)
            {
                SetCurrentCheckpoint(DefaultCheckpoint, false);
            }
            else if (m_AllCheckpoints.Count > 0)
            {
                // Use first checkpoint marked as default, or just first one
                RoomCheckpoint defaultCP = m_AllCheckpoints.Find(cp => cp.SetAsDefaultOnStart);
                if (defaultCP != null)
                {
                    SetCurrentCheckpoint(defaultCP, false);
                }
                else
                {
                    SetCurrentCheckpoint(m_AllCheckpoints[0], false);
                }
            }
            else
            {
                Debug.LogWarning("[CheckpointManager] No checkpoints registered! Respawn will fail.");
            }

             var player = FindFirstObjectByType<PlayerCharacterController>();
            if (player != null && m_CurrentCheckpoint != null)
            {
                player.transform.SetPositionAndRotation(
                    m_CurrentCheckpoint.GetRespawnPosition(),
                    m_CurrentCheckpoint.GetRespawnRotation()
                );

                if (DebugMode)
                    Debug.Log($"[CheckpointManager] Moved player to default checkpoint: {m_CurrentCheckpoint.name}");
            }
        }

        /// <summary>
        /// Registers a checkpoint with the manager (called by RoomCheckpoint on Start)
        /// </summary>
        public void RegisterCheckpoint(RoomCheckpoint checkpoint)
        {
            if (checkpoint == null)
                return;

            if (!m_AllCheckpoints.Contains(checkpoint))
            {
                m_AllCheckpoints.Add(checkpoint);

                if (DebugMode)
                {
                    Debug.Log($"[CheckpointManager] Registered checkpoint: {checkpoint.gameObject.name}");
                }
            }
        }

        /// <summary>
        /// Unregisters a checkpoint (called on destroy)
        /// </summary>
        public void UnregisterCheckpoint(RoomCheckpoint checkpoint)
        {
            if (checkpoint == null)
                return;

            m_AllCheckpoints.Remove(checkpoint);

            // If this was the current checkpoint, clear it
            if (m_CurrentCheckpoint == checkpoint)
            {
                m_CurrentCheckpoint = null;
            }
        }

        /// <summary>
        /// Sets the current active checkpoint
        /// </summary>
        public void SetCurrentCheckpoint(RoomCheckpoint checkpoint, bool fireEvent = true)
        {
            Debug.Log($"[CheckpointManager] SetCurrentCheckpoint called for '{checkpoint.name}'. Firing event: {fireEvent}");
            if (checkpoint == null)
            {
                Debug.LogWarning("[CheckpointManager] Attempted to set null checkpoint!");
                return;
            }

            // Don't do anything if this is already the active checkpoint
            if (m_CurrentCheckpoint == checkpoint) return;

            // Update previous checkpoint
            if (m_CurrentCheckpoint != null)
            {
                m_CurrentCheckpoint.SetActive(false);
            }

            // Set new checkpoint
            m_CurrentCheckpoint = checkpoint;
            m_CurrentCheckpoint.SetActive(true);

            if (DebugMode)
            {
                Debug.Log($"[CheckpointManager] Current checkpoint set to: {checkpoint.gameObject.name}");
            }

            // Fire the event
            if (fireEvent)
            {
                OnCheckpointChanged?.Invoke(m_CurrentCheckpoint);
            }
        }

        /// <summary>
        /// Gets the respawn position from current checkpoint
        /// </summary>
        public Vector3 GetRespawnPosition()
        {
            if (m_CurrentCheckpoint != null)
            {
                return m_CurrentCheckpoint.GetRespawnPosition();
            }

            Debug.LogWarning("[CheckpointManager] No current checkpoint! Returning Vector3.zero");
            return Vector3.zero;
        }

        /// <summary>
        /// Gets the respawn rotation from current checkpoint
        /// </summary>
        public Quaternion GetRespawnRotation()
        {
            if (m_CurrentCheckpoint != null)
            {
                return m_CurrentCheckpoint.GetRespawnRotation();
            }

            Debug.LogWarning("[CheckpointManager] No current checkpoint! Returning Quaternion.identity");
            return Quaternion.identity;
        }

        /// <summary>
        /// Gets the current checkpoint reference
        /// </summary>
        public RoomCheckpoint GetCurrentCheckpoint()
        {
            return m_CurrentCheckpoint;
        }

        /// <summary>
        /// Checks if a checkpoint is currently set
        /// </summary>
        public bool HasCheckpoint()
        {
            return m_CurrentCheckpoint != null;
        }
    }
}

