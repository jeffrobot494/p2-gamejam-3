// CameraClickSoundSpawner.cs
using UnityEngine;
using UnityEngine.InputSystem; // New Input System

public class CameraClickSoundSpawner : MonoBehaviour
{
    [Header("Camera & Targeting")]
    public Camera cam;                          // If null, uses Camera.main
    public LayerMask groundMask = ~0;           // What counts as "ground" to place the sound on
    public float maxRayDistance = 2000f;

    [Header("Sound Params")]
    [Range(0f, 1f)] public float loudness = 0.5f;
    [Range(0f, 1f)] public float minLoudness = 0.05f;
    [Range(0f, 1f)] public float maxLoudness = 1f;
    public float quality = 0f;

    [Header("Occlusion (same as your SoundEmitter)")]
    public LayerMask wallMask;
    [Range(0.1f, 1f)] public float wallPenalty = 0.8f;
    public bool drawDebug = true;

    [Header("Input")]
    [Tooltip("Mouse wheel notches per 0.05 loudness change (approx). Larger = slower changes.")]
    public float scrollNotchToLoudness = 0.05f;

    [Header("Optional Runtime Feedback")]
    public bool popupOnEmit = true; // uses DebugPopupText if present in project

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (cam == null) return;

        // --- Adjust loudness via mouse wheel (Input System) ---
        // Windows wheel typically reports ~120 per notch. Normalize by 120.
        if (Mouse.current != null)
        {
            float wheel = Mouse.current.scroll.ReadValue().y; // positive = up
            if (Mathf.Abs(wheel) > 0.01f)
            {
                float delta = (wheel / 120f) * scrollNotchToLoudness;
                loudness = Mathf.Clamp(loudness + delta, minLoudness, maxLoudness);
                // Optional: show current loudness in Console
                // Debug.Log($"Loudness: {loudness:0.00}");
            }

            // --- Left click to emit at ground under cursor ---
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundMask, QueryTriggerInteraction.Ignore))
                {
                    // Emit the sound at hit.point
                    Sound.Spawn(hit.point, loudness, quality, wallMask, wallPenalty, drawDebug);

                    if (popupOnEmit)
                    {
                        // Optional runtime popup label (cyan)
                        var method = typeof(DebugPopupText).GetMethod("Spawn");
                        if (method != null) // only if your DebugPopupText exists
                            DebugPopupText.Spawn(hit.point, $"Emit L:{loudness:0.00} Q:{quality:0.00}", Color.cyan);
                    }
                }
            }
        }
    }
}
