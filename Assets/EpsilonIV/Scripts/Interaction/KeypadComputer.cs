using UnityEngine;
using TMPro;
using System.Collections;

namespace EpsilonIV
{
    /// <summary>
    /// Core keypad logic with state machine
    /// Tracks code entry, validates against correct code, provides feedback
    /// </summary>
    public class KeypadComputer : MonoBehaviour
    {
        [Header("Code Settings")]
        [Tooltip("The correct code to unlock (e.g., '1234')")]
        public string CorrectCode = "1234";

        [Tooltip("Maximum digits that can be entered")]
        public int MaxCodeLength = 8;

        [Tooltip("Keep door unlocked permanently after correct code")]
        public bool StayUnlockedPermanently = false;

        [Header("Door Reference")]
        [Tooltip("The door that this keypad controls")]
        public Door ControlledDoor;

        [Tooltip("Automatically re-lock when door closes (only if not staying unlocked permanently)")]
        public bool RelockOnDoorClose = true;

        [Header("Display")]
        [Tooltip("Text display showing entered code (works with both UI and World Space TextMeshPro)")]
        public TMP_Text DisplayText;

        [Tooltip("Text to show when idle/cleared")]
        public string IdleDisplayText = "----";

        [Tooltip("Separator between digits (e.g., ' ' or '-')")]
        public string DigitSeparator = " ";

        [Header("Visual Feedback")]
        [Tooltip("Light component for status indication (optional)")]
        public Light StatusLight;

        [Tooltip("Color when idle/entering")]
        public Color IdleColor = Color.white;

        [Tooltip("Color on success")]
        public Color SuccessColor = Color.green;

        [Tooltip("Color on failure")]
        public Color FailureColor = Color.red;

        [Tooltip("Display text color on success")]
        public Color SuccessTextColor = Color.green;

        [Tooltip("Display text color on failure")]
        public Color FailureTextColor = Color.red;

        [Tooltip("Display text color normal")]
        public Color NormalTextColor = Color.white;

        [Header("Audio")]
        [Tooltip("Sound when button is pressed")]
        public AudioClip ButtonPressSound;

        [Tooltip("Sound when correct code is entered")]
        public AudioClip SuccessSound;

        [Tooltip("Sound when wrong code is entered")]
        public AudioClip FailureSound;

        [Header("Timing")]
        [Tooltip("How long to show success feedback before unlocking")]
        public float SuccessFeedbackDuration = 1.5f;

        [Tooltip("How long to show failure feedback before resetting")]
        public float FailureFeedbackDuration = 1.5f;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool DebugMode = false;

        // State
        public enum KeypadState
        {
            Idle,       // Waiting for input, display clear
            Entering,   // Accepting input, showing digits
            Success,    // Correct code, showing success feedback
            Failed,     // Wrong code, showing failure feedback
            Unlocked    // Permanently unlocked (if StayUnlockedPermanently = true)
        }

        private KeypadState m_CurrentState = KeypadState.Idle;
        private string m_CurrentCode = "";
        private bool m_IsUnlocked = false;
        private AudioSource m_AudioSource;
        private Color m_OriginalLightColor;

        public bool IsUnlocked => m_IsUnlocked;
        public KeypadState CurrentState => m_CurrentState;

        void Start()
        {
            // Setup audio source
            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource == null && (ButtonPressSound != null || SuccessSound != null || FailureSound != null))
            {
                m_AudioSource = gameObject.AddComponent<AudioSource>();
                m_AudioSource.playOnAwake = false;
                m_AudioSource.spatialBlend = 1f; // 3D sound
            }

            // Store original light color
            if (StatusLight != null)
            {
                m_OriginalLightColor = StatusLight.color;
                StatusLight.color = IdleColor;
            }

            // Initialize display
            UpdateDisplay();

            // Subscribe to door events if auto re-lock is enabled
            if (RelockOnDoorClose && ControlledDoor != null)
            {
                ControlledDoor.OnDoorClosed.AddListener(OnDoorClosed);
            }

