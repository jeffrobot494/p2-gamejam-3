using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Shakes the camera for explosion or impact effects.
    /// Can be triggered manually or on Start for cutscenes.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [Header("Shake Settings")]
        [Tooltip("Maximum shake intensity (position offset)")]
        [Range(0f, 5f)]
        public float shakeIntensity = 0.5f;

        [Tooltip("Duration of the shake effect (seconds)")]
        [Range(0.1f, 10f)]
        public float shakeDuration = 2f;

        [Tooltip("How quickly the shake diminishes (higher = faster decay)")]
        [Range(0.1f, 5f)]
        public float dampingSpeed = 1f;

        [Tooltip("Shake frequency (higher = more erratic)")]
        [Range(1f, 50f)]
        public float frequency = 25f;

        [Header("Options")]
        [Tooltip("Auto-trigger shake on Start()")]
        public bool playOnStart = false;

        [Tooltip("Also apply rotation shake")]
        public bool shakeRotation = true;

        [Tooltip("Maximum rotation shake (degrees)")]
        [Range(0f, 10f)]
        public float rotationIntensity = 2f;

        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private float shakeTimer = 0f;
        private bool isShaking = false;

        void Start()
        {
            originalPosition = transform.localPosition;
            originalRotation = transform.localRotation;

            if (playOnStart)
            {
                TriggerShake();
            }
        }

        void Update()
        {
            if (!isShaking) return;

            if (shakeTimer > 0)
            {
                // Calculate decay factor (starts at 1.0, fades to 0)
                float decayFactor = shakeTimer / shakeDuration;
                decayFactor = Mathf.Pow(decayFactor, dampingSpeed);

                // Generate random shake offset using Perlin noise for smoothness
                float xOffset = (Mathf.PerlinNoise(Time.time * frequency, 0f) - 0.5f) * 2f;
                float yOffset = (Mathf.PerlinNoise(0f, Time.time * frequency) - 0.5f) * 2f;
                float zOffset = (Mathf.PerlinNoise(Time.time * frequency, Time.time * frequency) - 0.5f) * 2f;

                Vector3 shakeOffset = new Vector3(xOffset, yOffset, zOffset) * shakeIntensity * decayFactor;
                transform.localPosition = originalPosition + shakeOffset;

                // Apply rotation shake if enabled
                if (shakeRotation)
                {
                    float xRot = (Mathf.PerlinNoise(Time.time * frequency + 100f, 0f) - 0.5f) * 2f;
                    float yRot = (Mathf.PerlinNoise(0f, Time.time * frequency + 100f) - 0.5f) * 2f;
                    float zRot = (Mathf.PerlinNoise(Time.time * frequency + 200f, Time.time * frequency + 200f) - 0.5f) * 2f;

                    Vector3 rotationShake = new Vector3(xRot, yRot, zRot) * rotationIntensity * decayFactor;
                    transform.localRotation = originalRotation * Quaternion.Euler(rotationShake);
                }

                shakeTimer -= Time.deltaTime;
            }
            else
            {
                // Shake complete - reset to original transform
                transform.localPosition = originalPosition;
                transform.localRotation = originalRotation;
                isShaking = false;
            }
        }

        /// <summary>
        /// Trigger the camera shake effect
        /// </summary>
        public void TriggerShake()
        {
            originalPosition = transform.localPosition;
            originalRotation = transform.localRotation;
            shakeTimer = shakeDuration;
            isShaking = true;
        }

        /// <summary>
        /// Trigger shake with custom intensity and duration
        /// </summary>
        public void TriggerShake(float intensity, float duration)
        {
            shakeIntensity = intensity;
            shakeDuration = duration;
            TriggerShake();
        }

        /// <summary>
        /// Stop shaking immediately and reset camera
        /// </summary>
        public void StopShake()
        {
            transform.localPosition = originalPosition;
            transform.localRotation = originalRotation;
            isShaking = false;
            shakeTimer = 0f;
        }
    }
}
