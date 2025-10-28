using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Displays corner brackets around interactable objects with a centered prompt
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The PlayerInteraction component to monitor")]
        public PlayerInteraction PlayerInteraction;

        [Tooltip("Camera used for world-to-screen calculations")]
        public Camera PlayerCamera;

        [Header("UI Elements")]
        [Tooltip("Top-left bracket image")]
        public RectTransform BracketTopLeft;

        [Tooltip("Top-right bracket image")]
        public RectTransform BracketTopRight;

        [Tooltip("Bottom-left bracket image")]
        public RectTransform BracketBottomLeft;

        [Tooltip("Bottom-right bracket image")]
        public RectTransform BracketBottomRight;

        [Tooltip("Center text for interaction prompt")]
        public TextMeshProUGUI PromptText;

        [Header("Settings")]
        [Tooltip("Padding from the object bounds (in screen space pixels)")]
        public float BracketPadding = 20f;

        [Tooltip("Default prompt text")]
        public string DefaultPromptText = "[E] INTERACT";

        [Tooltip("Size of the brackets")]
        public float BracketSize = 30f;

        [Header("Animation")]
        [Tooltip("Enable bracket pulsing animation")]
        public bool EnablePulse = true;

        [Tooltip("Pulse speed")]
        public float PulseSpeed = 2f;

        [Tooltip("Pulse scale range")]
        public Vector2 PulseScaleRange = new Vector2(0.9f, 1.1f);

        [Tooltip("Smoothing factor for bracket movement (0 = no smoothing, higher = smoother)")]
        public float PositionSmoothness = 15f;

        private CanvasGroup m_CanvasGroup;
        private float m_PulseTime = 0f;
        private Vector2 m_TargetTopLeft;
        private Vector2 m_TargetTopRight;
        private Vector2 m_TargetBottomLeft;
        private Vector2 m_TargetBottomRight;
        private Vector2 m_TargetCenter;

        void Start()
        {
            m_CanvasGroup = GetComponent<CanvasGroup>();
            if (m_CanvasGroup == null)
            {
                m_CanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Start hidden
            HidePrompt();

            // Auto-find references if not set
            if (PlayerInteraction == null)
            {
                PlayerInteraction = FindFirstObjectByType<PlayerInteraction>();
            }

            if (PlayerCamera == null && PlayerInteraction != null)
            {
                PlayerCamera = PlayerInteraction.PlayerCamera;
            }

            // Validate
            if (PlayerInteraction == null)
            {
                Debug.LogError("InteractionPromptUI: PlayerInteraction not assigned!");
            }

            if (PlayerCamera == null)
            {
                Debug.LogError("InteractionPromptUI: PlayerCamera not assigned!");
            }
        }

        void Update()
        {
            IInteractable currentInteractable = PlayerInteraction?.GetCurrentInteractable();

            if (currentInteractable != null)
            {
                ShowPrompt(currentInteractable);
                UpdateBracketPositions(currentInteractable);

                if (EnablePulse)
                {
                    AnimatePulse();
                }
            }
            else
            {
                HidePrompt();
            }
        }

        void ShowPrompt(IInteractable interactable)
        {
            if (m_CanvasGroup.alpha < 1f)
            {
                m_CanvasGroup.alpha = 1f;
            }

            // Update prompt text
            string promptText = interactable.GetInteractionPrompt();
            if (string.IsNullOrEmpty(promptText))
            {
                promptText = DefaultPromptText;
            }

            if (PromptText != null)
            {
                PromptText.text = promptText;
            }
        }

        void HidePrompt()
        {
            if (m_CanvasGroup.alpha > 0f)
            {
                m_CanvasGroup.alpha = 0f;
                m_PulseTime = 0f;
            }
        }

        void UpdateBracketPositions(IInteractable interactable)
        {
            Transform targetTransform = interactable.GetTransform();
            if (targetTransform == null)
                return;

            // Get the object's renderer bounds
            Renderer targetRenderer = targetTransform.GetComponent<Renderer>();
            Bounds bounds;

            if (targetRenderer != null)
            {
                bounds = targetRenderer.bounds;
            }
            else
            {
                // Fallback: use a default size around the object's position
                bounds = new Bounds(targetTransform.position, Vector3.one);
            }

            // Get the 8 corners of the bounding box
            Vector3[] worldCorners = new Vector3[8];
            worldCorners[0] = bounds.min;
            worldCorners[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
            worldCorners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
            worldCorners[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
            worldCorners[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
            worldCorners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
            worldCorners[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
            worldCorners[7] = bounds.max;

            // Convert all corners to screen space and find min/max
            Vector2 min = Vector2.one * float.MaxValue;
            Vector2 max = Vector2.one * float.MinValue;

            foreach (Vector3 corner in worldCorners)
            {
                Vector3 screenPoint = PlayerCamera.WorldToScreenPoint(corner);

                // Skip if behind camera
                if (screenPoint.z < 0)
                    continue;

                min = Vector2.Min(min, new Vector2(screenPoint.x, screenPoint.y));
                max = Vector2.Max(max, new Vector2(screenPoint.x, screenPoint.y));
            }

            // Add padding
            min -= Vector2.one * BracketPadding;
            max += Vector2.one * BracketPadding;

            // Calculate target positions
            m_TargetTopLeft = new Vector2(min.x, max.y);
            m_TargetTopRight = new Vector2(max.x, max.y);
            m_TargetBottomLeft = new Vector2(min.x, min.y);
            m_TargetBottomRight = new Vector2(max.x, min.y);
            m_TargetCenter = (min + max) * 0.5f;

            // Smoothly interpolate to target positions
            float smoothFactor = PositionSmoothness * Time.deltaTime;

            if (BracketTopLeft != null)
            {
                Vector2 current = BracketTopLeft.position;
                BracketTopLeft.position = Vector2.Lerp(current, m_TargetTopLeft, smoothFactor);
            }

            if (BracketTopRight != null)
            {
                Vector2 current = BracketTopRight.position;
                BracketTopRight.position = Vector2.Lerp(current, m_TargetTopRight, smoothFactor);
            }

            if (BracketBottomLeft != null)
            {
                Vector2 current = BracketBottomLeft.position;
                BracketBottomLeft.position = Vector2.Lerp(current, m_TargetBottomLeft, smoothFactor);
            }

            if (BracketBottomRight != null)
            {
                Vector2 current = BracketBottomRight.position;
                BracketBottomRight.position = Vector2.Lerp(current, m_TargetBottomRight, smoothFactor);
            }

            // Center the prompt text
            if (PromptText != null)
            {
                Vector2 current = PromptText.transform.position;
                PromptText.transform.position = Vector2.Lerp(current, m_TargetCenter, smoothFactor);
            }
        }

        void AnimatePulse()
        {
            m_PulseTime += Time.deltaTime * PulseSpeed;
            float scale = Mathf.Lerp(PulseScaleRange.x, PulseScaleRange.y, (Mathf.Sin(m_PulseTime) + 1f) * 0.5f);

            if (BracketTopLeft != null)
                BracketTopLeft.localScale = Vector3.one * scale;
            if (BracketTopRight != null)
                BracketTopRight.localScale = Vector3.one * scale;
            if (BracketBottomLeft != null)
                BracketBottomLeft.localScale = Vector3.one * scale;
            if (BracketBottomRight != null)
                BracketBottomRight.localScale = Vector3.one * scale;
        }
    }
}
