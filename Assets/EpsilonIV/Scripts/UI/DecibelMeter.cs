using UnityEngine;
using UnityEngine.UI;

namespace EpsilonIV
{
    /// <summary>
    /// Visual decibel meter that displays the loudness of sounds the player is making.
    /// Listens to SoundEmitter.OnAnySoundEmitted and displays the current sound level.
    /// </summary>
    public class DecibelMeter : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Image or Slider that displays the current sound level (0-1)")]
        public Image fillImage;

        [Tooltip("Optional slider component (will use fillAmount if Image, or value if Slider)")]
        public Slider slider;

        [Header("Meter Settings")]
        [Tooltip("Maximum loudness value that maps to 100% on the meter. Sounds louder than this will be clamped.")]
        [Range(0.1f, 2f)]
        public float maxLoudness = 1f;

        [Tooltip("How quickly the meter decays back to zero when no sound is emitted")]
        [Range(0.1f, 5f)]
        public float decaySpeed = 2f;

        [Tooltip("How quickly the meter responds to new sounds (higher = faster response)")]
        [Range(0.1f, 20f)]
        public float riseSpeed = 10f;

        [Tooltip("Minimum loudness threshold to register on the meter (filters out tiny sounds)")]
        [Range(0f, 0.1f)]
        public float noiseFloor = 0.01f;

        [Header("Visual Settings")]
        [Tooltip("Color for 0-25% loudness")]
        public Color lowColor = Color.green;

        [Tooltip("Color for 25-50% loudness")]
        public Color mediumLowColor = Color.yellow;

        [Tooltip("Color for 50-75% loudness")]
        public Color mediumHighColor = new Color(1f, 0.5f, 0f); // Orange

        [Tooltip("Color for 75-100% loudness")]
        public Color highColor = Color.red;

        [Tooltip("Enable color zones based on loudness")]
        public bool enableColorZones = true;

        private float currentLevel = 0f;
        private float targetLevel = 0f;

        void OnEnable()
        {
            // Subscribe to the static sound event
            SoundEmitter.OnAnySoundEmitted.AddListener(OnSoundEmitted);
        }

        void OnDisable()
        {
            // Unsubscribe to avoid memory leaks
            SoundEmitter.OnAnySoundEmitted.RemoveListener(OnSoundEmitted);
        }

        void Update()
        {
            // Smoothly interpolate current level towards target
            if (currentLevel < targetLevel)
            {
                // Rising: lerp towards target
                currentLevel = Mathf.Lerp(currentLevel, targetLevel, riseSpeed * Time.deltaTime);
            }
            else if (currentLevel > 0f)
            {
                // Falling: decay back to zero
                currentLevel -= decaySpeed * Time.deltaTime;
                currentLevel = Mathf.Max(0f, currentLevel);
            }

            // Decay target level over time as well
            if (targetLevel > 0f)
            {
                targetLevel -= decaySpeed * Time.deltaTime;
                targetLevel = Mathf.Max(0f, targetLevel);
            }

            // Update UI
            UpdateMeterVisuals();
        }

        /// <summary>
        /// Called when any sound is emitted in the game
        /// </summary>
        private void OnSoundEmitted(float loudness, float quality, Vector3 position)
        {
            // Filter out sounds below the noise floor
            if (loudness < noiseFloor)
                return;

            // Normalize loudness to 0-1 range based on maxLoudness setting
            float normalizedLoudness = loudness / maxLoudness;

            // Update the target level (take the max of current target and new sound)
            // This prevents the meter from flickering if multiple small sounds occur
            targetLevel = Mathf.Max(targetLevel, normalizedLoudness);

            // Clamp to 0-1 range
            targetLevel = Mathf.Clamp01(targetLevel);
        }

        /// <summary>
        /// Update the visual representation of the meter
        /// </summary>
        private void UpdateMeterVisuals()
        {
            // Update fill amount
            if (slider != null)
            {
                slider.value = currentLevel;
                Debug.Log($"[DecibelMeter] Updated slider value to {currentLevel}");
            }
            else if (fillImage != null)
            {
                fillImage.fillAmount = currentLevel;
                Debug.Log($"[DecibelMeter] Updated fillImage.fillAmount to {currentLevel}, Image type: {fillImage.type}");
            }

            // Update color zones
            if (enableColorZones && fillImage != null)
            {
                Color targetColor;
                float percentage = currentLevel * 100f;

                if (percentage < 25f)
                {
                    targetColor = lowColor;
                }
                else if (percentage < 50f)
                {
                    targetColor = mediumLowColor;
                }
                else if (percentage < 75f)
                {
                    targetColor = mediumHighColor;
                }
                else
                {
                    targetColor = highColor;
                }

                fillImage.color = targetColor;
            }
        }
    }
}
