// ListenerDebugLabel.cs (from earlier; unchanged except using NoWrap already)
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Listener))]
public class ListenerDebugLabel : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 2.0f, 0);
    public float fontSize = 2f;
    public Color idleColor = new Color(1f,1f,1f,0.75f);
    public Color checkingColor = Color.yellow;
    public float checkingFlashTime = 0.35f;

    private TextMeshPro _tmp;
    private Transform _cam;
    private float _flashTimer;

    void Awake()
    {
        var child = new GameObject("ListenerLabel");
        child.transform.SetParent(transform, false);
        child.transform.localPosition = offset;

        _tmp = child.AddComponent<TextMeshPro>();
        _tmp.alignment = TextAlignmentOptions.Center;
        _tmp.fontSize = fontSize;
        _tmp.textWrappingMode = TextWrappingModes.NoWrap;
        _tmp.text = "Idle";
        _tmp.color = idleColor;
    }

    void OnEnable() { if (Camera.main) _cam = Camera.main.transform; }

    void LateUpdate()
    {
        if (_cam) transform.GetChild(0).transform.LookAt(transform.GetChild(0).position + _cam.forward, _cam.up);

        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f)
            {
                _tmp.text = "Idle";
                _tmp.color = idleColor;
            }
        }
    }

    public void ShowChecking(float loudness, float quality)
    {
        _tmp.text = $"Checking Sound\nL:{loudness:0.00} Q:{quality:0.00}";
        _tmp.color = checkingColor;
        _flashTimer = checkingFlashTime;
    }
}
