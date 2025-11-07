using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Animates a light's intensity for an explosion flash effect.
    /// Fades in quickly, holds at peak, then fades out.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class ExplosionFlash : MonoBehaviour
    {
        [Header("Flash Settings")]
        [Tooltip("Maximum light intensity at peak")]
        [Range(0f, 100f)]
        public float maxIntensity = 30f;

        [Tooltip("Time to reach peak intensity (seconds)")]
        [Range(0.01f, 2f)]
        public float fadeInDuration = 0.2f;

        [Tooltip("Time to hold at peak intensity (seconds)")]
        [Range(0f, 2f)]
        public float holdDuration = 0.3f;

        [Tooltip("Time to fade out completely (seconds)")]
        [Range(0.1f, 5f)]
        public float fadeOutDuration = 1.5f;

        [Tooltip("Auto-trigger flash on Start()")]
        public bool playOnStart = true;

        [Tooltip("Destroy GameObject after flash completes")]
        public bool destroyAfterFlash = false;

        private Light explosionLight;
        private float elapsedTime = 0f;
        private bool isFlashing = false;

        void Start()
        {
            explosionLight = GetComponent<Light>();
            explosionLight.intensity = 0f;

            if (playOnStart)
            {
                TriggerFlash();
            }
        }

        void Update()
        {
            if (!isFlashing) return;

            elapsedTime += Time.deltaTime;

            // Phase 1: Fade In
            if (elapsedTime < fadeInDuration)
            {
                float t = elapsedTime / fadeInDuration;
                explosionLight.intensity = Mathf.Lerp(0f, maxIntensity, t);
            }
            // Phase 2: Hold
            else if (elapsedTime < fadeInDuration + holdDuration)
            {
                explosionLight.intensity = maxIntensity;
            }
            // Phase 3: Fade Out
            else if (elapsedTime < fadeInDuration + holdDuration + fadeOutDuration)
            {
                float fadeOutStart = fadeInDuration + holdDuration;
                float t = (elapsedTime - fadeOutStart) / fadeOutDuration;
                explosionLight.intensity = Mathf.Lerp(maxIntensity, 0f, t);
            }
            // Phase 4: Complete
            else
            {
                explosionLight.intensity = 0f;
                isFlashing = false;

                if (destroyAfterFlash)
                {
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        /// Trigger the explosion flash effect
        /// </summary>
        public void TriggerFlash()
        {
            elapsedTime = 0f;
            isFlashing = true;
            explosionLight.intensity = 0f;
        }

        /// <summary>
        /// Get total duration of the flash effect
        /// </summary>
        public float GetTotalDuration()
        {
            return fadeInDuration + holdDuration + fadeOutDuration;
        }
    }
}
