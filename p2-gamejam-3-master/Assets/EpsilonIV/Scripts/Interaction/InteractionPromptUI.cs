using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EpsilonIV
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

        [Header("Adaptive Scaling")]
        [Tooltip("Enable adaptive bracket scaling based on object size")]
        public bool EnableAdaptiveScaling = true;

        [Tooltip("Reference object size (e.g., player height in meters) - objects this size or larger get full scale")]
        public float ReferenceObjectSize = 1.8f;

        [Tooltip("Minimum scale for brackets (for very small objects)")]
        [Range(0.1f, 1f)]
        public float MinBracketScale = 0.1f;

        [Tooltip("Maximum scale for brackets")]
        [Range(0.1f, 2f)]
        public float MaxBracketScale = 1f;

        [Header("Animation")]
        [Tooltip("Enable bracket pulsing animation")]
        public bool EnablePulse = true;

        [Tooltip("Pulse speed")]
        public float PulseSpeed = 2f;

        [Tooltip("Pulse scale range")]
        public Vector2 PulseScaleRange = new Vector2(0.9f, 1.1f);

        [Tooltip("Smoothing factor for bracket movement (0 = no smoothing, higher = smoother)")]
        public float PositionSmoothness = 15f;

        [Header("Glitch Effects")]
        [Tooltip("Enable glitch/jitter effects")]
        public bool EnableGlitch = true;

        [Tooltip("Maximum random offset for glitch effect (in pixels)")]
        public float GlitchIntensity = 2f;

        [Tooltip("How often glitch happens (lower = more frequent)")]
        public float GlitchFrequency = 0.1f;

        [Tooltip("Enable random flicker effect")]
        public bool EnableFlicker = true;

        [Tooltip("Chance of flicker per frame (0-1)")]
        [Range(0f, 0.1f)]
        public float FlickerChance = 0.02f;

        [Tooltip("Minimum alpha during flicker")]
        [Range(0.5f, 1f)]
        public float FlickerMinAlpha = 0.7f;

        private CanvasGroup m_CanvasGroup;
        private float m_PulseTime = 0f;
        private Vector2 m_TargetTopLeft;
        private Vector2 m_TargetTopRight;
        private Vector2 m_TargetBottomLeft;
        private Vector2 m_TargetBottomRight;
        private Vector2 m_TargetCenter;
        private float m_NextGlitchTime = 0f;
        private Vector2[] m_GlitchOffsets = new Vector2[4];
        private float m_FlickerAlpha = 1f;
        private float m_CurrentAdaptiveScale = 1f;

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
                else if (EnableAdaptiveScaling)
                {
                    // Apply adaptive scale without pulse animation
                    ApplyAdaptiveScale();
                }

                if (EnableGlitch)
                {
                    ApplyGlitchEffect();
                }

                if (EnableFlicker)
                {
                    ApplyFlickerEffect();
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
                m_FlickerAlpha = 1f;
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
                // Try to get collider bounds as fallback
                Collider targetCollider = targetTransform.GetComponent<Collider>();
                if (targetCollider != null)
                {
                    bounds = targetCollider.bounds;
                }
                else
                {
                    // Last resort: try to find renderer in children
                    Renderer childRenderer = targetTransform.GetComponentInChildren<Renderer>();
                    if (childRenderer != null)
                    {
                        bounds = childRenderer.bounds;
                    }
                    else
                    {
                        // Final fallback: use a small default size
                        bounds = new Bounds(targetTransform.position, Vector3.one * 0.2f);
                    }
                }
            }

            // Calculate adaptive scale based on object size
            if (EnableAdaptiveScaling)
            {
                // Get the largest dimension of the bounds
                float objectSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

                // Calculate scale factor relative to reference size
                float scaleFactor = objectSize / ReferenceObjectSize;

                // Clamp between min and max scale
                m_CurrentAdaptiveScale = Mathf.Clamp(scaleFactor, MinBracketScale, MaxBracketScale);
            }
            else
            {
                m_CurrentAdaptiveScale = 1f;
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
                BracketTopLeft.position = Vector2.Lerp(current, m_TargetTopLeft + m_GlitchOffsets[0], smoothFactor);
            }

            if (BracketTopRight != null)
            {
                Vector2 current = BracketTopRight.position;
                BracketTopRight.position = Vector2.Lerp(current, m_TargetTopRight + m_GlitchOffsets[1], smoothFactor);
            }

            if (BracketBottomLeft != null)
            {
                Vector2 current = BracketBottomLeft.position;
                BracketBottomLeft.position = Vector2.Lerp(current, m_TargetBottomLeft + m_GlitchOffsets[2], smoothFactor);
            }

            if (BracketBottomRight != null)
            {
                Vector2 current = BracketBottomRight.position;
                BracketBottomRight.position = Vector2.Lerp(current, m_TargetBottomRight + m_GlitchOffsets[3], smoothFactor);
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
            float pulseScale = Mathf.Lerp(PulseScaleRange.x, PulseScaleRange.y, (Mathf.Sin(m_PulseTime) + 1f) * 0.5f);

            // Combine pulse scale with adaptive scale
            float finalScale = pulseScale * m_CurrentAdaptiveScale;

            if (BracketTopLeft != null)
                BracketTopLeft.localScale = Vector3.one * finalScale;
            if (BracketTopRight != null)
                BracketTopRight.localScale = Vector3.one * finalScale;
            if (BracketBottomLeft != null)
                BracketBottomLeft.localScale = Vector3.one * finalScale;
            if (BracketBottomRight != null)
                BracketBottomRight.localScale = Vector3.one * finalScale;
        }

        void ApplyAdaptiveScale()
        {
            // Apply only the adaptive scale (no pulse animation)
            if (BracketTopLeft != null)
                BracketTopLeft.localScale = Vector3.one * m_CurrentAdaptiveScale;
            if (BracketTopRight != null)
                BracketTopRight.localScale = Vector3.one * m_CurrentAdaptiveScale;
            if (BracketBottomLeft != null)
                BracketBottomLeft.localScale = Vector3.one * m_CurrentAdaptiveScale;
            if (BracketBottomRight != null)
                BracketBottomRight.localScale = Vector3.one * m_CurrentAdaptiveScale;
        }

        void ApplyGlitchEffect()
        {
            // Apply random offset at intervals
            if (Time.time >= m_NextGlitchTime)
            {
                m_NextGlitchTime = Time.time + GlitchFrequency;

                // Generate random offsets for each bracket
                for (int i = 0; i < 4; i++)
                {
                    m_GlitchOffsets[i] = new Vector2(
                        Random.Range(-GlitchIntensity, GlitchIntensity),
                        Random.Range(-GlitchIntensity, GlitchIntensity)
                    );
                }
            }
        }

        void ApplyFlickerEffect()
        {
            // Random chance to flicker
            if (Random.value < FlickerChance)
            {
                m_FlickerAlpha = Random.Range(FlickerMinAlpha, 1f);
            }
            else
            {
                // Smoothly return to full opacity
                m_FlickerAlpha = Mathf.Lerp(m_FlickerAlpha, 1f, Time.deltaTime * 10f);
            }

            // Apply flicker to canvas group
            m_CanvasGroup.alpha = m_FlickerAlpha;
        }
    }
}
