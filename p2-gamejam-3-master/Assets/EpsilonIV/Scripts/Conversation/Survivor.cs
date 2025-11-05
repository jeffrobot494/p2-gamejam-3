using UnityEngine;
using UnityEngine.Events;
using System.Reflection;

namespace EpsilonIV
{
    /// <summary>
    /// Manages individual survivor state and behavior
    /// Controls state machine: Waiting -> Active -> Rescued
    /// </summary>
    public class Survivor : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("RadioNpc component (auto-found if not assigned)")]
        public RadioNpc radioNpc;

        [Header("Current State")]
        [Tooltip("Current state of this survivor")]
        [SerializeField]
        private SurvivorState currentState = SurvivorState.Waiting;

        [Header("Events")]
        [Tooltip("Fired when state changes")]
        public UnityEvent<Survivor, SurvivorState> OnStateChanged = new UnityEvent<Survivor, SurvivorState>();

        [Tooltip("Fired when survivor becomes active")]
        public UnityEvent<Survivor> OnActivated = new UnityEvent<Survivor>();

        [Tooltip("Fired when survivor is rescued")]
        public UnityEvent<Survivor> OnRescued = new UnityEvent<Survivor>();

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool debugMode = true;

        // Cached profile reference
        private SurvivorProfile m_Profile;

        /// <summary>
        /// Gets the current state
        /// </summary>
        public SurvivorState CurrentState => currentState;

        /// <summary>
        /// Gets the survivor profile
        /// </summary>
        public SurvivorProfile Profile => m_Profile;

        void Start()
        {
            // Auto-find RadioNpc if not assigned
            if (radioNpc == null)
            {
                radioNpc = GetComponent<RadioNpc>();
                if (radioNpc == null)
                {
                    Debug.LogError($"[Survivor] {gameObject.name} has no RadioNpc component!");
                    return;
                }
            }

            // Load SurvivorProfile from RadioNpc using reflection
            LoadProfileFromRadioNpc();

            if (m_Profile == null)
            {
                Debug.LogWarning($"[Survivor] {gameObject.name} has no SurvivorProfile assigned in RadioNpc!");
            }

            if (debugMode)
            {
                Debug.Log($"[Survivor] {gameObject.name} initialized in {currentState} state. Profile: {(m_Profile != null ? m_Profile.displayName : "None")}");
            }
        }

        /// <summary>
        /// Loads the SurvivorProfile from the RadioNpc's Player2Npc base class using reflection
        /// </summary>
        void LoadProfileFromRadioNpc()
        {
            if (radioNpc == null)
                return;

            // Access the private survivorProfile field from Player2Npc base class
            var baseType = radioNpc.GetType().BaseType; // Get Player2Npc
            if (baseType != null)
            {
                var field = baseType.GetField("survivorProfile", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    m_Profile = field.GetValue(radioNpc) as SurvivorProfile;
                }
            }
        }

        /// <summary>
        /// Sets the survivor state and triggers appropriate events
        /// </summary>
        public void SetState(SurvivorState newState)
        {
            if (currentState == newState)
            {
                if (debugMode)
                {
                    Debug.Log($"[Survivor] {gameObject.name} already in {newState} state, ignoring");
                }
                return;
            }

            SurvivorState previousState = currentState;
            currentState = newState;

            if (debugMode)
            {
                Debug.Log($"[Survivor] {gameObject.name} state changed: {previousState} -> {newState}");
            }

            // Fire state changed event
            OnStateChanged?.Invoke(this, newState);

            // Handle state entry logic
            switch (newState)
            {
                case SurvivorState.Active:
                    HandleActiveStateEntry();
                    break;

                case SurvivorState.Rescued:
                    HandleRescuedStateEntry();
                    break;
            }
        }

        /// <summary>
        /// Called when entering Active state
        /// </summary>
        void HandleActiveStateEntry()
        {
            if (debugMode)
            {
                Debug.Log($"[Survivor] {gameObject.name} entering Active state");
            }

            // Send activation prompt if available
            SendActivationPrompt();

            // Fire activated event
            OnActivated?.Invoke(this);
        }

        /// <summary>
        /// Called when entering Rescued state
        /// </summary>
        void HandleRescuedStateEntry()
        {
            if (debugMode)
            {
                Debug.Log($"[Survivor] {gameObject.name} entering Rescued state");
            }

            // Fire rescued event
            OnRescued?.Invoke(this);
        }

        /// <summary>
        /// Sends the activation prompt to this NPC (if defined in profile)
        /// </summary>
        void SendActivationPrompt()
        {
            if (m_Profile == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[Survivor] {gameObject.name} has no profile, cannot send activation prompt");
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(m_Profile.onActivatedPrompt))
            {
                if (debugMode)
                {
                    Debug.Log($"[Survivor] {gameObject.name} has no activation prompt defined, skipping");
                }
                return;
            }

            if (radioNpc == null)
            {
                Debug.LogError($"[Survivor] {gameObject.name} cannot send activation prompt - RadioNpc is null!");
                return;
            }

            if (debugMode)
            {
                Debug.Log($"[Survivor] {gameObject.name} sending activation prompt: '{m_Profile.onActivatedPrompt}'");
            }

            // Send the prompt to the NPC
            radioNpc.SendMessage(m_Profile.onActivatedPrompt, "");
        }

        /// <summary>
        /// Marks this survivor as rescued
        /// Called by SurvivorInteractable when player presses E
        /// </summary>
        public void Rescue()
        {
            if (currentState == SurvivorState.Rescued)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[Survivor] {gameObject.name} is already rescued!");
                }
                return;
            }

            if (debugMode)
            {
                Debug.Log($"[Survivor] {gameObject.name} rescued by player!");
            }

            SetState(SurvivorState.Rescued);
        }

        /// <summary>
        /// Checks if this survivor is currently active
        /// </summary>
        public bool IsActive()
        {
            return currentState == SurvivorState.Active;
        }

        /// <summary>
        /// Checks if this survivor has been rescued
        /// </summary>
        public bool IsRescued()
        {
            return currentState == SurvivorState.Rescued;
        }

        /// <summary>
        /// Checks if this survivor is waiting to be activated
        /// </summary>
        public bool IsWaiting()
        {
            return currentState == SurvivorState.Waiting;
        }

        // ===== TESTING / DEBUG CONTEXT MENU METHODS =====

        [ContextMenu("Set State: Waiting")]
        void TestSetStateWaiting()
        {
            SetState(SurvivorState.Waiting);
        }

        [ContextMenu("Set State: Active")]
        void TestSetStateActive()
        {
            SetState(SurvivorState.Active);
        }

        [ContextMenu("Set State: Rescued")]
        void TestSetStateRescued()
        {
            SetState(SurvivorState.Rescued);
        }

        [ContextMenu("Test Rescue")]
        void TestRescue()
        {
            Rescue();
        }

        [ContextMenu("Log Current State")]
        void LogCurrentState()
        {
            Debug.Log($"[Survivor] {gameObject.name} - State: {currentState}, Profile: {(m_Profile != null ? m_Profile.displayName : "None")}");
        }
    }

    /// <summary>
    /// Defines the possible states of a survivor
    /// </summary>
    public enum SurvivorState
    {
        Waiting,    // Not yet activated, GameObject is inactive
        Active,     // Currently active, player can communicate
        Rescued     // Player has rescued this survivor
    }
}