            if (DebugMode)
            {
                Debug.Log($"[KeypadComputer] Initialized. Correct code: {CorrectCode}");
            }
        }

        void OnDestroy()
        {
            // Unsubscribe from door events
            if (ControlledDoor != null)
            {
                ControlledDoor.OnDoorClosed.RemoveListener(OnDoorClosed);
            }
        }

        /// <summary>
        /// Called when the controlled door closes
        /// </summary>
        void OnDoorClosed()
        {
            if (!StayUnlockedPermanently)
            {
                ResetUnlock();

                if (DebugMode)
                {
                    Debug.Log($"[KeypadComputer] Door closed, re-locking");
                }
            }
        }

        /// <summary>
        /// Called by KeypadButton when a button is pressed
        /// </summary>
        public void OnButtonPressed(string value)
        {
            // Ignore input in certain states
            if (m_CurrentState == KeypadState.Success ||
                m_CurrentState == KeypadState.Failed ||
                m_CurrentState == KeypadState.Unlocked)
            {
                if (DebugMode)
                {
                    Debug.Log($"[KeypadComputer] Ignoring input '{value}' - in state {m_CurrentState}");
                }
                return;
            }

            // Play button press sound
            PlaySound(ButtonPressSound);

            // Handle Clear button
            if (value == "Clear")
            {
                if (DebugMode)
                {
                    Debug.Log($"[KeypadComputer] Clear pressed");
                }
                Clear();
                return;
            }

            // Don't exceed max length
            if (m_CurrentCode.Length >= MaxCodeLength)
            {
                if (DebugMode)
                {
                    Debug.Log($"[KeypadComputer] Max code length reached, ignoring '{value}'");
                }
                return;
            }

            // Add digit to current code
            m_CurrentCode += value;
            m_CurrentState = KeypadState.Entering;

            if (DebugMode)
            {
                Debug.Log($"[KeypadComputer] Code entered: {m_CurrentCode}");
            }

            UpdateDisplay();

            // Check if we've reached the correct code length
            if (m_CurrentCode.Length == CorrectCode.Length)
            {
                CheckCode();
            }
        }

        /// <summary>
        /// Clears the current code and resets to idle state
        /// </summary>
        public void Clear()
        {
            m_CurrentCode = "";
            m_CurrentState = KeypadState.Idle;
            UpdateDisplay();

            if (StatusLight != null)
            {
                StatusLight.color = IdleColor;
            }

            if (DebugMode)
            {
                Debug.Log($"[KeypadComputer] Cleared");
            }
        }

        /// <summary>
        /// Checks if current code matches correct code
        /// </summary>
        void CheckCode()
        {
            if (m_CurrentCode == CorrectCode)
            {
                if (DebugMode)
                {
                    Debug.Log($"[KeypadComputer] Correct code entered!");
                }
                StartCoroutine(SuccessSequence());
            }
            else
            {
                if (DebugMode)
                {
                    Debug.Log($"[KeypadComputer] Wrong code entered. Expected: {CorrectCode}, Got: {m_CurrentCode}");
                }
                StartCoroutine(FailureSequence());
            }
        }

        /// <summary>
        /// Success feedback sequence
        /// </summary>
        IEnumerator SuccessSequence()
        {
            m_CurrentState = KeypadState.Success;

            // Visual feedback
            if (DisplayText != null)
            {
                DisplayText.color = SuccessTextColor;
            }

            if (StatusLight != null)
            {
                StatusLight.color = SuccessColor;
            }

            // Audio feedback
            PlaySound(SuccessSound);

            // Wait for feedback duration
            yield return new WaitForSeconds(SuccessFeedbackDuration);

            // Unlock
            m_IsUnlocked = true;

            // Unlock the controlled door
            if (ControlledDoor != null)
            {
                ControlledDoor.Unlock();

                if (DebugMode)
                {
                    Debug.Log($"[KeypadComputer] Unlocked door: {ControlledDoor.gameObject.name}");
                }
            }
            else
            {
                if (DebugMode)
                {
                    Debug.LogWarning($"[KeypadComputer] No door assigned to unlock!");
                }
            }

            if (StayUnlockedPermanently)
            {
                m_CurrentState = KeypadState.Unlocked;
                if (DisplayText != null)
                {
                    DisplayText.text = "UNLOCKED";
                }

                if (DebugMode)
                {
                    Debug.Log($"[KeypadComputer] Permanently unlocked");
                }
            }
            else
            {
                // Reset to idle but keep unlocked flag
                Clear();

                if (DebugMode)
                {
                    Debug.Log($"[KeypadComputer] Unlocked (temporary)");
                }
            }
        }

