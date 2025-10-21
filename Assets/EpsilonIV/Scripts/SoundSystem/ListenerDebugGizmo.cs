// ListenerDebugGizmo.cs (Editor-only Scene view labels)
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Listener))]
public class ListenerDebugGizmo : MonoBehaviour
{
    public float labelYOffset = 2.0f;
    public float sceneLabelDuration = 0.5f; // how long "Checking Sound" stays visible

    [HideInInspector] public float lastCheckTime;
    [HideInInspector] public float lastLoudness;
    [HideInInspector] public float lastQuality;

    // Toggle per-listener if desired
    public bool enableSceneLabel = true;

    private void OnDrawGizmos()
    {
        if (!enableSceneLabel) return;

        float age = Time.time - lastCheckTime;
        if (age < 0f || age > sceneLabelDuration) return;

        float t = Mathf.InverseLerp(sceneLabelDuration, 0f, age);
        Color c = Color.Lerp(new Color(1f,1f,0f,0f), new Color(1f,1f,0f,1f), t);

        using (new Handles.DrawingScope(c))
        {
            Vector3 pos = transform.position + Vector3.up * labelYOffset;
            Handles.Label(pos, $"Checking Sound\nL:{lastLoudness:0.00} Q:{lastQuality:0.00}");
        }
    }
}
#endif
