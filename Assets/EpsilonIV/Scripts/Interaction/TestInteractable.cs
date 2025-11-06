using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Simple test interactable object that logs when interacted with
    /// </summary>
    public class TestInteractable : MonoBehaviour, IInteractable
    {
        [Header("Interaction Settings")]
        [Tooltip("Custom prompt text (leave empty for default)")]
        public string CustomPrompt = "";

        [Tooltip("Enable debug logging")]
        public bool DebugMode = true;

        [Header("Visual Feedback")]
        [Tooltip("Change color when interacted with")]
        public bool ChangeColorOnInteract = true;

        [Tooltip("Color to change to when interacted")]
        public Color InteractedColor = Color.cyan;

        private int m_InteractionCount = 0;
        private Renderer m_Renderer;
        private Color m_OriginalColor;
        private bool m_HasInteracted = false;

        void Start()
        {
            m_Renderer = GetComponent<Renderer>();
            if (m_Renderer != null && m_Renderer.material != null)
            {
                m_OriginalColor = m_Renderer.material.color;
            }
        }

        public void Interact()
        {
            m_InteractionCount++;

            if (DebugMode)
            {
                Debug.Log($"[TestInteractable] '{gameObject.name}' interacted with! (Count: {m_InteractionCount})");
            }

            // Visual feedback
            if (ChangeColorOnInteract && m_Renderer != null && !m_HasInteracted)
            {
                m_Renderer.material.color = InteractedColor;
                m_HasInteracted = true;
            }
        }

        public Transform GetTransform()
        {
            return transform;
        }

        public string GetInteractionPrompt()
        {
            if (!string.IsNullOrEmpty(CustomPrompt))
            {
                return CustomPrompt;
            }

            return null; // Use default prompt
        }

        // Optional: Reset color after a delay
        public void ResetColor()
        {
            if (m_Renderer != null && m_HasInteracted)
            {
                m_Renderer.material.color = m_OriginalColor;
                m_HasInteracted = false;
            }
        }
    }
}
