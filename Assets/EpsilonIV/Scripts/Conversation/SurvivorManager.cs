using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace EpsilonIV
{
    /// <summary>
    /// Manages survivor progression - controls which survivor is active
    /// Singleton that orchestrates survivor activation sequence
    /// </summary>
    public class SurvivorManager : MonoBehaviour
    {
        public static SurvivorManager Instance { get; private set; }

        [Header("References")]
        [Tooltip("MessageManager that contains the npcGameObjects array")]
        public MessageManager messageManager;

        [Header("Progression")]
        [Tooltip("The ID of the checkpoint that will activate the first survivor.")]
        [SerializeField] private int firstSurvivorActivationCheckpointID = 1;

        [Tooltip("Current active survivor index (-1 = none active)")]
        [SerializeField]
        private int currentSurvivorIndex = -1;

        [Header("Events")]
        [Tooltip("Fired when a survivor is activated")]
        public UnityEvent<int, Survivor> OnSurvivorActivated = new UnityEvent<int, Survivor>();

        [Tooltip("Fired when a survivor is rescued")]
        public UnityEvent<int, Survivor> OnSurvivorRescued = new UnityEvent<int, Survivor>();

        [Tooltip("Fired when all survivors have been rescued")]
        public UnityEvent OnAllSurvivorsRescued = new UnityEvent();

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool debugMode = true;

        // Internal tracking
        private List<Survivor> allSurvivors = new List<Survivor>();

        /// <summary>
        /// Gets the current survivor index
        /// </summary>
        public int CurrentSurvivorIndex => currentSurvivorIndex;

        /// <summary>
        /// Gets the list of all survivors
        /// </summary>
        public IReadOnlyList<Survivor> AllSurvivors => allSurvivors.AsReadOnly();

        void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[SurvivorManager] Multiple SurvivorManagers detected! Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void Start()
        {
            if (messageManager == null)
            {
                Debug.LogError("[SurvivorManager] MessageManager is not assigned!");
                return;
            }

            // Find all survivors from MessageManager's NPC array
            FindAllSurvivors();

            // Deactivate all survivors initially
            DeactivateAllSurvivors();

            if (debugMode)
            {
                Debug.Log($"[SurvivorManager] Initialized with {allSurvivors.Count} survivors");
            }
        }

        /// <summary>
        /// Public handler for the CheckpointManager.OnCheckpointChanged event.
        /// </summary>
        public void HandleCheckpointChanged(Unity.FPS.Gameplay.RoomCheckpoint checkpoint)
        {
            Debug.Log($"[SurvivorManager] HandleCheckpointChanged received event for checkpoint ID: {checkpoint.checkpointID}. Comparing against activation ID: {firstSurvivorActivationCheckpointID}");

            // Check if this is the trigger for the first survivor and if no survivor is currently active
            if (checkpoint.checkpointID == firstSurvivorActivationCheckpointID && currentSurvivorIndex == -1)
            {
                if (debugMode)
                {
                    Debug.Log($"[SurvivorManager] First survivor activation checkpoint reached. Activating first survivor.");
                }
                ActivateFirstSurvivor();
            }
        }

        /// <summary>
        /// Finds all Survivor components from MessageManager's npcGameObjects array
        /// </summary>
        void FindAllSurvivors()
        {
            allSurvivors.Clear();

            if (messageManager == null || messageManager.npcGameObjects == null)
            {
                Debug.LogError("[SurvivorManager] Cannot find survivors - MessageManager or npcGameObjects is null!");
                return;
            }

            foreach (GameObject npcObj in messageManager.npcGameObjects)
            {
                if (npcObj == null)
                    continue;

                Survivor survivor = npcObj.GetComponent<Survivor>();
                if (survivor != null)
                {
                    allSurvivors.Add(survivor);

                    // Subscribe to rescue event
                    survivor.OnRescued.AddListener(HandleSurvivorRescued);

                    if (debugMode)
                    {
                        Debug.Log($"[SurvivorManager] Found survivor: {npcObj.name}");
                    }
                }
            }

            if (allSurvivors.Count == 0)
            {
                Debug.LogWarning("[SurvivorManager] No survivors found in MessageManager.npcGameObjects!");
            }
        }

        void OnDestroy()
        {
            // Unsubscribe from all survivor events
            foreach (Survivor survivor in allSurvivors)
            {
                if (survivor != null)
                {
                    survivor.OnRescued.RemoveListener(HandleSurvivorRescued);
                }
            }
        }

        /// <summary>
        /// Deactivates all survivor GameObjects
        /// </summary>
        void DeactivateAllSurvivors()
        {
            foreach (Survivor survivor in allSurvivors)
            {
                if (survivor != null)
                {
                    survivor.gameObject.SetActive(false);
                    survivor.SetState(SurvivorState.Waiting);

                    if (debugMode)
                    {
                        Debug.Log($"[SurvivorManager] Deactivated {survivor.gameObject.name}");
                    }
                }
            }
        }

        /// <summary>
        /// Activates the first survivor (index 0)
        /// Called by checkpoint trigger
        /// </summary>
        public void ActivateFirstSurvivor()
        {
            Debug.Log($"[SurvivorManager] ActivateFirstSurvivor called. Current survivor index: {currentSurvivorIndex}");
            if (allSurvivors.Count == 0)
            {
                Debug.LogError("[SurvivorManager] Cannot activate first survivor - no survivors found!");
                return;
            }

            if (currentSurvivorIndex != -1)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[SurvivorManager] Survivor already active (index {currentSurvivorIndex}), ignoring ActivateFirstSurvivor");
                }
                return;
            }

            ActivateSurvivor(0);
        }

        /// <summary>
        /// Activates a survivor by index
        /// </summary>
        void ActivateSurvivor(int index)
        {
            if (index < 0 || index >= allSurvivors.Count)
            {
                Debug.LogError($"[SurvivorManager] Cannot activate survivor - index {index} out of range (0-{allSurvivors.Count - 1})");
                return;
            }

            Survivor survivor = allSurvivors[index];
            if (survivor == null)
            {
                Debug.LogError($"[SurvivorManager] Cannot activate survivor at index {index} - survivor is null!");
                return;
            }

            currentSurvivorIndex = index;

            // Activate GameObject (MessageManager will now route to this NPC)
            survivor.gameObject.SetActive(true);

            // Wait for next frame so Start() can complete, then set state
            StartCoroutine(ActivateSurvivorAfterStart(index, survivor));
        }

        /// <summary>
        /// Waits for survivor Start() to complete before setting state to Active
        /// </summary>
        System.Collections.IEnumerator ActivateSurvivorAfterStart(int index, Survivor survivor)
        {
            // Wait one frame for Start() to run and load profile
            yield return null;

            // Now set survivor state to Active (triggers greeting prompt)
            survivor.SetState(SurvivorState.Active);

            if (debugMode)
            {
                Debug.Log($"[SurvivorManager] Activated survivor {index}: {survivor.gameObject.name}");
            }

            // Fire event
            OnSurvivorActivated?.Invoke(index, survivor);
        }

        /// <summary>
        /// Called when a survivor is rescued
        /// Handles progression to the next survivor
        /// </summary>
        void HandleSurvivorRescued(Survivor rescuedSurvivor)
        {
            if (rescuedSurvivor == null)
            {
                Debug.LogError("[SurvivorManager] HandleSurvivorRescued called with null survivor!");
                return;
            }

            // Find the index of the rescued survivor
            int rescuedIndex = allSurvivors.IndexOf(rescuedSurvivor);
            if (rescuedIndex == -1)
            {
                Debug.LogError($"[SurvivorManager] Rescued survivor {rescuedSurvivor.gameObject.name} not found in allSurvivors list!");
                return;
            }

            if (debugMode)
            {
                Debug.Log($"[SurvivorManager] Survivor rescued: {rescuedSurvivor.gameObject.name} (index {rescuedIndex})");
            }

            // Fire rescued event
            OnSurvivorRescued?.Invoke(rescuedIndex, rescuedSurvivor);

            // Deactivate the rescued survivor's GameObject
            rescuedSurvivor.gameObject.SetActive(false);

            if (debugMode)
            {
                Debug.Log($"[SurvivorManager] Deactivated rescued survivor: {rescuedSurvivor.gameObject.name}");
            }

            // Check if there's a next survivor
            int nextIndex = rescuedIndex + 1;
            if (nextIndex < allSurvivors.Count)
            {
                // Activate next survivor
                if (debugMode)
                {
                    Debug.Log($"[SurvivorManager] Activating next survivor (index {nextIndex})");
                }
                ActivateSurvivor(nextIndex);
            }
            else
            {
                // All survivors rescued!
                currentSurvivorIndex = -1;

                if (debugMode)
                {
                    Debug.Log($"[SurvivorManager] All survivors rescued!");
                }

                OnAllSurvivorsRescued?.Invoke();
            }
        }

        /// <summary>
        /// Gets the currently active survivor (or null if none)
        /// </summary>
        public Survivor GetCurrentSurvivor()
        {
            if (currentSurvivorIndex < 0 || currentSurvivorIndex >= allSurvivors.Count)
                return null;

            return allSurvivors[currentSurvivorIndex];
        }

        /// <summary>
        /// Checks if there's a currently active survivor
        /// </summary>
        public bool HasActiveSurvivor()
        {
            return currentSurvivorIndex >= 0 && currentSurvivorIndex < allSurvivors.Count;
        }

        /// <summary>
        /// Gets total number of survivors
        /// </summary>
        public int GetSurvivorCount()
        {
            return allSurvivors.Count;
        }

        // ===== TESTING / DEBUG CONTEXT MENU METHODS =====

        [ContextMenu("Activate First Survivor")]
        void TestActivateFirst()
        {
            ActivateFirstSurvivor();
        }

        [ContextMenu("Log All Survivors")]
        void TestLogAllSurvivors()
        {
            Debug.Log($"[SurvivorManager] Total survivors: {allSurvivors.Count}");
            for (int i = 0; i < allSurvivors.Count; i++)
            {
                Survivor s = allSurvivors[i];
                if (s != null)
                {
                    Debug.Log($"  [{i}] {s.gameObject.name} - State: {s.CurrentState}, Active: {s.gameObject.activeSelf}");
                }
                else
                {
                    Debug.Log($"  [{i}] NULL");
                }
            }
            Debug.Log($"[SurvivorManager] Current active index: {currentSurvivorIndex}");
        }

        [ContextMenu("Deactivate All Survivors")]
        void TestDeactivateAll()
        {
            DeactivateAllSurvivors();
            currentSurvivorIndex = -1;
        }

        [ContextMenu("Activate Survivor Index 0")]
        void TestActivateSurvivor0() => ActivateSurvivorByIndex(0);

        [ContextMenu("Activate Survivor Index 1")]
        void TestActivateSurvivor1() => ActivateSurvivorByIndex(1);

        [ContextMenu("Activate Survivor Index 2")]
        void TestActivateSurvivor2() => ActivateSurvivorByIndex(2);

        [ContextMenu("Activate Survivor Index 3")]
        void TestActivateSurvivor3() => ActivateSurvivorByIndex(3);

        [ContextMenu("Activate Survivor Index 4")]
        void TestActivateSurvivor4() => ActivateSurvivorByIndex(4);

        /// <summary>
        /// Activate a specific survivor by index for testing.
        /// Use "Log All Survivors" context menu to see indices.
        /// </summary>
        public void ActivateSurvivorByIndex(int index)
        {
            if (index < 0 || index >= allSurvivors.Count)
            {
                Debug.LogError($"[SurvivorManager] Cannot activate survivor - index {index} out of range (0-{allSurvivors.Count - 1})");
                return;
            }

            // Deactivate current survivor if any
            if (currentSurvivorIndex >= 0 && currentSurvivorIndex < allSurvivors.Count)
            {
                Survivor current = allSurvivors[currentSurvivorIndex];
                if (current != null)
                {
                    current.gameObject.SetActive(false);
                    current.SetState(SurvivorState.Waiting);
                }
            }

            // Activate the requested survivor
            ActivateSurvivor(index);
        }
    }
}
