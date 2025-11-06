using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Broadcasts sounds when the player types, making them detectable by aliens.
/// Discourages typing in favor of voice communication.
/// </summary>
[RequireComponent(typeof(SoundEmitter))]
public class TypingSoundBroadcaster : MonoBehaviour
{
    [Header("Sound Settings")]
    [Tooltip("Loudness of sound emitted per keystroke")]
    [Range(0f, 1f)]
    [SerializeField] private float keystrokeLoudness = 0.3f;

    [Tooltip("Quality of keystroke sound")]
    [SerializeField] private float keystrokeQuality = 0f;

    [Header("Filtering")]
    [Tooltip("Only broadcast when chat/input field is active (optional)")]
    [SerializeField] private bool onlyWhenChatActive = false;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private SoundEmitter soundEmitter;
    private InputAction anyKeyAction;

    private void Awake()
    {
        soundEmitter = GetComponent<SoundEmitter>();

        // Create an action that listens to ANY keyboard key
        anyKeyAction = new InputAction(
            name: "AnyKey",
            type: InputActionType.Button,
            binding: "<Keyboard>/<anyKey>"
        );

        // Subscribe to the action
        anyKeyAction.performed += OnAnyKeyPressed;
    }

    private void OnEnable()
    {
        anyKeyAction?.Enable();
    }

    private void OnDisable()
    {
        anyKeyAction?.Disable();
    }

    private void OnDestroy()
    {
        // Clean up
        if (anyKeyAction != null)
        {
            anyKeyAction.performed -= OnAnyKeyPressed;
            anyKeyAction.Dispose();
        }
    }

    private bool isChatActive = false;

    /// <summary>
    /// Called when chat input field gains focus. Subscribe this to InputFieldController.OnInputFieldFocused
    /// </summary>
    public void OnChatFocused()
    {
        isChatActive = true;
        if (debugMode)
        {
            Debug.Log("[TypingSoundBroadcaster] Chat focused - typing sounds will now broadcast");
        }
    }

    /// <summary>
    /// Called when chat input field loses focus. Subscribe this to InputFieldController.OnInputFieldUnfocused
    /// </summary>
    public void OnChatUnfocused()
    {
        isChatActive = false;
        if (debugMode)
        {
            Debug.Log("[TypingSoundBroadcaster] Chat unfocused - typing sounds disabled");
        }
    }

    private void OnAnyKeyPressed(InputAction.CallbackContext context)
    {
        // Get the actual keyboard key that was pressed
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Filter out control keys FIRST (Enter, Escape, etc.) that shouldn't make typing sounds
        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
        {
            if (debugMode)
            {
                Debug.Log($"[TypingSoundBroadcaster] Filtered control key");
            }
            return;
        }

        // Optional: Check if chat is active before broadcasting
        if (onlyWhenChatActive && !isChatActive)
        {
            if (debugMode)
            {
                Debug.Log($"[TypingSoundBroadcaster] Chat not active, skipping");
            }
            return;
        }

        if (debugMode)
        {
            Debug.Log($"[TypingSoundBroadcaster] BROADCASTING SOUND");
        }

        // Broadcast sound through SoundEmitter
        soundEmitter.EmitSound(keystrokeLoudness, keystrokeQuality);
    }
}
