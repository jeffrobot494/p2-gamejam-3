// DebugPopupText.cs (patched)
using UnityEngine;
using TMPro;

public class DebugPopupText : MonoBehaviour
{
    public float riseSpeed = 0.75f;
    public float lifetime = 1.2f;
    public float maxDistanceFace = 50f;
    public TMP_FontAsset fallbackFont; // optional: assign in Inspector
    public float fontSize = 2.2f;
    private TextMeshPro _tmp;
    private float _t;
    private Transform _cam;

    void Awake()
    {
        _tmp = GetComponent<TextMeshPro>();
        if (_tmp == null) _tmp = gameObject.AddComponent<TextMeshPro>();

        // Make sure we actually have a font/material to render with
        if (_tmp.font == null)
            _tmp.font = fallbackFont != null ? fallbackFont : TMP_Settings.defaultFontAsset;

        _tmp.alignment = TextAlignmentOptions.Center;
        _tmp.fontSize = 10.0f;                         // world units; bump if tiny in your scene
        _tmp.textWrappingMode = TextWrappingModes.NoWrap;
        _tmp.enableAutoSizing = false;
        _tmp.raycastTarget = false;                   // not UI, but keep things simple
        var col = _tmp.color; col.a = 1f; _tmp.color = col;
    }

    public void Show(string text, Color color)
    {
        _tmp.text = text;
        color.a = 1f;          // ensure fully opaque at spawn
        _tmp.color = color;
        _t = 0f;
        if (Camera.main) _cam = Camera.main.transform;
        transform.localScale = Vector3.one; // avoid accidental zero scale from parents
    }

    void Update()
    {
        _t += Time.deltaTime;
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        // Face camera (billboard)
        if (_cam)
        {
            // Billboard: face camera cleanly, no sideways tilt
            transform.LookAt(
                transform.position + _cam.forward,
                _cam.up
            );
        }


        // Correct fade: stay opaque until ~60% of lifetime, then fade to 0 by lifetime
        float fadeStart = lifetime * 0.6f;
        float fadeT = Mathf.InverseLerp(fadeStart, lifetime, _t);
        var c = _tmp.color; c.a = 1f - Mathf.Clamp01(fadeT); _tmp.color = c;

        if (_t >= lifetime) Destroy(gameObject);
    }

    public static void Spawn(Vector3 worldPos, string text, Color color)
    {
        var go = new GameObject("DebugPopupText");
        go.transform.position = worldPos + Vector3.up * 1.5f;
        var d = go.AddComponent<DebugPopupText>();
        d.Show(text, color);
    }
}
