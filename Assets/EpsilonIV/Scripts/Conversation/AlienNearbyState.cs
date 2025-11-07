using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Provides dynamic game state information about alien proximity.
    /// Used by RadioNpc to inject real-time threat information into NPC conversations.
    /// </summary>
    public class AlienNearbyState : MonoBehaviour
    {
        [Header("Alien Reference")]
        [Tooltip("The alien GameObject to track")]
        [SerializeField] private GameObject alien;

        [Header("Distance Thresholds")]
        [Tooltip("Distance threshold - alien closer than this is 'nearby' (unsafe)")]
        [SerializeField] private float nearbyDistance = 15f;

        [Tooltip("Distance threshold - alien farther than this is 'far away' (safe)")]
        [SerializeField] private float safeDistance = 30f;

        [Header("Game State Messages")]
        [Tooltip("Message sent when alien is nearby (dangerous)")]
        [SerializeField] private string nearbyMessage = "[game state truth: the alien is still close by, it's not safe to come down]";

        [Tooltip("Message sent when alien is far away (safe)")]
        [SerializeField] private string safeMessage = "[game state truth: the alien is gone and it's safe to come down]";

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        /// <summary>
        /// Get the current game state message based on alien proximity
        /// </summary>
        public string GetGameStateMessage()
        {
            if (alien == null)
            {
                if (debugMode)
                    Debug.LogWarning("[AlienNearbyState] No alien assigned - returning safe message");
                return safeMessage;
            }

            float distance = Vector3.Distance(transform.position, alien.transform.position);

            if (debugMode)
                Debug.Log($"[AlienNearbyState] Alien is {distance:F1}m away");

            // Determine if alien is nearby or far
            if (distance <= nearbyDistance)
            {
                if (debugMode)
                    Debug.Log($"[AlienNearbyState] Alien is nearby ({distance:F1}m <= {nearbyDistance}m) - UNSAFE");
                return nearbyMessage;
            }
            else if (distance >= safeDistance)
            {
                if (debugMode)
                    Debug.Log($"[AlienNearbyState] Alien is far away ({distance:F1}m >= {safeDistance}m) - SAFE");
                return safeMessage;
            }
            else
            {
                // In between - still consider unsafe
                if (debugMode)
                    Debug.Log($"[AlienNearbyState] Alien is in medium range ({distance:F1}m) - UNSAFE");
                return nearbyMessage;
            }
        }

        /// <summary>
        /// Visualize the distance thresholds in the Scene view
        /// </summary>
        void OnDrawGizmos()
        {
            if (!debugMode)
                return;

            if (alien != null)
            {
                float distance = Vector3.Distance(transform.position, alien.transform.position);

                // Draw line to alien
                Gizmos.color = distance <= nearbyDistance ? Color.red : (distance >= safeDistance ? Color.green : Color.yellow);
                Gizmos.DrawLine(transform.position, alien.transform.position);
            }

            // Draw distance spheres
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, nearbyDistance);

            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, safeDistance);
        }
    }
}
