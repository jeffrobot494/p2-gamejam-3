using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Animates a sphere's scale for an explosion effect.
    /// Rapidly expands from zero to maximum size, then shrinks.
    /// </summary>
    public class ExplosionSphere : MonoBehaviour
    {
        [Header("Scale Animation")]
        [Tooltip("Maximum scale the sphere reaches at peak")]
        public float maxScale = 50f;

        [Tooltip("Time to reach maximum scale (seconds)")]
        [Range(0.01f, 20f)]
        public float expandDuration = 0.5f;

        [Tooltip("Time to hold at maximum scale (seconds)")]
        [Range(0f, 2f)]
        public float holdDuration = 0.2f;

        [Tooltip("Time to shrink back down (seconds)")]
        [Range(0.1f, 20f)]
        public float shrinkDuration = 1f;

        [Header("Options")]
        [Tooltip("Auto-trigger explosion on Start()")]
        public bool playOnStart = true;

        [Tooltip("Destroy GameObject after animation completes")]
        public bool destroyAfterExplosion = true;

        [Tooltip("Use ease-in/ease-out curves for smoother animation")]
        public bool useEasing = true;

        private Vector3 startScale;
        private float elapsedTime = 0f;
        private bool isExploding = false;

        void Start()
        {
            startScale = Vector3.zero;
            transform.localScale = startScale;

            if (playOnStart)
            {
                TriggerExplosion();
            }
        }

        void Update()
        {
            if (!isExploding) return;

            elapsedTime += Time.deltaTime;

            // Phase 1: Expand
            if (elapsedTime < expandDuration)
            {
                float t = elapsedTime / expandDuration;
                if (useEasing)
                    t = EaseOutCubic(t);

                float currentScale = Mathf.Lerp(0f, maxScale, t);
                transform.localScale = Vector3.one * currentScale;
            }
            // Phase 2: Hold
            else if (elapsedTime < expandDuration + holdDuration)
            {
                transform.localScale = Vector3.one * maxScale;
            }
            // Phase 3: Shrink
            else if (elapsedTime < expandDuration + holdDuration + shrinkDuration)
            {
                float shrinkStart = expandDuration + holdDuration;
                float t = (elapsedTime - shrinkStart) / shrinkDuration;
                if (useEasing)
                    t = EaseInCubic(t);

                float currentScale = Mathf.Lerp(maxScale, 0f, t);
                transform.localScale = Vector3.one * currentScale;
            }
            // Phase 4: Complete
            else
            {
                transform.localScale = Vector3.zero;
                isExploding = false;

                if (destroyAfterExplosion)
                {
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        /// Trigger the explosion animation
        /// </summary>
        public void TriggerExplosion()
        {
            elapsedTime = 0f;
            isExploding = true;
            transform.localScale = Vector3.zero;
        }

        /// <summary>
        /// Get total duration of the explosion effect
        /// </summary>
        public float GetTotalDuration()
        {
            return expandDuration + holdDuration + shrinkDuration;
        }

        // Easing functions for smoother animation
        private float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private float EaseInCubic(float t)
        {
            return t * t * t;
        }
    }
}
