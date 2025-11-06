// EmitterDebugGizmo.cs (Editor-only Scene view labels)
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SoundEmitter))]
public class EmitterDebugGizmo : MonoBehaviour
{
    [System.NonSerialized] public float labelYOffset = 1.5f;
    [System.NonSerialized] public float sceneLabelDuration = 1.25f; // seconds

    private SoundEmitter _emitter;

    private void OnEnable()
    {
        if (_emitter == null) _emitter = GetComponent<SoundEmitter>();
    }

    private void OnDrawGizmos()
    {
        if (_emitter == null) return;
        if (!_emitter.debugSceneLabels) return;

        float age = Time.time - _emitter.lastEmitTime;
        if (age < 0f || age > sceneLabelDuration) return;

        // Optional fade color over time
        float t = Mathf.InverseLerp(sceneLabelDuration, 0f, age); // 1 -> 0
        Color c = Color.Lerp(new Color(0,1,1,0f), new Color(0,1,1,1f), t);

        using (new Handles.DrawingScope(c))
        {
            Vector3 pos = transform.position + Vector3.up * labelYOffset;
            Handles.Label(pos, $"Emit L:{_emitter.lastEmitLoudness:0.00} Q:{_emitter.lastEmitQuality:0.00}");
        }
    }
}
#endif
