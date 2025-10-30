using UnityEngine;
using TMPro;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Simple UI display for GameTimer (visuals only)
    /// </summary>
    public class GameTimerUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The GameTimer to display")]
        public GameTimer GameTimer;

        [Tooltip("Text component to display the time")]
        public TextMeshProUGUI TimeText;

        [Header("Display Settings")]
        [Tooltip("Text format: {0} = time formatted as MM:SS")]
        public string TimeFormat = "TIME: {0}";

        [Tooltip("Color when time is normal")]
        public Color NormalColor = Color.white;

        [Tooltip("Color when time is low (warning)")]
        public Color WarningColor = Color.yellow;

        [Tooltip("Color when time is critical")]
        public Color CriticalColor = Color.red;

        [Tooltip("Time threshold for warning color (seconds)")]
        public float WarningThreshold = 60f; // 1 minute

        [Tooltip("Time threshold for critical color (seconds)")]
        public float CriticalThreshold = 30f; // 30 seconds

        [Header("Effects")]
        [Tooltip("Enable pulsing effect when time is critical")]
        public bool EnablePulse = true;

        [Tooltip("Pulse speed when time is critical")]
        public float PulseSpeed = 3f;

        private float m_PulseTime = 0f;

        void Start()
        {
            // Auto-find GameTimer if not assigned
            if (GameTimer == null)
            {
                GameTimer = FindFirstObjectByType<GameTimer>();
            }

            if (GameTimer == null)
            {
                Debug.LogError("[GameTimerUI] No GameTimer found in scene!");
            }

            if (TimeText == null)
            {
                Debug.LogError("[GameTimerUI] No TimeText assigned!");
            }
        }

        void Update()
        {
            if (GameTimer == null || TimeText == null)
                return;

            UpdateTimeDisplay();
            UpdateTimeColor();

            if (EnablePulse && GameTimer.TimeRemaining <= CriticalThreshold)
            {
                UpdatePulseEffect();
            }
            else
            {
                // Reset scale
                TimeText.transform.localScale = Vector3.one;
            }
        }

        void UpdateTimeDisplay()
        {
            string formattedTime = GameTimer.GetFormattedTime();
            TimeText.text = string.Format(TimeFormat, formattedTime);
        }

        void UpdateTimeColor()
        {
            float timeRemaining = GameTimer.TimeRemaining;

            if (timeRemaining <= CriticalThreshold)
            {
                TimeText.color = CriticalColor;
            }
            else if (timeRemaining <= WarningThreshold)
            {
                TimeText.color = WarningColor;
            }
            else
            {
                TimeText.color = NormalColor;
            }
        }

        void UpdatePulseEffect()
        {
            m_PulseTime += Time.deltaTime * PulseSpeed;
            float scale = 1f + Mathf.Sin(m_PulseTime) * 0.1f;
            TimeText.transform.localScale = Vector3.one * scale;
        }
    }
}
