using UnityEngine;
using UnityEngine.Events;

namespace EpsilonIV
{
    /// <summary>
    /// Manages sprint stamina system.
    /// Player can only sprint when stamina is full.
    /// Stamina drains while sprinting and recharges when not sprinting.
    /// </summary>
    public class SprintStaminaManager : MonoBehaviour
    {
        [Header("Stamina Settings")]
        [Tooltip("Duration in seconds the player can sprint before stamina is depleted")]
        [SerializeField] private float sprintDuration = 3f;

        [Tooltip("Time in seconds for stamina to fully recharge from empty")]
        [SerializeField] private float rechargeTime = 5f;

        [Header("Events")]
        [Tooltip("Fired when stamina changes. Parameter: stamina percent (0-1)")]
        public UnityEvent<float> OnStaminaChanged = new UnityEvent<float>();

        [Tooltip("Fired when stamina is depleted")]
        public UnityEvent OnStaminaDepleted = new UnityEvent();

        [Tooltip("Fired when stamina is fully recharged")]
        public UnityEvent OnStaminaRecharged = new UnityEvent();

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // State
        private float currentStamina = 1f; // 0 to 1 (percent)
        private bool isSprinting = false;
        private bool wasFullLastFrame = true;

        // Derived rates
        private float drainRate; // Stamina percent per second while sprinting
        private float rechargeRate; // Stamina percent per second while recharging

        void Start()
        {
            // Calculate drain and recharge rates based on duration settings
            drainRate = 1f / sprintDuration; // Drain 100% over sprintDuration seconds
            rechargeRate = 1f / rechargeTime; // Recharge 100% over rechargeTime seconds

            currentStamina = 1f; // Start with full stamina
            wasFullLastFrame = true;

            if (debugMode)
            {
                Debug.Log($"[SprintStaminaManager] Initialized. Drain rate: {drainRate:F3}/s, Recharge rate: {rechargeRate:F3}/s");
            }

            // Fire initial stamina event
            OnStaminaChanged?.Invoke(currentStamina);
        }

        void Update()
        {
            if (isSprinting)
            {
                // Drain stamina
                currentStamina -= drainRate * Time.deltaTime;
                currentStamina = Mathf.Max(0f, currentStamina);

                if (debugMode && currentStamina <= 0f)
                {
                    Debug.Log("[SprintStaminaManager] Stamina depleted!");
                }

                // Fire depleted event when stamina reaches zero
                if (currentStamina <= 0f)
                {
                    OnStaminaDepleted?.Invoke();
                }

                OnStaminaChanged?.Invoke(currentStamina);
            }
            else
            {
                // Recharge stamina
                if (currentStamina < 1f)
                {
                    currentStamina += rechargeRate * Time.deltaTime;
                    currentStamina = Mathf.Min(1f, currentStamina);
                    OnStaminaChanged?.Invoke(currentStamina);
                }

                // Fire recharged event when stamina becomes full
                if (currentStamina >= 1f && !wasFullLastFrame)
                {
                    if (debugMode)
                    {
                        Debug.Log("[SprintStaminaManager] Stamina fully recharged!");
                    }
                    OnStaminaRecharged?.Invoke();
                }
            }

            wasFullLastFrame = currentStamina >= 1f;
        }

        /// <summary>
        /// Checks if the player can start sprinting.
        /// Player can only sprint when stamina is full.
        /// </summary>
        public bool CanSprint()
        {
            return currentStamina >= 1f;
        }

        /// <summary>
        /// Call this when the player starts sprinting.
        /// </summary>
        public void StartSprinting()
        {
            if (!CanSprint())
            {
                if (debugMode)
                {
                    Debug.LogWarning("[SprintStaminaManager] Cannot start sprinting - stamina not full!");
                }
                return;
            }

            isSprinting = true;

            if (debugMode)
            {
                Debug.Log("[SprintStaminaManager] Started sprinting");
            }
        }

        /// <summary>
        /// Call this when the player stops sprinting.
        /// </summary>
        public void StopSprinting()
        {
            isSprinting = false;

            if (debugMode)
            {
                Debug.Log($"[SprintStaminaManager] Stopped sprinting. Stamina: {currentStamina * 100f:F1}%");
            }
        }

        /// <summary>
        /// Gets the current stamina as a percentage (0-1).
        /// </summary>
        public float GetStaminaPercent()
        {
            return currentStamina;
        }

        /// <summary>
        /// Returns true if currently sprinting.
        /// </summary>
        public bool IsSprinting()
        {
            return isSprinting;
        }

        /// <summary>
        /// Forces stamina to stop draining (e.g., when player stops moving).
        /// Use this instead of StopSprinting if you want to maintain sprint input
        /// but stop draining stamina temporarily.
        /// </summary>
        public void PauseDrain()
        {
            isSprinting = false;
        }
    }
}
