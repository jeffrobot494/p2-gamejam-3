// SoundEmitter.cs (Unity Input System version)
using UnityEngine;
using UnityEngine.InputSystem; // New Input System

public class SoundEmitter : MonoBehaviour
{
    [Header("Defaults (used if EmitSound called without args)")]
    [Range(0f, 1f)] public float defaultLoudness = 1f;
    public float defaultQuality = 0f;

    [Header("Obstruction")]
    [Tooltip("Which layers count as walls/occluders for sound.")]
    public LayerMask wallMask;
    [Range(0.1f, 1f), Tooltip("Per-wall multiplier applied to loudness (e.g., 0.8 = -20% per wall).")]
    public float wallPenalty = 0.8f;

    [Header("Debug")]
    public bool drawDebug = false;

    [Header("Input (New Input System)")]
    [Tooltip("Optional: Reference an Input Action from an Input Actions asset (e.g., 'Gameplay/EmitSound'). If unset, a fallback action bound to <Keyboard>/e is used.")]
    public InputActionReference emitAction;

    // Fallback action if no InputActionReference is assigned
    private InputAction runtimeEmitAction;

    /// <summary>
    /// Emit a sound with specific loudness [0..1] and quality (free-form scalar).
    /// </summary>
    public void EmitSound(float loudness, float quality)
    {
        Sound.Spawn(transform.position, loudness, quality, wallMask, wallPenalty, drawDebug);
        DebugPopupText.Spawn(transform.position, $"Emit L:{loudness:0.00} Q:{quality:0.00}", Color.cyan);
    }

    /// <summary>
    /// Emit a sound using default settings.
    /// </summary>
    public void EmitSound()
    {
        EmitSound(defaultLoudness, defaultQuality);
    }

    private void OnEnable()
    {
        // Prefer referenced action (from actions asset)
        if (emitAction != null && emitAction.action != null)
        {
            emitAction.action.performed += OnEmitPerformed;
            emitAction.action.Enable();
        }
        else
        {
            // Fallback: create a simple runtime action bound to Keyboard 'E'
            if (runtimeEmitAction == null)
            {
                runtimeEmitAction = new InputAction(name: "EmitSound", binding: "<Keyboard>/e");
                runtimeEmitAction.performed += OnEmitPerformed;
            }
            runtimeEmitAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (emitAction != null && emitAction.action != null)
        {
            emitAction.action.performed -= OnEmitPerformed;
            emitAction.action.Disable();
        }

        if (runtimeEmitAction != null)
        {
            runtimeEmitAction.performed -= OnEmitPerformed;
            runtimeEmitAction.Disable();
        }
    }

    // Callback for the Input System action
    private void OnEmitPerformed(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
            EmitSound();
    }
}
