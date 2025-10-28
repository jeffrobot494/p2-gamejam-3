using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Manages mission countdown timer (logic only, no UI)
    /// </summary>
    public class GameTimer : MonoBehaviour
    {
        [Header("Timer Settings")]
        [Tooltip("Starting time in seconds")]
        public float InitialTime = 300f; // 5 minutes

        [Tooltip("Time penalty when player dies (seconds)")]
        public float DeathPenalty = 30f;

        [Tooltip("Start timer automatically on Start()")]
        public bool StartAutomatically = true;

        [Header("Events")]
        [Tooltip("Called when timer reaches zero")]
        public UnityEvent OnTimerExpired;

        [Tooltip("Called when time is reduced (e.g., death penalty)")]
        public UnityEvent<float> OnTimeReduced;

        [Tooltip("Called when timer is paused")]
        public UnityEvent OnTimerPaused;

        [Tooltip("Called when timer is resumed")]
        public UnityEvent OnTimerResumed;

        // Current state
        private float m_TimeRemaining;
        private bool m_IsRunning = false;
        private bool m_HasExpired = false;

        /// <summary>
        /// Gets the current time remaining in seconds
        /// </summary>
        public float TimeRemaining => m_TimeRemaining;

        /// <summary>
        /// Gets whether the timer is currently running
        /// </summary>
        public bool IsRunning => m_IsRunning;

        /// <summary>
        /// Gets whether the timer has expired (reached zero)
        /// </summary>
        public bool HasExpired => m_HasExpired;

        /// <summary>
        /// Gets the initial time the timer started with
        /// </summary>
        public float InitialTimeValue => InitialTime;

        /// <summary>
        /// Gets the percentage of time remaining (0-1)
        /// </summary>
        public float TimeRemainingPercent => Mathf.Clamp01(m_TimeRemaining / InitialTime);

        void Start()
        {
            m_TimeRemaining = InitialTime;

            if (StartAutomatically)
            {
                StartTimer();
            }
        }

        void Update()
        {
            if (m_IsRunning && !m_HasExpired)
            {
                m_TimeRemaining -= Time.deltaTime;

                // Check if timer expired
                if (m_TimeRemaining <= 0f)
                {
                    m_TimeRemaining = 0f;
                    m_HasExpired = true;
                    m_IsRunning = false;

                    OnTimerExpired?.Invoke();

                    Debug.Log("[GameTimer] Timer expired!");
                }
            }
        }

        /// <summary>
        /// Starts or resumes the timer
        /// </summary>
        public void StartTimer()
        {
            if (m_HasExpired)
            {
                Debug.LogWarning("[GameTimer] Cannot start - timer has already expired!");
                return;
            }

            m_IsRunning = true;
            Debug.Log("[GameTimer] Timer started");
        }

        /// <summary>
        /// Pauses the timer
        /// </summary>
        public void PauseTimer()
        {
            if (!m_IsRunning)
                return;

            m_IsRunning = false;
            OnTimerPaused?.Invoke();
            Debug.Log("[GameTimer] Timer paused");
        }

        /// <summary>
        /// Resumes the timer if it was paused
        /// </summary>
        public void ResumeTimer()
        {
            if (m_IsRunning || m_HasExpired)
                return;

            m_IsRunning = true;
            OnTimerResumed?.Invoke();
            Debug.Log("[GameTimer] Timer resumed");
        }

        /// <summary>
        /// Reduces the time remaining by the specified amount
        /// Used for death penalty
        /// </summary>
        public void ReduceTime(float seconds)
        {
            if (seconds <= 0f)
                return;

            m_TimeRemaining -= seconds;

            // Clamp to zero
            if (m_TimeRemaining < 0f)
            {
                m_TimeRemaining = 0f;
            }

            OnTimeReduced?.Invoke(seconds);

            Debug.Log($"[GameTimer] Time reduced by {seconds:F1}s. Remaining: {m_TimeRemaining:F1}s");

            // Check if this reduction caused expiration
            if (m_TimeRemaining <= 0f && !m_HasExpired)
            {
                m_HasExpired = true;
                m_IsRunning = false;
                OnTimerExpired?.Invoke();
            }
        }

        /// <summary>
        /// Applies the death penalty (reduces time by DeathPenalty amount)
        /// </summary>
        public void ApplyDeathPenalty()
        {
            ReduceTime(DeathPenalty);
        }

        /// <summary>
        /// Adds time to the timer (for pickups/bonuses)
        /// </summary>
        public void AddTime(float seconds)
        {
            if (seconds <= 0f)
                return;

            m_TimeRemaining += seconds;

            // Don't exceed initial time
            m_TimeRemaining = Mathf.Min(m_TimeRemaining, InitialTime);

            Debug.Log($"[GameTimer] Time added: {seconds:F1}s. Remaining: {m_TimeRemaining:F1}s");
        }

        /// <summary>
        /// Resets the timer to initial time
        /// </summary>
        public void ResetTimer()
        {
            m_TimeRemaining = InitialTime;
            m_HasExpired = false;
            m_IsRunning = false;

            Debug.Log("[GameTimer] Timer reset");
        }

        /// <summary>
        /// Formats the time remaining as MM:SS
        /// </summary>
        public string GetFormattedTime()
        {
            int minutes = Mathf.FloorToInt(m_TimeRemaining / 60f);
            int seconds = Mathf.FloorToInt(m_TimeRemaining % 60f);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        /// <summary>
        /// Gets time remaining in minutes (float)
        /// </summary>
        public float GetTimeInMinutes()
        {
            return m_TimeRemaining / 60f;
        }
    }
}
