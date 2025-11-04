using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Simple trigger to send prompts to survivors.
    /// Can be triggered by collider, timer, or manually via script/event.
    /// </summary>
    public class SurvivorPromptTrigger : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("SurvivorPrompter to trigger")]
        [SerializeField] private SurvivorPrompter survivorPrompter;

        [Header("Trigger Settings")]
        [Tooltip("Event ID to trigger (must match EventPrompt in SurvivorPrompter)")]
        [SerializeField] private string eventId = "";

        [Tooltip("Trigger when player enters collider (requires Collider with 'Is Trigger' checked)")]
        [SerializeField] private bool triggerOnEnter = true;

        [Tooltip("Trigger automatically after delay (useful for timed events)")]
        [SerializeField] private bool triggerOnTimer = false;

        [Tooltip("Delay in seconds before triggering (if triggerOnTimer is true)")]
        [SerializeField] private float triggerDelay = 5f;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        private bool hasTriggered = false;

        void Start()
        {
            if (survivorPrompter == null)
            {
                // Try to find it in scene
                survivorPrompter = FindFirstObjectByType<SurvivorPrompter>();
                if (survivorPrompter == null)
                {
                    Debug.LogError($"[SurvivorPromptTrigger] No SurvivorPrompter found on {gameObject.name}!");
                }
            }

            if (string.IsNullOrEmpty(eventId))
            {
                Debug.LogWarning($"[SurvivorPromptTrigger] No eventId set on {gameObject.name}!");
            }

            // Start timer if configured
            if (triggerOnTimer)
            {
                Invoke(nameof(TriggerPrompt), triggerDelay);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (!triggerOnEnter || hasTriggered)
                return;

            // Check if player entered
            if (other.CompareTag("Player"))
            {
                if (debugMode)
                    Debug.Log($"[SurvivorPromptTrigger] Player entered trigger zone: {gameObject.name}");

                TriggerPrompt();
            }
        }

        /// <summary>
        /// Manually trigger the prompt (can be called from UnityEvents or other scripts)
        /// </summary>
        public void TriggerPrompt()
        {
            if (hasTriggered)
            {
                if (debugMode)
                    Debug.Log($"[SurvivorPromptTrigger] Already triggered: {eventId}");
                return;
            }

            if (survivorPrompter == null)
            {
                Debug.LogError($"[SurvivorPromptTrigger] Cannot trigger - SurvivorPrompter is null!");
                return;
            }

            if (string.IsNullOrEmpty(eventId))
            {
                Debug.LogError($"[SurvivorPromptTrigger] Cannot trigger - eventId is empty!");
                return;
            }

            if (debugMode)
                Debug.Log($"[SurvivorPromptTrigger] Triggering event: {eventId}");

            survivorPrompter.TriggerEventPrompt(eventId);
            hasTriggered = true;
        }

        /// <summary>
        /// Reset the trigger (useful for testing)
        /// </summary>
        public void ResetTrigger()
        {
            hasTriggered = false;
            if (debugMode)
                Debug.Log($"[SurvivorPromptTrigger] Reset trigger: {eventId}");
        }

        [ContextMenu("Test: Trigger Prompt")]
        void TestTrigger()
        {
            TriggerPrompt();
        }
    }
}
