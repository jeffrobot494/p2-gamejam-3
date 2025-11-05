using UnityEngine;
using UnityEngine.UI;

namespace EpsilonIV
{
    /// <summary>
    /// Simple test button for activating a survivor without losing game focus
    /// Attach to a UI Button in the scene for testing
    /// </summary>
    public class SurvivorTestButton : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The survivor to control with this button")]
        public Survivor targetSurvivor;

        [Header("Button Setup")]
        [Tooltip("The button component (auto-found if not assigned)")]
        public Button button;

        void Start()
        {
            // Auto-find button if not assigned
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button == null)
            {
                Debug.LogError("[SurvivorTestButton] No Button component found!");
                return;
            }

            // Subscribe to button click
            button.onClick.AddListener(ActivateSurvivor);

            if (targetSurvivor == null)
            {
                Debug.LogWarning("[SurvivorTestButton] No target survivor assigned!");
            }
        }

        /// <summary>
        /// Called when button is clicked - activates the survivor
        /// </summary>
        public void ActivateSurvivor()
        {
            if (targetSurvivor == null)
            {
                Debug.LogError("[SurvivorTestButton] Cannot activate - no target survivor assigned!");
                return;
            }

            Debug.Log($"[SurvivorTestButton] Activating survivor: {targetSurvivor.gameObject.name}");
            targetSurvivor.SetState(SurvivorState.Active);
        }

        /// <summary>
        /// Test method for setting to Waiting state
        /// </summary>
        public void SetWaiting()
        {
            if (targetSurvivor == null) return;
            Debug.Log($"[SurvivorTestButton] Setting survivor to Waiting: {targetSurvivor.gameObject.name}");
            targetSurvivor.SetState(SurvivorState.Waiting);
        }

        /// <summary>
        /// Test method for rescuing the survivor
        /// </summary>
        public void RescueSurvivor()
        {
            if (targetSurvivor == null) return;
            Debug.Log($"[SurvivorTestButton] Rescuing survivor: {targetSurvivor.gameObject.name}");
            targetSurvivor.Rescue();
        }
    }
}
