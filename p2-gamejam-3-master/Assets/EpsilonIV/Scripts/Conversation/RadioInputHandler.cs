using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;

namespace EpsilonIV
{
    /// <summary>
    /// Detects all player input related to radio communication.
    /// Fires events that other components can subscribe to.
    /// </summary>
    public class RadioInputHandler : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Controller that manages input field focus and provides text")]
        public InputFieldController inputFieldController;

        [Tooltip("MessageManager for STT control (Phase 6)")]
        public MessageManager messageManager;

        [Header("Input Configuration")]
        [Tooltip("Input Action Asset containing player controls")]
        public InputActionAsset inputActionAsset;

        [Header("Events")]
        [Tooltip("Fired when player submits a message (Enter key while focused)")]
        public UnityEvent<string> OnMessageSubmitted;

        [Tooltip("Fired when player requests STT (future - Phase 6)")]
        public UnityEvent OnSTTStartRequested;

        [Tooltip("Fired when player stops STT (future - Phase 6)")]
        public UnityEvent OnSTTStopRequested;

        private InputAction toggleInputAction;
        private InputAction cancelAction;
        private InputAction sttAction;  // Phase 6 - R key for voice input
        private bool isWaitingForMessage = false;

        void Awake()
        {
            if (inputFieldController == null)
            {
                Debug.LogError("RadioInputHandler: InputFieldController is not assigned!");
            }

            if (inputActionAsset == null)
            {
                Debug.LogError("RadioInputHandler: InputActionAsset is not assigned!");
            }

            SetupInputActions();
        }

        void OnEnable()
        {
            if (toggleInputAction != null)
            {
                toggleInputAction.Enable();
                toggleInputAction.performed += OnEnterPressed;
            }

            if (cancelAction != null)
            {
                cancelAction.Enable();
                cancelAction.performed += OnEscapePressed;
            }

            if (sttAction != null)
            {
                sttAction.Enable();
                sttAction.started += OnRKeyPressed;
                sttAction.canceled += OnRKeyReleased;
            }
        }

        void OnDisable()
        {
            if (toggleInputAction != null)
            {
                toggleInputAction.performed -= OnEnterPressed;
                toggleInputAction.Disable();
            }

            if (cancelAction != null)
            {
                cancelAction.performed -= OnEscapePressed;
                cancelAction.Disable();
            }

            if (sttAction != null)
            {
                sttAction.started -= OnRKeyPressed;
                sttAction.canceled -= OnRKeyReleased;
                sttAction.Disable();
            }
        }

        private void SetupInputActions()
        {
            if (inputActionAsset == null)
            {
                Debug.LogError("RadioInputHandler: Cannot setup input actions - InputActionAsset is null");
                return;
            }

            // Create Enter key action
            toggleInputAction = new InputAction(
                name: "ToggleRadioInput",
                type: InputActionType.Button,
                binding: "<Keyboard>/enter"
            );

            // Create Escape key action
            cancelAction = new InputAction(
                name: "CancelRadioInput",
                type: InputActionType.Button,
                binding: "<Keyboard>/escape"
            );

            // Create R key action for STT (Phase 6)
            sttAction = new InputAction(
                name: "PushToTalk",
                type: InputActionType.Button,
                binding: "<Keyboard>/r"
            );
        }

        private void OnEnterPressed(InputAction.CallbackContext context)
        {
            Debug.Log($"RadioInputHandler: Enter pressed. isWaitingForMessage={isWaitingForMessage}");

            if (!isWaitingForMessage)
            {
                // First press: Focus the input field
                Debug.Log("RadioInputHandler: Focusing input field");
                inputFieldController?.FocusInputField();
                isWaitingForMessage = true;
            }
            else
            {
                // Second press: Submit the message
                SubmitMessage();
            }
        }

        private void OnEscapePressed(InputAction.CallbackContext context)
        {
            if (isWaitingForMessage)
            {
                Debug.Log("RadioInputHandler: Escape pressed, canceling input");
                CancelInput();
            }
        }

        private void SubmitMessage()
        {
            if (inputFieldController == null)
            {
                Debug.LogError("RadioInputHandler: Cannot submit - inputFieldController is null");
                return;
            }

            // Get the text from the input field controller
            string message = inputFieldController.GetInputText();

            if (string.IsNullOrWhiteSpace(message))
            {
                Debug.Log("RadioInputHandler: Message is empty, just unfocusing");
                CancelInput();
                return;
            }

            Debug.Log($"RadioInputHandler: Submitting message: '{message}'");

            // Fire the event
            OnMessageSubmitted?.Invoke(message);

            // Clear and unfocus
            inputFieldController.ClearInputField();
            inputFieldController.UnfocusInputField();
            isWaitingForMessage = false;
        }

        private void CancelInput()
        {
            inputFieldController?.ClearInputField();
            inputFieldController?.UnfocusInputField();
            isWaitingForMessage = false;
        }

        // Phase 6 - STT Input Detection

        private void OnRKeyPressed(InputAction.CallbackContext context)
        {
            if (messageManager == null)
            {
                Debug.LogError("RadioInputHandler: Cannot start STT - messageManager is not assigned!");
                return;
            }

            Debug.Log("RadioInputHandler: R key pressed - starting STT");
            messageManager.StartSTT();
            OnSTTStartRequested?.Invoke();
        }

        private void OnRKeyReleased(InputAction.CallbackContext context)
        {
            if (messageManager == null)
            {
                Debug.LogError("RadioInputHandler: Cannot stop STT - messageManager is not assigned!");
                return;
            }

            Debug.Log("RadioInputHandler: R key released - stopping STT");
            messageManager.StopSTT();
            OnSTTStopRequested?.Invoke();
        }
    }
}
