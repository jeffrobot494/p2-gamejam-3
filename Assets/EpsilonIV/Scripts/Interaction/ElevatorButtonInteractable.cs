using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Makes an elevator button interactable.
    /// When the player interacts, it triggers the elevator movement.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ElevatorButtonInteractable : MonoBehaviour, IInteractable
    {
        [Header("Interaction")]
        [Tooltip("Text shown when elevator can be triggered")]
        public string Prompt = "[E] CALL ELEVATOR";

        [Tooltip("Text shown while elevator is moving or during delay")]
        public string BusyPrompt = "ELEVATOR MOVING...";

        [Header("Elevator Reference")]
        [Tooltip("Reference to the ElevatorController script on the elevator GameObject")]
        [SerializeField] private ElevatorController elevator;

        [Header("Debug")]
        public bool DebugMode = false;

        private bool isBusy = false;

        public void Interact()
        {
            if (elevator == null)
            {
                Debug.LogError($"[ElevatorButtonInteractable] No ElevatorController assigned on {gameObject.name}!");
                return;
            }

            if (isBusy)
            {
                if (DebugMode)
                    Debug.Log("[ElevatorButtonInteractable] Elevator is already moving.");
                return;
            }

            if (DebugMode)
                Debug.Log("[ElevatorButtonInteractable] Button pressed, starting elevator delay.");

            // Start the elevator coroutine
            StartCoroutine(ActivateElevator());
        }

        private System.Collections.IEnumerator ActivateElevator()
        {
            isBusy = true;

            // Trigger elevator
            elevator.TriggerElevator();

            // Wait for the elevator's delay + estimated movement time (optional)
            yield return new WaitForSeconds(3.5f);

            isBusy = false;
        }

        public Transform GetTransform()
        {
            return transform;
        }

        public string GetInteractionPrompt()
        {
            return isBusy ? BusyPrompt : Prompt;
        }
    }
}
