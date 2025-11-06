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
    }
}
