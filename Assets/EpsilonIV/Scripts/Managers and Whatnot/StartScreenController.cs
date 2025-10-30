using UnityEngine;

namespace EpsilonIV
{
    /// <summary>
    /// Simple controller for start screen - only allows mouse look
    /// Player can look around while waiting to start the game
    /// </summary>
    [RequireComponent(typeof(PlayerInputHandler))]
    public class StartScreenController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the camera used for mouse look")]
        public Camera PlayerCamera;

        [Header("Rotation")]
        [Tooltip("Rotation speed for moving the camera")]
        public float RotationSpeed = 200f;

        // Private state
        private PlayerInputHandler m_InputHandler;
        private float m_CameraVerticalAngle = 0f;

        void Start()
        {
            m_InputHandler = GetComponent<PlayerInputHandler>();

            if (PlayerCamera == null)
            {
                PlayerCamera = GetComponentInChildren<Camera>();
            }

            // Initialize camera angle
            m_CameraVerticalAngle = PlayerCamera.transform.localEulerAngles.x;
        }

        void Update()
        {
            HandleMouseLook();
        }

        void HandleMouseLook()
        {
            // Horizontal character rotation
            transform.Rotate(
                new Vector3(0f, m_InputHandler.GetLookInputsHorizontal() * RotationSpeed, 0f),
                Space.Self);

            // Vertical camera rotation
            m_CameraVerticalAngle += m_InputHandler.GetLookInputsVertical() * RotationSpeed;

            // Limit the camera's vertical angle to min/max
            m_CameraVerticalAngle = Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);

            // Apply the vertical angle as a local rotation to the camera transform
            PlayerCamera.transform.localEulerAngles = new Vector3(m_CameraVerticalAngle, 0, 0);
        }

        /// <summary>
        /// Gets the current camera vertical angle (for transitioning to other controllers)
        /// </summary>
        public float GetCameraVerticalAngle()
        {
            return m_CameraVerticalAngle;
        }

        /// <summary>
        /// Sets the camera vertical angle (for transitioning from other controllers)
        /// </summary>
        public void SetCameraVerticalAngle(float angle)
        {
            m_CameraVerticalAngle = angle;
        }
    }
}
