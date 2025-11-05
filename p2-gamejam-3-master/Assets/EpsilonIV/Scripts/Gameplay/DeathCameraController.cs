using System.Collections;
using UnityEngine;
using EpsilonIV;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// Handles camera animation during player death
    /// Makes camera fall to ground and rotate to sideways view
    /// </summary>
    public class DeathCameraController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The camera transform to animate")]
        public Transform CameraTransform;

        [Tooltip("Player character controller (to get ground position)")]
        public PlayerCharacterController PlayerController;

        [Header("Fall Animation")]
        [Tooltip("How long the fall animation takes (seconds)")]
        public float FallDuration = 1f;

        [Tooltip("Height offset from ground when lying down")]
        public float GroundHeightOffset = 0.2f;

        [Tooltip("How much to tilt the camera (90 = lying on side)")]
        public float TiltAngle = 90f;

        [Tooltip("Curve for fall animation (non-linear fall)")]
        public AnimationCurve FallCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Ground View")]
        [Tooltip("How long to hold the ground view before fade (seconds)")]
        public float GroundViewDuration = 1.5f;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool DebugMode = true;

        // State
        private Vector3 m_OriginalLocalPosition;
        private Quaternion m_OriginalLocalRotation;
        private bool m_IsAnimating = false;

        void Start()
        {
            // Auto-find camera if not assigned
            if (CameraTransform == null)
            {
                CameraTransform = Camera.main?.transform;
            }

            if (CameraTransform == null)
            {
                Debug.LogError("[DeathCameraController] No camera transform assigned or found!");
            }

            // Auto-find player controller
            if (PlayerController == null)
            {
                PlayerController = FindFirstObjectByType<PlayerCharacterController>();
            }

            // Store original local position/rotation relative to parent
            if (CameraTransform != null)
            {
                m_OriginalLocalPosition = CameraTransform.localPosition;
                m_OriginalLocalRotation = CameraTransform.localRotation;
            }
        }

        /// <summary>
        /// Plays the death camera fall animation
        /// Returns a coroutine that can be yielded
        /// </summary>
        public IEnumerator PlayDeathAnimation()
        {
            if (m_IsAnimating)
            {
                Debug.LogWarning("[DeathCameraController] Animation already playing!");
                yield break;
            }

            m_IsAnimating = true;

            if (DebugMode)
            {
                Debug.Log("[DeathCameraController] Playing death animation");
            }

            // Perform fall animation
            yield return StartCoroutine(FallToGround());

            // Hold the ground view
            yield return new WaitForSeconds(GroundViewDuration);

            m_IsAnimating = false;

            if (DebugMode)
            {
                Debug.Log("[DeathCameraController] Death animation complete");
            }
        }

        /// <summary>
        /// Animates camera falling to ground and tilting sideways
        /// </summary>
        private IEnumerator FallToGround()
        {
            if (CameraTransform == null)
                yield break;

            Vector3 startPosition = CameraTransform.position;
            Quaternion startRotation = CameraTransform.rotation;

            // Calculate target position (ground level at player position)
            Vector3 playerPosition = PlayerController != null ? PlayerController.transform.position : transform.position;
            Vector3 targetPosition = new Vector3(
                playerPosition.x,
                playerPosition.y + GroundHeightOffset,
                playerPosition.z
            );

            // Calculate target rotation (tilted sideways)
            // Keep current Y rotation (looking direction) but tilt on Z axis
            Vector3 currentEuler = startRotation.eulerAngles;
            Quaternion targetRotation = Quaternion.Euler(0f, currentEuler.y, TiltAngle);

            float elapsed = 0f;

            while (elapsed < FallDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FallDuration);

                // Apply curve to time
                float curvedT = FallCurve.Evaluate(t);

                // Interpolate position and rotation
                CameraTransform.position = Vector3.Lerp(startPosition, targetPosition, curvedT);
                CameraTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, curvedT);

                yield return null;
            }

            // Ensure final values
            CameraTransform.position = targetPosition;
            CameraTransform.rotation = targetRotation;
        }

        /// <summary>
        /// Resets camera to original position and rotation
        /// Call this after respawn, before fading back in
        /// </summary>
        public void ResetCamera()
        {
            if (CameraTransform == null)
                return;

            // Reset to original local position/rotation
            CameraTransform.localPosition = m_OriginalLocalPosition;
            CameraTransform.localRotation = m_OriginalLocalRotation;

            if (DebugMode)
            {
                Debug.Log("[DeathCameraController] Camera reset to original position");
            }
        }

        /// <summary>
        /// Immediately snaps camera to ground view (for testing)
        /// </summary>
        public void SnapToGroundView()
        {
            if (CameraTransform == null || PlayerController == null)
                return;

            Vector3 playerPosition = PlayerController.transform.position;
            CameraTransform.position = new Vector3(
                playerPosition.x,
                playerPosition.y + GroundHeightOffset,
                playerPosition.z
            );

            Vector3 currentEuler = CameraTransform.rotation.eulerAngles;
            CameraTransform.rotation = Quaternion.Euler(0f, currentEuler.y, TiltAngle);
        }

        /// <summary>
        /// Gets whether the death animation is currently playing
        /// </summary>
        public bool IsAnimating => m_IsAnimating;
    }
}
