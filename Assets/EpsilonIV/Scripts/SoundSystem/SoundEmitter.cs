// SoundEmitter.cs (additions for debug toggles and last-emit tracking)
using UnityEngine;
using UnityEngine.InputSystem;

public class SoundEmitter : MonoBehaviour
{
    [Header("Defaults (used if EmitSound called without args)")]
    [Range(0f, 1f)] public float defaultLoudness = 1f;
    public float defaultQuality = 0f;

    [Header("Obstruction")]
    public LayerMask wallMask;
    [Range(0.1f, 1f)] public float wallPenalty = 0.8f;

    [Header("Debug")]
    public bool debugRuntimePopups = true;   // runtime TMP popups
    public bool debugSceneLabels  = true;    // Scene-view labels via Handles
    public bool drawDebug = false;           // rays/lines from your Sound.cs

    [Header("Input (New Input System)")]
    public InputActionReference emitAction;

    // Fallback runtime action
    private InputAction runtimeEmitAction;

    // --- Debug state (read by Scene gizmo) ---
    [HideInInspector] public float lastEmitLoudness;
    [HideInInspector] public float lastEmitQuality;
    [HideInInspector] public float lastEmitTime;   // Time.time when emitted

    public void EmitSound(float loudness, float quality)
    {
        // Spawn the actual sound
        Sound.Spawn(transform.position, loudness, quality, wallMask, wallPenalty, drawDebug);

        // Record debug state for Scene labels
        lastEmitLoudness = loudness;
        lastEmitQuality  = quality;
        lastEmitTime     = Time.time;

        // Optional runtime popup
        if (debugRuntimePopups)
        {
            DebugPopupText.Spawn(transform.position, $"Emit L:{loudness:0.00} Q:{quality:0.00}", Color.cyan);
        }
    }

    public void EmitSound() => EmitSound(defaultLoudness, defaultQuality);

    private void OnEnable()
    {
        if (emitAction != null && emitAction.action != null)
        {
            emitAction.action.performed += OnEmitPerformed;
            emitAction.action.Enable();
        }
        else
        {
            if (runtimeEmitAction == null)
            {
                runtimeEmitAction = new InputAction("EmitSound", binding: "<Keyboard>/e");
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

    private void OnEmitPerformed(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) EmitSound();
    }
}
