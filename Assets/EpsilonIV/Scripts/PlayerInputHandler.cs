using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EpsilonIV
{
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Input Actions")]
        [Tooltip("Input Action Asset containing player controls")]
        public InputActionAsset InputActionAsset;

        [Header("Sensitivity")]
        [Tooltip("Sensitivity multiplier for moving the camera around")]
        public float LookSensitivity = 1f;

        [Tooltip("Additional sensitivity multiplier for WebGL")]
        public float WebglLookSensitivityMultiplier = 0.25f;

        [Tooltip("Limit to consider an input when using a trigger on a controller")]
        public float TriggerAxisThreshold = 0.4f;

        [Tooltip("Used to flip the vertical input axis")]
        public bool InvertYAxis = false;

        [Tooltip("Used to flip the horizontal input axis")]
        public bool InvertXAxis = false;

        bool m_FireInputWasHeld;

        private InputAction m_MoveAction;
        private InputAction m_LookAction;
        private InputAction m_JumpAction;
        private InputAction m_FireAction;
        private InputAction m_AimAction;
        private InputAction m_SprintAction;
        private InputAction m_CrouchAction;
        private InputAction m_ReloadAction;
        private InputAction m_NextWeaponAction;
        private InputAction m_InteractAction;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (InputActionAsset != null)
            {
                var playerMap = InputActionAsset.FindActionMap("Player");
                if (playerMap != null)
                {
                    m_MoveAction = playerMap.FindAction("Move");
                    m_LookAction = playerMap.FindAction("Look");
                    m_JumpAction = playerMap.FindAction("Jump");
                    m_FireAction = playerMap.FindAction("Fire");
                    m_AimAction = playerMap.FindAction("Aim");
                    m_SprintAction = playerMap.FindAction("Sprint");
                    m_CrouchAction = playerMap.FindAction("Crouch");
                    m_ReloadAction = playerMap.FindAction("Reload");
                    m_NextWeaponAction = playerMap.FindAction("NextWeapon");
                    m_InteractAction = playerMap.FindAction("Interact");

                    m_MoveAction?.Enable();
                    m_LookAction?.Enable();
                    m_JumpAction?.Enable();
                    m_FireAction?.Enable();
                    m_AimAction?.Enable();
                    m_SprintAction?.Enable();
                    m_CrouchAction?.Enable();
                    m_ReloadAction?.Enable();
                    m_NextWeaponAction?.Enable();
                    m_InteractAction?.Enable();
                }
                else
                {
                    Debug.LogError("PlayerInputHandler: Could not find 'Player' action map!");
                }
            }
            else
            {
                Debug.LogError("PlayerInputHandler: InputActionAsset is not assigned! Please assign it in the Inspector.");
            }
        }

        void LateUpdate()
        {
            m_FireInputWasHeld = GetFireInputHeld();
        }

        public bool CanProcessInput()
        {
            return Cursor.lockState == CursorLockMode.Locked;
        }

        public Vector3 GetMoveInput()
        {
            if (CanProcessInput())
            {
                var input = m_MoveAction.ReadValue<Vector2>();
                Vector3 move = new Vector3(input.x, 0f, input.y);

                // constrain move input to a maximum magnitude of 1, otherwise diagonal movement might exceed the max move speed defined
                move = Vector3.ClampMagnitude(move, 1);

                return move;
            }

            return Vector3.zero;
        }

        public float GetLookInputsHorizontal()
        {
            if (!CanProcessInput())
                return 0.0f;
            
            float input = m_LookAction.ReadValue<Vector2>().x;

            if (InvertXAxis)
                input *= -1;

            input *= LookSensitivity;
            
#if UNITY_WEBGL
            // Mouse tends to be even more sensitive in WebGL due to mouse acceleration, so reduce it even more
            input *= WebglLookSensitivityMultiplier;
#endif

            return input;
        }

        public float GetLookInputsVertical()
        {
            if (!CanProcessInput())
                return 0.0f;
            
            float input = m_LookAction.ReadValue<Vector2>().y;

            if (InvertYAxis)
                input *= -1;

            input *= LookSensitivity;
            
#if UNITY_WEBGL
            // Mouse tends to be even more sensitive in WebGL due to mouse acceleration, so reduce it even more
            input *= WebglLookSensitivityMultiplier;
#endif

            return input;
        }

        public bool GetJumpInputDown()
        {
            if (CanProcessInput())
            {
                return m_JumpAction.WasPressedThisFrame();
            }

            return false;
        }

        public bool GetJumpInputHeld()
        {
            if (CanProcessInput())
            {
                return m_JumpAction.IsPressed();
            }

            return false;
        }

        public bool GetFireInputDown()
        {
            return GetFireInputHeld() && !m_FireInputWasHeld;
        }

        public bool GetFireInputReleased()
        {
            return !GetFireInputHeld() && m_FireInputWasHeld;
        }

        public bool GetFireInputHeld()
        {
            if (CanProcessInput())
            {
                return m_FireAction.IsPressed();
            }

            return false;
        }

        public bool GetAimInputHeld()
        {
            if (CanProcessInput())
            {
                return m_AimAction.IsPressed();
            }

            return false;
        }

        public bool GetSprintInputHeld()
        {
            if (CanProcessInput())
            {
                return m_SprintAction.IsPressed();
            }

            return false;
        }

        public bool GetCrouchInputDown()
        {
            if (CanProcessInput())
            {
                return m_CrouchAction.WasPressedThisFrame();
            }

            return false;
        }

        public bool GetCrouchInputReleased()
        {
            if (CanProcessInput())
            {
                return m_CrouchAction.WasReleasedThisFrame();
            }

            return false;
        }

        public bool GetReloadButtonDown()
        {
            if (CanProcessInput())
            {
                return m_ReloadAction.WasPressedThisFrame();
            }

            return false;
        }

        public int GetSwitchWeaponInput()
        {
            if (CanProcessInput())
            {
                var input = m_NextWeaponAction.ReadValue<float>();

                if (input > 0f)
                    return -1;
                
                if (input < 0f)
                    return 1;
            }

            return 0;
        }

        public int GetSelectWeaponInput()
        {
            if (CanProcessInput())
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame)
                    return 1;
                if (Keyboard.current.digit2Key.wasPressedThisFrame)
                    return 2;
                if (Keyboard.current.digit3Key.wasPressedThisFrame)
                    return 3;
                if (Keyboard.current.digit4Key.wasPressedThisFrame)
                    return 4;
                if (Keyboard.current.digit5Key.wasPressedThisFrame)
                    return 5;
                if (Keyboard.current.digit6Key.wasPressedThisFrame)
                    return 6;
                if (Keyboard.current.digit7Key.wasPressedThisFrame)
                    return 7;
                if (Keyboard.current.digit8Key.wasPressedThisFrame)
                    return 8;
                if (Keyboard.current.digit9Key.wasPressedThisFrame)
                    return 9;
            }

            return 0;
        }

        public bool GetInteractInputDown()
        {
            if (CanProcessInput())
            {
                return m_InteractAction.WasPressedThisFrame();
            }

            return false;
        }
    }
}