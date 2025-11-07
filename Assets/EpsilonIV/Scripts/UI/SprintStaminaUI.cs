using UnityEngine;
using UnityEngine.UI;

namespace EpsilonIV
{
    /// <summary>
    /// UI display for sprint stamina bar.
    /// Fills a horizontal image based on stamina percent.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SprintStaminaUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The SprintStaminaManager to monitor")]
        [SerializeField] private SprintStaminaManager staminaManager;

        [Header("Visual Settings")]
        [Tooltip("Color when stamina is full or recharging")]
        [SerializeField] private Color normalColor = Color.white;

        [Tooltip("Color when stamina is depleted")]
        [SerializeField] private Color depletedColor = Color.red;

        [Tooltip("Smooth the fill amount changes")]
        [SerializeField] private bool smoothFill = true;

        [Tooltip("Speed of smooth fill transition")]
        [SerializeField] private float smoothSpeed = 5f;

        [Header("Auto-Hide")]
        [Tooltip("Hide the bar when stamina is full")]
        [SerializeField] private bool hideWhenFull = true;

        [Tooltip("Fade duration when showing/hiding")]
        [SerializeField] private float fadeDuration = 0.3f;

        private Image fillImage;
        private CanvasGroup canvasGroup;
        private float targetFillAmount = 1f;
        private float currentFillAmount = 1f;
        private bool isDepleted = false;

        void Awake()
        {
            fillImage = GetComponent<Image>();

            // Get or add CanvasGroup for fading
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null && hideWhenFull)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Auto-find stamina manager if not assigned
            if (staminaManager == null)
            {
                staminaManager = FindFirstObjectByType<SprintStaminaManager>();
            }

            if (staminaManager == null)
            {
                Debug.LogError("[SprintStaminaUI] No SprintStaminaManager found! Assign one in the inspector or add to scene.");
            }
        }

        void Start()
        {
            if (staminaManager != null)
            {
                // Subscribe to stamina events
                staminaManager.OnStaminaChanged.AddListener(OnStaminaChanged);
                staminaManager.OnStaminaDepleted.AddListener(OnStaminaDepleted);
                staminaManager.OnStaminaRecharged.AddListener(OnStaminaRecharged);

                // Initialize
                fillImage.fillAmount = 1f;
                currentFillAmount = 1f;
                targetFillAmount = 1f;
                fillImage.color = normalColor;

                // Start hidden if configured
                if (hideWhenFull && canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
            }
        }

        void OnDestroy()
        {
            if (staminaManager != null)
            {
                staminaManager.OnStaminaChanged.RemoveListener(OnStaminaChanged);
                staminaManager.OnStaminaDepleted.RemoveListener(OnStaminaDepleted);
                staminaManager.OnStaminaRecharged.RemoveListener(OnStaminaRecharged);
            }
        }

        void Update()
        {
            // Smooth fill amount
            if (smoothFill)
            {
                currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * smoothSpeed);
                fillImage.fillAmount = currentFillAmount;
            }
            else
            {
                fillImage.fillAmount = targetFillAmount;
            }

            // Handle auto-hide
            if (hideWhenFull && canvasGroup != null)
            {
                float targetAlpha = (targetFillAmount >= 1f) ? 0f : 1f;
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime / fadeDuration);
            }

            // Update color based on depleted state
            fillImage.color = isDepleted ? depletedColor : normalColor;
        }

        private void OnStaminaChanged(float staminaPercent)
        {
            targetFillAmount = Mathf.Clamp01(staminaPercent);
        }

        private void OnStaminaDepleted()
        {
            isDepleted = true;
        }

        private void OnStaminaRecharged()
        {
            isDepleted = false;
        }

        /// <summary>
        /// Manually set the fill amount (for testing)
        /// </summary>
        public void SetFillAmount(float amount)
        {
            targetFillAmount = Mathf.Clamp01(amount);
        }
    }
}
