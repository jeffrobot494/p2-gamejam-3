using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EpsilonIV
{
    /// <summary>
    /// Manages the input field's focus state and cursor locking.
    /// Provides public methods to focus, unfocus, and clear the input field.
    /// Does NOT handle input detection - that's RadioInputHandler's job.
    /// </summary>
    public class InputFieldController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The input field for typing messages")]
        public TMP_InputField inputField;

        [Tooltip("Input Action Asset containing player controls")]
        public InputActionAsset inputActionAsset;

        private InputActionMap playerActionMap;
        private bool isInputFieldFocused = false;

        void Awake()
        {
            Debug.Log("InputFieldController: Awake() called");

            if (inputField == null)
            {
                Debug.LogError("InputFieldController: InputField is not assigned!");
            }

            if (inputActionAsset == null)
            {
                Debug.LogError("InputFieldController: InputActionAsset is not assigned!");
                return;
            }

            // Find the Player action map for disabling/enabling during input
            playerActionMap = inputActionAsset.FindActionMap("Player");

            if (playerActionMap == null)
            {
                Debug.LogError("InputFieldController: Could not find 'Player' action map!");
            }
            else
            {
                Debug.Log($"InputFieldController: Found Player action map: {playerActionMap.name}");
            }
        }

        void Start()
        {
            Debug.Log("InputFieldController: Start() called");

            // Ensure input field starts unfocused
            if (inputField != null)
            {
                inputField.DeactivateInputField();
            }

            Debug.Log("InputFieldController: Initialization complete");
        }

        /// <summary>
        /// Focus the input field and unlock cursor (but keep invisible).
        /// Called by RadioInputHandler when player presses Enter.
        /// </summary>
        public void FocusInputField()
        {
            if (inputField == null)
            {
                Debug.LogError("InputFieldController: Cannot focus - inputField is null");
                return;
            }

            Debug.Log("InputFieldController: Focusing input field");
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
        /// Unfocus the input field and lock cursor back to game.
        /// Called by RadioInputHandler after message is submitted or canceled.
        /// </summary>
        public void UnfocusInputField()
        {
            Debug.Log("InputFieldController: Unfocusing input field");
            isInputFieldFocused = false;

            // Deactivate input field
            if (inputField != null)
            {
                inputField.DeactivateInputField();
            }

            EventSystem.current?.SetSelectedGameObject(null);

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
        /// Clear the text in the input field.
        /// Called by RadioInputHandler after message is submitted.
        /// </summary>
        public void ClearInputField()
        {
            if (inputField != null)
            {
                Debug.Log("InputFieldController: Clearing input field");
                inputField.text = "";
            }
        }

        /// <summary>
        /// Check if input field currently has focus.
        /// </summary>
        public bool IsInputFieldFocused()
        {
            return isInputFieldFocused;
        }

        /// <summary>
        /// Get the current text from the input field.
        /// </summary>
        public string GetInputText()
        {
            if (inputField != null)
            {
                return inputField.text;
            }
            return string.Empty;
        }
    }
}
