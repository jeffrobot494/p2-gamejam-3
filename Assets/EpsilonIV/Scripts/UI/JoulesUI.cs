using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace EpsilonIV
{
    /// <summary>
    /// UI component that displays the player's joule balance.
    /// Listens to JoulesManager and updates text display.
    /// </summary>
    public class JoulesUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The JoulesManager to monitor")]
        [SerializeField] private JoulesManager joulesManager;

        [Tooltip("Text component to display joule count")]
        [SerializeField] private TextMeshProUGUI joulesText;

        [Tooltip("Optional: Icon image that changes color when joules are low")]
        [SerializeField] private Image joulesIcon;

        [Header("Display Settings")]
        [Tooltip("Format string for joules display. {0} = joule count")]
        [SerializeField] private string displayFormat = "⚡ {0} Joules";

        [Tooltip("Show patron tier in display")]
        [SerializeField] private bool showPatronTier = true;

        [Header("Visual Feedback")]
        [Tooltip("Color when joules are normal")]
        [SerializeField] private Color normalColor = Color.white;

        [Tooltip("Color when joules are low")]
        [SerializeField] private Color lowColor = Color.red;

        [Tooltip("Animate when joules change")]
        [SerializeField] private bool animateOnChange = true;

        [Tooltip("Scale pulse amount when joules change")]
        [SerializeField] private float pulseScale = 1.2f;

        [Tooltip("Duration of pulse animation")]
        [SerializeField] private float pulseDuration = 0.3f;

        private int displayedJoules = 0;
        private string displayedPatronTier = "";
        private Vector3 originalScale;
        private float pulseTimer = 0f;

        void Awake()
        {
            // Auto-find components if not assigned
            if (joulesManager == null)
            {
                joulesManager = FindFirstObjectByType<JoulesManager>();
            }

            if (joulesText == null)
            {
                joulesText = GetComponent<TextMeshProUGUI>();
            }

            if (joulesText == null)
            {
                Debug.LogError("[JoulesUI] No TextMeshProUGUI component found! Assign joulesText in inspector or add to this GameObject.");
            }

            if (joulesManager == null)
            {
                Debug.LogError("[JoulesUI] No JoulesManager found! Assign one in the inspector or add to scene.");
            }

            originalScale = transform.localScale;
        }

        void Start()
        {
            if (joulesManager != null)
            {
                // Subscribe to joules events
                joulesManager.OnJoulesUpdated.AddListener(OnJoulesUpdated);
                joulesManager.OnJoulesLow.AddListener(OnJoulesLow);

                // Initialize display with current values
                displayedJoules = joulesManager.GetCurrentJoules();
                displayedPatronTier = joulesManager.GetCurrentPatronTier();
                UpdateDisplay();
            }
        }

        void OnDestroy()
        {
            if (joulesManager != null)
            {
                joulesManager.OnJoulesUpdated.RemoveListener(OnJoulesUpdated);
                joulesManager.OnJoulesLow.RemoveListener(OnJoulesLow);
            }
        }

        void Update()
        {
            // Handle pulse animation
            if (pulseTimer > 0f)
            {
                pulseTimer -= Time.deltaTime;
                float t = pulseTimer / pulseDuration;
                float scale = Mathf.Lerp(1f, pulseScale, Mathf.Sin(t * Mathf.PI));
                transform.localScale = originalScale * scale;

                if (pulseTimer <= 0f)
                {
                    transform.localScale = originalScale;
                }
            }
        }

        private void OnJoulesUpdated(int joules, string patronTier)
        {
            displayedJoules = joules;
            displayedPatronTier = patronTier;
            UpdateDisplay();

            // Trigger pulse animation
            if (animateOnChange)
            {
                pulseTimer = pulseDuration;
            }
        }

        private void OnJoulesLow(int joules)
        {
            // Additional visual feedback for low joules
            if (joulesText != null)
            {
                joulesText.color = lowColor;
            }

            if (joulesIcon != null)
            {
                joulesIcon.color = lowColor;
            }
        }

        private void UpdateDisplay()
        {
            if (joulesText == null) return;

            // Format the joules text
            string displayText = string.Format(displayFormat, displayedJoules);

            // Add patron tier if enabled and present
            if (showPatronTier && !string.IsNullOrEmpty(displayedPatronTier))
            {
                displayText += $"\n<size=70%>{displayedPatronTier}</size>";
            }

            joulesText.text = displayText;

            // Update color based on joules level
            bool isLow = joulesManager != null && joulesManager.IsJoulesLow();
            Color targetColor = isLow ? lowColor : normalColor;

            if (joulesText != null)
            {
                joulesText.color = targetColor;
            }

            if (joulesIcon != null)
            {
                joulesIcon.color = targetColor;
            }
        }

        /// <summary>
        /// Manually set the displayed joule value (for testing)
        /// </summary>
        public void SetDisplayedJoules(int joules)
        {
            displayedJoules = joules;
            UpdateDisplay();
        }

        /// <summary>
        /// Manually refresh the display
        /// </summary>
        public void RefreshDisplay()
        {
            if (joulesManager != null)
            {
                displayedJoules = joulesManager.GetCurrentJoules();
                displayedPatronTier = joulesManager.GetCurrentPatronTier();
                UpdateDisplay();
            }
        }
    }
}
