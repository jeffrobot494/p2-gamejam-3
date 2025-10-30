using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EpsilonIV
{
    /// <summary>
    /// Manages radio input field focus with Enter key using Unity's new Input System.
    /// Press Enter to focus input field, press Enter again to send message and return to game.
    /// </summary>
    public class RadioInputManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The input field for typing messages")]
        public TMP_InputField inputField;

        [Header("Input Actions")]
        [Tooltip("Input Action Asset containing player controls")]
        public InputActionAsset inputActionAsset;

        private InputAction toggleInputAction;
        private InputAction cancelAction;
        private InputActionMap playerActionMap;
        private bool isInputFieldFocused = false;

        void Awake()
        {
            Debug.Log("RadioInputManager: Awake() called");

            if (inputField == null)
            {
                Debug.LogError("RadioInputManager: InputField is not assigned!");
                return;
            }
            else
            {
                Debug.Log("RadioInputManager: InputField assigned successfully");
            }

            if (inputActionAsset == null)
            {
                Debug.LogError("RadioInputManager: InputActionAsset is not assigned!");
                return;
            }
            else
            {
                Debug.Log("RadioInputManager: InputActionAsset assigned successfully");
            }

            // Set up input actions
            SetupInputActions();
            Debug.Log("RadioInputManager: Input actions created");
        }

        void Start()
        {
            Debug.Log("RadioInputManager: Start() called");

            // Ensure input field starts unfocused
            if (inputField != null)
            {
                inputField.DeactivateInputField();
            }
            Debug.Log("RadioInputManager: Initialization complete");
        }

        void OnEnable()
        {
            Debug.Log("RadioInputManager: OnEnable() called");

            if (toggleInputAction != null)
            {
                toggleInputAction.Enable();
                toggleInputAction.performed += OnToggleInputPerformed;
                Debug.Log("RadioInputManager: Toggle input action enabled and subscribed");
            }
            else
            {
                Debug.LogWarning("RadioInputManager: toggleInputAction is null in OnEnable");
            }

            if (cancelAction != null)
            {
                cancelAction.Enable();
                cancelAction.performed += OnCancelPerformed;
                Debug.Log("RadioInputManager: Cancel action enabled and subscribed");
            }
            else
            {
                Debug.LogWarning("RadioInputManager: cancelAction is null in OnEnable");
            }
        }

        void OnDisable()
        {
            if (toggleInputAction != null)
            {
                toggleInputAction.performed -= OnToggleInputPerformed;
                toggleInputAction.Disable();
            }

            if (cancelAction != null)
            {
                cancelAction.performed -= OnCancelPerformed;
                cancelAction.Disable();
            }
        }

        private void SetupInputActions()
        {
            Debug.Log("RadioInputManager: SetupInputActions() called");

            // Create a new action map for UI input (or use existing Player map)
            playerActionMap = inputActionAsset.FindActionMap("Player");

            if (playerActionMap == null)
            {
                Debug.LogError("RadioInputManager: Could not find 'Player' action map!");
                return;
            }
            else
            {
                Debug.Log($"RadioInputManager: Found Player action map: {playerActionMap.name}");
            }

            // Try to find existing action or create inline actions
            // For Enter key toggle
            toggleInputAction = new InputAction(
                name: "ToggleRadioInput",
                type: InputActionType.Button,
                binding: "<Keyboard>/enter"
            );
            Debug.Log("RadioInputManager: Created toggleInputAction for Enter key");

            // For Escape key cancel
            cancelAction = new InputAction(
                name: "CancelRadioInput",
                type: InputActionType.Button,
                binding: "<Keyboard>/escape"
            );
            Debug.Log("RadioInputManager: Created cancelAction for Escape key");
        }

        private void OnToggleInputPerformed(InputAction.CallbackContext context)
        {
            Debug.Log($"RadioInputManager: Enter key pressed! isInputFieldFocused={isInputFieldFocused}");

            if (!isInputFieldFocused)
            {
                // Enter pressed while game has focus - focus the input field
                Debug.Log("RadioInputManager: Focusing input field");
                FocusInputField();
            }
            else
            {
                // Enter pressed while input field has focus - send message
                // Only send if there's text to send
                if (!string.IsNullOrWhiteSpace(inputField.text))
                {
                    Debug.Log("RadioInputManager: Sending message");
                    SendMessage();
                }
                else
                {
                    // If input is empty, just unfocus
                    Debug.Log("RadioInputManager: Input empty, unfocusing");
                    UnfocusInputField();
                }
            }
        }

        private void OnCancelPerformed(InputAction.CallbackContext context)
        {
            // Allow Escape to cancel input field focus
            if (isInputFieldFocused)
            {
                UnfocusInputField();
            }
        }

        /// <summary>
        /// Focus the input field and unlock cursor (but keep invisible)
        /// </summary>
        public void FocusInputField()
        {
            isInputFieldFocused = true;

            // Disable player action map to prevent game controls while typing
            if (playerActionMap != null)
            {
                playerActionMap.Disable();
            }

            // Unlock cursor but keep it invisible
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;

            // Focus the input field
            inputField.ActivateInputField();
            inputField.Select();

            // Move caret to end of text
            inputField.caretPosition = inputField.text.Length;
        }

        /// <summary>
        /// Unfocus the input field and lock cursor back to game
        /// </summary>
        public void UnfocusInputField()
        {
            isInputFieldFocused = false;

            // Deactivate input field
            inputField.DeactivateInputField();
            EventSystem.current.SetSelectedGameObject(null);

            // Re-enable player action map for game controls
            if (playerActionMap != null)
            {
                playerActionMap.Enable();
            }

            // Lock cursor back to game
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>
        /// Send the message and return focus to game
        /// </summary>
        private void SendMessage()
        {
            string message = inputField.text;

            if (string.IsNullOrWhiteSpace(message))
            {
                UnfocusInputField();
                return;
            }

            // TODO: Send message to active NPC via RadioManager
            Debug.Log($"Sending message: {message}");

            // Clear the input field
            inputField.text = "";

            // Return focus to game
            UnfocusInputField();

            // Here you would call your RadioManager to actually send the message
            // Example: Get RadioManager and send to active NPC
            RadioManager radioManager = FindObjectOfType<RadioManager>();
            if (radioManager != null)
            {
                // radioManager.SendMessageToActiveNPC(message);
                Debug.Log("RadioManager found - ready to send message when implemented");
            }
        }

        /// <summary>
        /// Check if input field currently has focus
        /// </summary>
        public bool IsInputFieldFocused()
        {
            return isInputFieldFocused;
        }

        /// <summary>
        /// Public method to manually send a message (useful for UI buttons)
        /// </summary>
        public void SendMessageButton()
        {
            if (!string.IsNullOrWhiteSpace(inputField.text))
            {
                SendMessage();
            }
        }
    }
}
