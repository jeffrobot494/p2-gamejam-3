using UnityEngine;
using UnityEngine.InputSystem;

namespace EpsilonIV
{
    /// <summary>
    /// Keyboard shortcuts for testing survivor state changes using new Input System
    /// Press F9 to activate survivor without losing focus
    /// Self-contained - no external InputActionAsset needed
    /// </summary>
    public class SurvivorTestKeys : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The survivor to control with keyboard shortcuts")]
        public Survivor targetSurvivor;

        private InputAction activateAction;
        private InputAction waitingAction;
        private InputAction rescueAction;

        void Awake()
        {
            // Create input actions programmatically
            activateAction = new InputAction(
                name: "ActivateSurvivor",
                type: InputActionType.Button,
                binding: "<Keyboard>/f9"
            );

            waitingAction = new InputAction(
                name: "SetSurvivorWaiting",
                type: InputActionType.Button,
                binding: "<Keyboard>/f7"
            );

            rescueAction = new InputAction(
                name: "RescueSurvivor",
                type: InputActionType.Button,
                binding: "<Keyboard>/f8"
            );
        }

        void OnEnable()
        {
            if (activateAction != null)
            {
                activateAction.Enable();
                activateAction.performed += OnActivatePressed;
            }

            if (waitingAction != null)
            {
                waitingAction.Enable();
                waitingAction.performed += OnWaitingPressed;
            }

            if (rescueAction != null)
            {
                rescueAction.Enable();
                rescueAction.performed += OnRescuePressed;
            }

            if (targetSurvivor == null)
            {
                Debug.LogWarning("[SurvivorTestKeys] No target survivor assigned!");
            }
        }

        void OnDisable()
        {
            if (activateAction != null)
            {
                activateAction.performed -= OnActivatePressed;
                activateAction.Disable();
            }

            if (waitingAction != null)
            {
                waitingAction.performed -= OnWaitingPressed;
                waitingAction.Disable();
            }

            if (rescueAction != null)
            {
                rescueAction.performed -= OnRescuePressed;
                rescueAction.Disable();
            }
        }

        private void OnActivatePressed(InputAction.CallbackContext context)
        {
            if (targetSurvivor == null)
            {
                Debug.LogError("[SurvivorTestKeys] Cannot activate - no target survivor assigned!");
                return;
            }

            Debug.Log($"[SurvivorTestKeys] F9 pressed - Activating survivor: {targetSurvivor.gameObject.name}");
            targetSurvivor.SetState(SurvivorState.Active);
        }

        private void OnWaitingPressed(InputAction.CallbackContext context)
        {
            if (targetSurvivor == null)
            {
                Debug.LogError("[SurvivorTestKeys] Cannot set waiting - no target survivor assigned!");
                return;
            }

            Debug.Log($"[SurvivorTestKeys] F7 pressed - Setting survivor to Waiting: {targetSurvivor.gameObject.name}");
            targetSurvivor.SetState(SurvivorState.Waiting);
        }

        private void OnRescuePressed(InputAction.CallbackContext context)
        {
            if (targetSurvivor == null)
            {
                Debug.LogError("[SurvivorTestKeys] Cannot rescue - no target survivor assigned!");
                return;
            }

            Debug.Log($"[SurvivorTestKeys] F8 pressed - Rescuing survivor: {targetSurvivor.gameObject.name}");
            targetSurvivor.Rescue();
        }
    }
}
