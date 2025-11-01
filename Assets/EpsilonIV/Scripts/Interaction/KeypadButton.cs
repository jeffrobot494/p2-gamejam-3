using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Individual button on a keypad
    /// Communicates button presses to the parent KeypadComputer
    /// </summary>
    public class KeypadButton : MonoBehaviour, IInteractable
    {
        [Header("Button Settings")]
        [Tooltip("Value of this button ('0'-'9' or 'Clear')")]
        public string ButtonValue = "0";

        [Tooltip("Reference to the parent KeypadComputer")]
        public KeypadComputer ParentKeypad;

        [Header("Visual Feedback")]
        [Tooltip("Enable visual feedback when pressed")]
        public bool EnableVisualFeedback = true;

        [Tooltip("Renderer to highlight when pressed")]
        public Renderer ButtonRenderer;

        [Tooltip("Material to use when button is pressed (optional)")]
        public Material PressedMaterial;

        [Tooltip("Duration of press visual feedback")]
        public float PressVisualDuration = 0.1f;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool DebugMode = false;

        private Material m_OriginalMaterial;
        private bool m_IsPressed = false;

        void Start()
        {
            // Auto-find parent keypad if not set
            if (ParentKeypad == null)
            {
                ParentKeypad = GetComponentInParent<KeypadComputer>();
            }

            if (ParentKeypad == null)
            {
                Debug.LogError($"[KeypadButton] No KeypadComputer found for button '{ButtonValue}' on {gameObject.name}");
            }

            // Store original material
            if (ButtonRenderer != null && ButtonRenderer.material != null)
            {
                m_OriginalMaterial = ButtonRenderer.material;
            }
        }

        public void Interact()
        {
            if (ParentKeypad == null)
            {
                Debug.LogWarning($"[KeypadButton] Cannot interact - no parent keypad assigned");
                return;
            }

            if (m_IsPressed)
            {
                if (DebugMode)
                {
                    Debug.Log($"[KeypadButton] Button '{ButtonValue}' already pressed, ignoring");
                }
                return;
            }

            if (DebugMode)
            {
                Debug.Log($"[KeypadButton] Button '{ButtonValue}' pressed");
            }

            // Notify parent keypad
            ParentKeypad.OnButtonPressed(ButtonValue);

            // Visual feedback
            if (EnableVisualFeedback)
            {
                StartCoroutine(PressButtonFeedback());
            }
        }

        public Transform GetTransform()
        {
            return transform;
        }

        public string GetInteractionPrompt()
        {
            if (ButtonValue == "Clear")
            {
                return "[E] CLEAR";
            }

            return $"[E] {ButtonValue}";
        }

        /// <summary>
        /// Visual feedback when button is pressed
        /// </summary>
        System.Collections.IEnumerator PressButtonFeedback()
        {
            m_IsPressed = true;

            // Change material if available
            if (ButtonRenderer != null && PressedMaterial != null)
            {
                ButtonRenderer.material = PressedMaterial;
            }

            // Wait for press duration
            yield return new WaitForSeconds(PressVisualDuration);

            // Restore original material
            if (ButtonRenderer != null && m_OriginalMaterial != null)
            {
                ButtonRenderer.material = m_OriginalMaterial;
            }

            m_IsPressed = false;
        }
    }
}
