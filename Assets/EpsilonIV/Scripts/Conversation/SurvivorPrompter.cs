using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace EpsilonIV
{
    /// <summary>
    /// Sends hidden context messages to survivors to make them feel dynamic and alive.
    /// Supports idle chatter (time-based) and event-triggered prompts.
    /// Messages are invisible to the player but prompt NPCs to speak naturally.
    /// The LLM generates responses based on each survivor's personality (already loaded in their profile).
    /// </summary>
    public class SurvivorPrompter : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("MessageManager to send messages through")]
        [SerializeField] private MessageManager messageManager;

        [Tooltip("SurvivorManager to get active survivor")]
        [SerializeField] private SurvivorManager survivorManager;

        [Header("Idle Chatter")]
        [Tooltip("Enable random idle messages when survivor hasn't spoken in a while")]
        [SerializeField] private bool enableIdleChatter = true;

        [Tooltip("Minimum seconds of silence before idle chatter can trigger")]
        [SerializeField] private float minIdleTime = 45f;

        [Tooltip("Maximum seconds of silence before idle chatter triggers")]
        [SerializeField] private float maxIdleTime = 120f;

        [Tooltip("Generic idle prompts - LLM will respond based on survivor's personality")]
        [SerializeField] [TextArea(3, 6)]
        private List<string> idlePrompts = new List<string>
        {
            "[INTERNAL: Break the silence. Say something about your current situation or feelings. Be natural and in-character.]",
            "[INTERNAL: Comment on the environment around you or how long you've been waiting.]",
            "[INTERNAL: Express concern, hope, or nervousness about your situation.]",
            "[INTERNAL: Ask if anyone is still there or if they can hear you.]"
        };

        [Header("Event-Based Prompts")]
        [Tooltip("Prompts that can be triggered by game events, checkpoints, or conditions")]
        [SerializeField] private List<EventPrompt> eventPrompts = new List<EventPrompt>();

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        // State tracking
        private float lastMessageTime;
        private float nextIdleTime;
        private HashSet<string> triggeredEventPrompts = new HashSet<string>();

        #region Unity Lifecycle

        void Start()
        {
            if (messageManager == null)
            {
                Debug.LogError("[SurvivorPrompter] MessageManager not assigned!");
                return;
            }

            if (survivorManager == null)
            {
                Debug.LogError("[SurvivorPrompter] SurvivorManager not assigned!");
                return;
            }

            // Initialize idle timer
            ResetIdleTimer();

            // Subscribe to survivor state changes
            survivorManager.OnSurvivorActivated.AddListener(OnSurvivorActivated);

            // Subscribe to player and NPC messages to track activity
            messageManager.OnPlayerMessageSent.AddListener(OnMessageActivity);
            messageManager.OnNpcResponseReceived.AddListener(OnNpcResponseActivity);

            if (debugMode)
                Debug.Log($"[SurvivorPrompter] Initialized. Idle chatter: {enableIdleChatter}");
        }

        void Update()
        {
            if (!enableIdleChatter) return;

            // Check if it's time for idle chatter
            if (Time.time - lastMessageTime > nextIdleTime)
            {
                SendIdlePrompt();
            }
        }

        void OnDestroy()
        {
            // Unsubscribe from events
            if (survivorManager != null)
            {
                survivorManager.OnSurvivorActivated.RemoveListener(OnSurvivorActivated);
            }

            if (messageManager != null)
            {
                messageManager.OnPlayerMessageSent.RemoveListener(OnMessageActivity);
                messageManager.OnNpcResponseReceived.RemoveListener(OnNpcResponseActivity);
            }
        }

        #endregion

        #region Idle Chatter

        /// <summary>
        /// Sends an idle prompt to make the survivor speak without player input
        /// </summary>
        void SendIdlePrompt()
        {
            Survivor activeSurvivor = survivorManager.GetCurrentSurvivor();
            if (activeSurvivor == null || !activeSurvivor.IsActive())
            {
                return;
            }

            RadioNpc npc = activeSurvivor.GetComponent<RadioNpc>();
            if (npc == null)
            {
                Debug.LogError($"[SurvivorPrompter] Active survivor {activeSurvivor.gameObject.name} has no RadioNpc!");
                return;
            }

            // Get random idle prompt
            string prompt = GetRandomIdlePrompt();

            if (debugMode)
                Debug.Log($"[SurvivorPrompter] Sending idle prompt to {activeSurvivor.gameObject.name}: '{prompt}'");

            // Send hidden context message
            // The LLM will respond based on the survivor's personality (loaded in their SurvivorProfile)
            npc.SendMessage(prompt, "");

            // Reset timer
            ResetIdleTimer();
        }

        /// <summary>
        /// Gets a random idle prompt from the configured list
        /// </summary>
        string GetRandomIdlePrompt()
        {
            if (idlePrompts.Count > 0)
            {
                return idlePrompts[Random.Range(0, idlePrompts.Count)];
            }

            // Fallback if no prompts configured
            return "[INTERNAL: Say something to break the silence. Be natural and in-character.]";
        }

        /// <summary>
        /// Resets the idle timer to a random value within the configured range
        /// </summary>
        void ResetIdleTimer()
        {
            lastMessageTime = Time.time;
            nextIdleTime = Random.Range(minIdleTime, maxIdleTime);

            if (debugMode)
                Debug.Log($"[SurvivorPrompter] Next idle prompt in {nextIdleTime:F1} seconds");
        }

        #endregion

        #region Event-Based Prompts

        /// <summary>
        /// Triggers a named event prompt. Call this from checkpoints, triggers, or game events.
        /// </summary>
        /// <param name="eventId">Unique identifier for this event</param>
        public void TriggerEventPrompt(string eventId)
        {
            EventPrompt prompt = eventPrompts.Find(p => p.eventId == eventId);
            if (prompt == null)
            {
                Debug.LogWarning($"[SurvivorPrompter] No event prompt found with ID: {eventId}");
                return;
            }

            // Check if this is a one-time prompt that's already been triggered
            if (prompt.triggerOnce && triggeredEventPrompts.Contains(eventId))
            {
                if (debugMode)
                    Debug.Log($"[SurvivorPrompter] Event prompt '{eventId}' already triggered (one-time only)");
                return;
            }

            Survivor activeSurvivor = survivorManager.GetCurrentSurvivor();
            if (activeSurvivor == null || !activeSurvivor.IsActive())
            {
                if (debugMode)
                    Debug.Log($"[SurvivorPrompter] No active survivor for event prompt '{eventId}'");
                return;
            }

            // Check if this prompt is for a specific survivor
            if (prompt.specificSurvivor != null && activeSurvivor.Profile != prompt.specificSurvivor)
            {
                if (debugMode)
                    Debug.Log($"[SurvivorPrompter] Event prompt '{eventId}' is for {prompt.specificSurvivor.displayName}, not current survivor");
                return;
            }

            RadioNpc npc = activeSurvivor.GetComponent<RadioNpc>();
            if (npc == null)
            {
                Debug.LogError($"[SurvivorPrompter] Active survivor {activeSurvivor.gameObject.name} has no RadioNpc!");
                return;
            }

            if (debugMode)
                Debug.Log($"[SurvivorPrompter] Triggering event prompt '{eventId}': '{prompt.promptMessage}'");

            // Send the prompt - LLM responds based on survivor's personality
            npc.SendMessage(prompt.promptMessage, "");

            // Mark as triggered if one-time
            if (prompt.triggerOnce)
            {
                triggeredEventPrompts.Add(eventId);
            }

            // Reset idle timer (survivor just spoke)
            ResetIdleTimer();

            // Fire event if configured
            prompt.onTriggered?.Invoke();
        }

        /// <summary>
        /// Sends a custom context prompt to the active survivor.
        /// Useful for dynamic game events or conditions.
        /// </summary>
        public void SendCustomPrompt(string promptMessage)
        {
            Survivor activeSurvivor = survivorManager.GetCurrentSurvivor();
            if (activeSurvivor == null || !activeSurvivor.IsActive())
            {
                if (debugMode)
                    Debug.Log("[SurvivorPrompter] No active survivor for custom prompt");
                return;
            }

            RadioNpc npc = activeSurvivor.GetComponent<RadioNpc>();
            if (npc == null)
            {
                Debug.LogError($"[SurvivorPrompter] Active survivor {activeSurvivor.gameObject.name} has no RadioNpc!");
                return;
            }

            if (debugMode)
                Debug.Log($"[SurvivorPrompter] Sending custom prompt: '{promptMessage}'");

            npc.SendMessage(promptMessage, "");
            ResetIdleTimer();
        }

        /// <summary>
        /// Resets the triggered event prompts (useful for testing or new game sessions)
        /// </summary>
        public void ResetTriggeredEvents()
        {
            triggeredEventPrompts.Clear();
            if (debugMode)
                Debug.Log("[SurvivorPrompter] Reset all triggered event prompts");
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when a new survivor is activated
        /// </summary>
        void OnSurvivorActivated(int index, Survivor survivor)
        {
            if (debugMode)
                Debug.Log($"[SurvivorPrompter] Survivor activated: {survivor.gameObject.name}");

            // Reset idle timer for new survivor
            ResetIdleTimer();
        }

        /// <summary>
        /// Called when player sends a message (resets idle timer)
        /// </summary>
        void OnMessageActivity(string message)
        {
            ResetIdleTimer();
        }

        /// <summary>
        /// Called when NPC responds (resets idle timer)
        /// </summary>
        void OnNpcResponseActivity(string npcName, string response)
        {
            ResetIdleTimer();
        }

        #endregion

        #region Testing / Debug

        [ContextMenu("Test: Send Random Idle Prompt")]
        void TestSendIdlePrompt()
        {
            SendIdlePrompt();
        }

        [ContextMenu("Test: Reset Idle Timer")]
        void TestResetIdleTimer()
        {
            ResetIdleTimer();
        }

        [ContextMenu("Test: Reset Triggered Events")]
        void TestResetTriggeredEvents()
        {
            ResetTriggeredEvents();
        }

        #endregion
    }

    /// <summary>
    /// Event-triggered prompt configuration.
    /// Can be triggered by checkpoints, game events, or specific conditions.
    /// </summary>
    [System.Serializable]
    public class EventPrompt
    {
        [Tooltip("Unique ID for this event (used when calling TriggerEventPrompt)")]
        public string eventId;

        [Tooltip("The hidden context message to send")]
        [TextArea(3, 6)]
        public string promptMessage;

        [Tooltip("Only trigger this prompt once per game session")]
        public bool triggerOnce = true;

        [Tooltip("Only trigger for this specific survivor (leave null for any survivor)")]
        public SurvivorProfile specificSurvivor;

        [Tooltip("Event fired when this prompt is triggered")]
        public UnityEvent onTriggered;
    }
}