        /// <summary>
        /// Failure feedback sequence
        /// </summary>
        IEnumerator FailureSequence()
        {
            m_CurrentState = KeypadState.Failed;

            // Visual feedback
            string originalText = GetDisplayText();

            if (DisplayText != null)
            {
                DisplayText.text = "ERROR";
                DisplayText.color = FailureTextColor;
            }

            if (StatusLight != null)
            {
                StatusLight.color = FailureColor;
            }

            // Audio feedback
            PlaySound(FailureSound);

            // Wait for feedback duration
            yield return new WaitForSeconds(FailureFeedbackDuration);

            // Auto-reset
            Clear();

            if (DisplayText != null)
            {
                DisplayText.color = NormalTextColor;
            }
        }

        /// <summary>
        /// Updates the display text
        /// </summary>
        void UpdateDisplay()
        {
            if (DisplayText == null)
                return;

            DisplayText.text = GetDisplayText();
        }

        /// <summary>
        /// Gets the text to display based on current state
        /// </summary>
        string GetDisplayText()
        {
            if (m_CurrentState == KeypadState.Idle && string.IsNullOrEmpty(m_CurrentCode))
            {
                return IdleDisplayText;
            }

            if (m_CurrentState == KeypadState.Unlocked)
            {
                return "UNLOCKED";
            }

            // Show entered digits with separator
            if (string.IsNullOrEmpty(m_CurrentCode))
            {
                return IdleDisplayText;
            }

            return string.Join(DigitSeparator, m_CurrentCode.ToCharArray());
        }

        /// <summary>
        /// Plays an audio clip
        /// </summary>
        void PlaySound(AudioClip clip)
        {
            if (clip != null && m_AudioSource != null)
            {
                m_AudioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Resets the unlock state (called by KeypadDoor when door closes if not staying unlocked permanently)
        /// </summary>
        public void ResetUnlock()
        {
            if (StayUnlockedPermanently)
                return;

            m_IsUnlocked = false;

            // Re-lock the controlled door
            if (ControlledDoor != null)
            {
                ControlledDoor.Lock();

                if (DebugMode)
                {
                    Debug.Log($"[KeypadComputer] Re-locked door: {ControlledDoor.gameObject.name}");
                }
            }

            if (DebugMode)
            {
                Debug.Log($"[KeypadComputer] Unlock state reset");
            }
        }

        /// <summary>
        /// Manually unlock the keypad (for testing or triggered events)
        /// </summary>
        public void ForceUnlock()
        {
            m_IsUnlocked = true;
            m_CurrentState = KeypadState.Unlocked;

            if (DisplayText != null)
            {
                DisplayText.text = "UNLOCKED";
                DisplayText.color = SuccessTextColor;
            }

            if (StatusLight != null)
            {
                StatusLight.color = SuccessColor;
            }

            // Unlock the controlled door
            if (ControlledDoor != null)
            {
                ControlledDoor.Unlock();
            }

            if (DebugMode)
            {
                Debug.Log($"[KeypadComputer] Force unlocked");
            }
        }

        /// <summary>
        /// Manually lock the keypad
        /// </summary>
        public void ForceLock()
        {
            m_IsUnlocked = false;
            Clear();

            // Lock the controlled door
            if (ControlledDoor != null)
            {
                ControlledDoor.Lock();
            }

            if (DebugMode)
            {
                Debug.Log($"[KeypadComputer] Force locked");
            }
        }
    }
}
