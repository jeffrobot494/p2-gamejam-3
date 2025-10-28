using System.Collections;
using UnityEngine;
using Unity.FPS.Game;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Orchestrates the entire death and respawn sequence
    /// Coordinates: camera animation, fade transitions, teleport, health reset, timer penalty
    /// </summary>
    public class DeathManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Player character controller")]
        public PlayerCharacterController PlayerController;

        [Tooltip("Player health component")]
        public Health PlayerHealth;

        [Tooltip("Death camera controller")]
        public DeathCameraController DeathCamera;

        [Tooltip("Game timer")]
        public GameTimer GameTimer;

        [Tooltip("Canvas group for fade to black")]
        public CanvasGroup FadeCanvasGroup;

        [Header("Timing")]
        [Tooltip("Duration of fade to black (seconds)")]
        public float FadeOutDuration = 1f;

        [Tooltip("Duration of fade from black (seconds)")]
        public float FadeInDuration = 1f;

        [Tooltip("Delay while screen is black before respawn (seconds)")]
        public float BlackScreenDelay = 0.5f;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool DebugMode = true;

        // State
        private bool m_IsRespawning = false;

        void Awake()
        {
            // Subscribe to player death event
            if (PlayerHealth != null)
            {
                PlayerHealth.OnDie += OnPlayerDeath;
            }
        }

        void Start()
        {
            // Auto-find references if not assigned
            if (PlayerController == null)
            {
                PlayerController = FindFirstObjectByType<PlayerCharacterController>();
            }

            if (PlayerHealth == null)
            {
                PlayerHealth = FindFirstObjectByType<Health>();
            }

            if (DeathCamera == null)
            {
                DeathCamera = FindFirstObjectByType<DeathCameraController>();
            }

            if (GameTimer == null)
            {
                GameTimer = FindFirstObjectByType<GameTimer>();
            }

            // Validate setup
            if (PlayerController == null)
            {
                Debug.LogError("[DeathManager] PlayerController not found!");
            }

            if (PlayerHealth == null)
            {
                Debug.LogError("[DeathManager] PlayerHealth not found!");
            }

            if (DeathCamera == null)
            {
                Debug.LogWarning("[DeathManager] DeathCamera not found! Death animation will be skipped.");
            }

            if (GameTimer == null)
            {
                Debug.LogWarning("[DeathManager] GameTimer not found! Time penalty will be skipped.");
            }

            if (FadeCanvasGroup == null)
            {
                Debug.LogWarning("[DeathManager] FadeCanvasGroup not assigned! Fades will be skipped.");
            }

            // Ensure fade starts invisible and disabled
            if (FadeCanvasGroup != null)
            {
                FadeCanvasGroup.alpha = 0f;
                FadeCanvasGroup.gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            // Unsubscribe from events
            if (PlayerHealth != null)
            {
                PlayerHealth.OnDie -= OnPlayerDeath;
            }
        }

        /// <summary>
        /// Called when player dies
        /// </summary>
        void OnPlayerDeath()
        {
            if (m_IsRespawning)
            {
                Debug.LogWarning("[DeathManager] Already respawning! Ignoring death event.");
                return;
            }

            if (DebugMode)
            {
                Debug.Log("[DeathManager] Player died! Starting respawn sequence...");
            }

            StartCoroutine(DeathSequence());
        }

        /// <summary>
        /// The complete death and respawn sequence
        /// </summary>
        IEnumerator DeathSequence()
        {
            m_IsRespawning = true;

            // 1. Disable player control
            if (PlayerController != null)
            {
                PlayerController.enabled = false;
            }

            // 2. Play death camera animation (fall and tilt)
            if (DeathCamera != null)
            {
                yield return StartCoroutine(DeathCamera.PlayDeathAnimation());
            }

            // 3. Fade to black
            if (FadeCanvasGroup != null)
            {
                yield return StartCoroutine(FadeToBlack());
            }

            // 4. Delay while screen is black
            yield return new WaitForSeconds(BlackScreenDelay);

            // 5. Perform respawn (while screen is black)
            PerformRespawn();

            // 6. Fade from black
            if (FadeCanvasGroup != null)
            {
                yield return StartCoroutine(FadeFromBlack());
            }

            // 7. Re-enable player control
            if (PlayerController != null)
            {
                PlayerController.enabled = true;

                if (DebugMode)
                {
                    Debug.Log($"[DeathManager] PlayerController re-enabled. Enabled state: {PlayerController.enabled}");
                }
            }

            // 8. Re-lock cursor for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (DebugMode)
            {
                Debug.Log("[DeathManager] Cursor locked for gameplay");
            }

            m_IsRespawning = false;

            if (DebugMode)
            {
                Debug.Log("[DeathManager] Respawn sequence complete!");
            }
        }

        /// <summary>
        /// Performs the actual respawn (teleport, reset health, penalty timer)
        /// </summary>
        void PerformRespawn()
        {
            if (DebugMode)
            {
                Debug.Log("[DeathManager] Performing respawn...");
            }

            // Get respawn position from checkpoint
            Vector3 respawnPosition = Vector3.zero;
            Quaternion respawnRotation = Quaternion.identity;

            if (CheckpointManager.Instance != null && CheckpointManager.Instance.HasCheckpoint())
            {
                respawnPosition = CheckpointManager.Instance.GetRespawnPosition();
                respawnRotation = CheckpointManager.Instance.GetRespawnRotation();
            }
            else
            {
                Debug.LogWarning("[DeathManager] No checkpoint found! Using current position.");
                if (PlayerController != null)
                {
                    respawnPosition = PlayerController.transform.position;
                    respawnRotation = PlayerController.transform.rotation;
                }
            }

            // Teleport player
            if (PlayerController != null)
            {
                // Disable character controller to allow teleport
                CharacterController charController = PlayerController.GetComponent<CharacterController>();
                if (charController != null)
                {
                    charController.enabled = false;
                }

                PlayerController.transform.position = respawnPosition;
                PlayerController.transform.rotation = respawnRotation;

                // Reset death state
                PlayerController.ResetDeathState();

                // Re-enable character controller
                if (charController != null)
                {
                    charController.enabled = true;
                }

                if (DebugMode)
                {
                    Debug.Log($"[DeathManager] Player teleported to: {respawnPosition}");
                }
            }

            // Reset camera to normal position
            if (DeathCamera != null)
            {
                DeathCamera.ResetCamera();
            }

            // Reset health to full
            if (PlayerHealth != null)
            {
                PlayerHealth.ResetDeathState();
                PlayerHealth.Heal(PlayerHealth.MaxHealth);

                if (DebugMode)
                {
                    Debug.Log("[DeathManager] Health reset to full");
                }
            }

            // Apply time penalty
            if (GameTimer != null)
            {
                GameTimer.ApplyDeathPenalty();

                if (DebugMode)
                {
                    Debug.Log($"[DeathManager] Time penalty applied: {GameTimer.DeathPenalty}s");
                }
            }
        }

        /// <summary>
        /// Fades screen to black
        /// </summary>
        IEnumerator FadeToBlack()
        {
            // Enable the fade overlay
            FadeCanvasGroup.gameObject.SetActive(true);

            float elapsed = 0f;

            while (elapsed < FadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FadeOutDuration);
                FadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            FadeCanvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Fades screen from black
        /// </summary>
        IEnumerator FadeFromBlack()
        {
            float elapsed = 0f;

            while (elapsed < FadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FadeInDuration);
                FadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            FadeCanvasGroup.alpha = 0f;

            // Disable the fade overlay when done
            FadeCanvasGroup.gameObject.SetActive(false);
        }

        /// <summary>
        /// Public method to trigger respawn manually (for testing)
        /// </summary>
        public void TriggerRespawn()
        {
            if (!m_IsRespawning)
            {
                StartCoroutine(DeathSequence());
            }
        }

        /// <summary>
        /// Gets whether a respawn is currently in progress
        /// </summary>
        public bool IsRespawning => m_IsRespawning;
    }
}
