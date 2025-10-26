// Sound.cs
using System.Collections.Generic;
using UnityEngine;

public class Sound : MonoBehaviour
{
    // Config
    private const float MinRadius = 0.5f;
    private const float MaxRadius = 50f;

    // Assigned at spawn
    private Vector3 sourcePos;
    private Vector3 sourceVelocity;
    private float originalLoudness; // [0..1]
    private float quality;
    private LayerMask wallMask;
    private float wallPenalty;      // e.g., 0.8f
    private bool drawDebug;

    // Reusable buffers to avoid GC
    private static readonly Collider[] overlapBuffer = new Collider[256];

    /// <summary>
    /// Factory to spawn a transient Sound processor.
    /// </summary>
    public static void Spawn(Vector3 pos, float loudness, float quality, LayerMask wallMask, float wallPenalty = 0.8f, bool drawDebug = false, Vector3 velocity = default)
    {
        var go = new GameObject("Sound");
        var s = go.AddComponent<Sound>();
        s.sourcePos = pos;
        s.sourceVelocity = velocity;
        s.originalLoudness = Mathf.Clamp01(loudness);
        s.quality = quality;
        s.wallMask = wallMask;
        s.wallPenalty = Mathf.Clamp(wallPenalty, 0.1f, 1f);
        s.drawDebug = drawDebug;
        go.transform.position = pos;
    }

    private void Start()
    {
        ProcessAndNotify();
        Destroy(gameObject); // one-shot
    }

    private void ProcessAndNotify()
    {
        if (originalLoudness <= 0f) return;

        // Quadratic scaling: quiet sounds have small radii, loud sounds reach max radius faster
        float radiusScale = originalLoudness * originalLoudness;
        float radius = Mathf.Lerp(MinRadius, MaxRadius, radiusScale);

        int count = Physics.OverlapSphereNonAlloc(sourcePos, radius, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);

        if (drawDebug)
        {
            DrawSphereGizmo(sourcePos, radius, 0.75f);
        }

        for (int i = 0; i < count; i++)
        {
            var col = overlapBuffer[i];
            if (!col) continue;

            // Identify listeners by component, not tags
            if (!col.TryGetComponent<Listener>(out var listener)) continue;

            Vector3 listenerPos = listener.transform.position;
            float distance = Vector3.Distance(sourcePos, listenerPos);
            if (distance > radius) continue; // out of range

            // Distance falloff (inverse square)
            // Normalize distance to [0..1], then apply inverse square formula
            float normalizedDistance = distance / radius;
            float falloff = 1f - (normalizedDistance * normalizedDistance);
            float heardLoudness = originalLoudness * falloff;
            if (heardLoudness <= 0f) continue;

            // Obstruction via RaycastAll against wallMask only
            Vector3 dir = (listenerPos - sourcePos);
            Vector3 dirNorm = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector3.forward;

            // Only check colliders on wallMask to avoid counting listener or other geometry
            RaycastHit[] hits = Physics.RaycastAll(sourcePos, dirNorm, distance, wallMask, QueryTriggerInteraction.Ignore);

            int wallCount = 0;
            if (hits != null && hits.Length > 0)
            {
                // Optionally sort by distance to be explicit; Physics returns sorted by distance already.
                wallCount = hits.Length;
                // Apply attenuation: loudness *= wallPenalty^wallCount
                heardLoudness *= Mathf.Pow(wallPenalty, wallCount);

                if (drawDebug)
                {
                    Debug.DrawLine(sourcePos, listenerPos, new Color(1f, 0.5f, 0f, 1f), 0.25f); // orange line to listener
                }
            }
            else if (drawDebug)
            {
                Debug.DrawLine(sourcePos, listenerPos, Color.green, 0.25f); // clear line of sight
            }

            // Notify the listener (they decide based on their threshold)
            listener.CheckSound(heardLoudness, sourcePos, quality, sourceVelocity);
        }
    }

    // --- Debug helpers ---
    private void DrawSphereGizmo(Vector3 center, float radius, float duration)
    {
        // Approximate with 3 circles
        int segments = 32;
        float step = Mathf.PI * 2f / segments;

        void DrawCircle(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 prev = center + a * radius;
            for (int i = 1; i <= segments; i++)
            {
                float t = i * step;
                Vector3 p = center + (a * Mathf.Cos(t) + b * Mathf.Sin(t)) * radius + c * 0f;
                Debug.DrawLine(prev, p, Color.cyan, duration);
                prev = p;
            }
        }

        DrawCircle(Vector3.right, Vector3.forward, Vector3.zero);
        DrawCircle(Vector3.up, Vector3.right, Vector3.zero);
        DrawCircle(Vector3.up, Vector3.forward, Vector3.zero);
    }
}
