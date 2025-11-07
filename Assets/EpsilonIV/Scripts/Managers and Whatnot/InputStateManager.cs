using UnityEngine;
using UnityEngine.Events;

namespace EpsilonIV
{
    /// <summary>
    /// Manages input states (Menu vs Gameplay) and cursor visibility/locking.
    /// Separates state management from input handling for cleaner architecture.
    /// </summary>
    public class InputStateManager : MonoBehaviour
    {
        public enum InputState
        {
            Menu,      // Cursor unlocked, visible - for UI interaction
            Gameplay   // Cursor locked, hidden - for FPS controls
        }

        [Header("Initial State")]
        [Tooltip("State to start in when the game loads")]
        [SerializeField] private InputState startingState = InputState.Menu;

        [Header("Menu Click Settings")]
        [Tooltip("If true, clicking outside UI in Menu state returns to Gameplay")]
        [SerializeField] private bool clickOutsideUIToReturnToGameplay = true;

        [Tooltip("UI GameObjects to ignore when checking clicks (e.g., HUD). Clicking on these won't return to gameplay.")]
        [SerializeField] private GameObject[] uiBlacklist;

        [Header("Events")]
        [Tooltip("Fired when entering Menu state")]
        public UnityEvent OnEnterMenuState;

        [Tooltip("Fired when entering Gameplay state")]
        public UnityEvent OnEnterGameplayState;

        [Tooltip("Fired when exiting Menu state")]
        public UnityEvent OnExitMenuState;

        [Tooltip("Fired when exiting Gameplay state")]
        public UnityEvent OnExitGameplayState;

        [Header("Debug")]
        [SerializeField] private bool debugLogging = true;

        private InputState currentState;

        /// <summary>
        /// Current input state (read-only)
        /// </summary>
        public InputState CurrentState => currentState;

        /// <summary>
        /// Is the player currently in Gameplay state (can move, look, shoot)?
        /// </summary>
        public bool IsInGameplayState => currentState == InputState.Gameplay;

        /// <summary>
        /// Is the player currently in Menu state (can click UI)?
        /// </summary>
        public bool IsInMenuState => currentState == InputState.Menu;

        void Start()
        {
            // Set initial state
            SetState(startingState);
        }

        void Update()
        {
            // Only check for clicks when in Menu state
            if (currentState == InputState.Menu && clickOutsideUIToReturnToGameplay)
            {
                // Check for left mouse click
                if (Input.GetMouseButtonDown(0))
                {
                    // Check if click is over UI (excluding blacklisted elements)
                    bool overUI = IsPointerOverUI();

                    if (debugLogging)
                    {
                        Debug.Log($"[InputStateManager] Click detected - Over UI: {overUI}");
                    }

                    if (!overUI)
                    {
                        // Clicked outside UI - return to gameplay
                        if (debugLogging)
                            Debug.Log("[InputStateManager] Clicked outside UI, returning to Gameplay");

                        SetState(InputState.Gameplay);
                    }
                }
            }
        }

        /// <summary>
        /// Toggle between Menu and Gameplay states
        /// </summary>
        public void ToggleState()
        {
            if (currentState == InputState.Menu)
                SetState(InputState.Gameplay);
            else
                SetState(InputState.Menu);
        }

        /// <summary>
        /// Set the current input state
        /// </summary>
        public void SetState(InputState newState)
        {
            if (currentState == newState)
                return; // Already in this state

            // Exit current state
            ExitState(currentState);

            // Enter new state
            InputState previousState = currentState;
            currentState = newState;
            EnterState(newState);

            if (debugLogging)
            {
                Debug.Log($"[InputStateManager] State changed: {previousState} → {newState}");
            }
        }

        /// <summary>
        /// Enter Menu state (for UI interaction)
        /// </summary>
        public void EnterMenuState()
        {
            SetState(InputState.Menu);
        }

        /// <summary>
        /// Enter Gameplay state (for FPS controls)
        /// </summary>
        public void EnterGameplayState()
        {
            SetState(InputState.Gameplay);
        }

        private void EnterState(InputState state)
        {
            switch (state)
            {
                case InputState.Menu:
                    // Unlock and show cursor for UI interaction
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;

                    if (debugLogging)
                        Debug.Log("[InputStateManager] Menu State: Cursor unlocked and visible");

                    OnEnterMenuState?.Invoke();
                    break;

                case InputState.Gameplay:
                    // Lock and hide cursor for FPS gameplay
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;

                    if (debugLogging)
                        Debug.Log("[InputStateManager] Gameplay State: Cursor locked and hidden");

                    OnEnterGameplayState?.Invoke();
                    break;
            }
        }

        private void ExitState(InputState state)
        {
            switch (state)
            {
                case InputState.Menu:
                    OnExitMenuState?.Invoke();
                    break;

                case InputState.Gameplay:
                    OnExitGameplayState?.Invoke();
                    break;
            }
        }

        /// <summary>
        /// Check if the mouse pointer is currently over a UI element (excluding blacklisted UI)
        /// </summary>
        private bool IsPointerOverUI()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                if (debugLogging)
                    Debug.LogWarning("[InputStateManager] EventSystem is null!");
                return false;
            }

            // Check if pointer is over ANY UI element
            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                if (debugLogging)
                    Debug.Log("[InputStateManager] IsPointerOverGameObject returned FALSE - not over any UI");
                return false;
            }

            // Pointer is over UI - now check if it's blacklisted UI
            var pointerEventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            {
                position = Input.mousePosition
            };

            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerEventData, results);

            if (debugLogging)
            {
                Debug.Log($"[InputStateManager] Raycast found {results.Count} UI elements");
                foreach (var result in results)
                {
                    Debug.Log($"[InputStateManager]   - {result.gameObject.name} (parent: {result.gameObject.transform.parent?.name})");
                }
            }

            // Check if there's ANY non-blacklisted UI under cursor
            bool hasNonBlacklistedUI = false;
            bool hasBlacklistedUI = false;

            foreach (var result in results)
            {
                if (IsInBlacklist(result.gameObject))
                {
                    hasBlacklistedUI = true;
                    if (debugLogging)
                        Debug.Log($"[InputStateManager]   -> Blacklisted: {result.gameObject.name}");
                }
                else
                {
                    hasNonBlacklistedUI = true;
                    if (debugLogging)
                        Debug.Log($"[InputStateManager]   -> NOT blacklisted: {result.gameObject.name}");
                }
            }

            // If there's ANY non-blacklisted UI (like buttons), stay in Menu
            if (hasNonBlacklistedUI)
            {
                if (debugLogging)
                    Debug.Log("[InputStateManager] Found non-blacklisted UI - staying in Menu");
                return true;
            }

            // Only blacklisted UI (like HUD/sprite) - treat as not over UI
            if (hasBlacklistedUI)
            {
                if (debugLogging)
                    Debug.Log("[InputStateManager] Only blacklisted UI found - treating as NOT over UI");
                return false;
            }

            // No UI at all (shouldn't reach here due to IsPointerOverGameObject check)
            return false;
        }

        /// <summary>
        /// Check if a GameObject or any of its parents are in the blacklist
        /// </summary>
        private bool IsInBlacklist(GameObject go)
        {
            if (uiBlacklist == null || uiBlacklist.Length == 0)
                return false;

            // Check the GameObject and all parents up the hierarchy
            Transform current = go.transform;
            while (current != null)
            {
                foreach (var blacklistedUI in uiBlacklist)
                {
                    if (blacklistedUI != null && current.gameObject == blacklistedUI)
                        return true;
                }
                current = current.parent;
            }

            return false;
        }
    }
}
