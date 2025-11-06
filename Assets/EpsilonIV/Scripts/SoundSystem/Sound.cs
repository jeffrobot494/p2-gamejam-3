// Sound.cs
using System.Collections.Generic;
using UnityEngine;

public class Sound : MonoBehaviour
{
    // Config
    private const float MinRadius = 0.5f;
    private const float MaxRadius = 50f;
    private const float CapsuleHeight = 4f; // Height limit for vertical sound propagation (prevents floor-to-floor sound travel)

    // Assigned at spawn
    private Vector3 sourcePos;
    private Vector3 sourceVelocity;
    private float originalLoudness; // [0..1]
    private float quality;
    private LayerMask wallMask;
    private float wallPenalty;      // e.g., 0.8f
    private bool drawDebug;
    private float capsuleHeight;
    private Quaternion sourceRotation;

    // Reusable buffers to avoid GC
    private static readonly Collider[] overlapBuffer = new Collider[256];

    /// <summary>
    /// Factory to spawn a transient Sound processor.
    /// </summary>
    public static void Spawn(Vector3 pos, float loudness, float quality, LayerMask wallMask, float wallPenalty = 0.8f, bool drawDebug = false, Vector3 velocity = default, float capsuleHeight = 0f, Quaternion rotation = default)
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
        s.capsuleHeight = capsuleHeight > 0f ? capsuleHeight : CapsuleHeight;
        s.sourceRotation = rotation == default ? Quaternion.identity : rotation;
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

        // Use box cast to limit vertical propagation with independent control of horizontal and vertical extent
        // Box gives us precise control: wide horizontally (radius), short vertically (capsuleHeight)
        // Box rotates with source to propagate sound in the direction the source is facing
        Vector3 boxHalfExtents = new Vector3(radius, capsuleHeight * 0.5f, radius);

        int count = Physics.OverlapBoxNonAlloc(sourcePos, boxHalfExtents, overlapBuffer, sourceRotation, ~0, QueryTriggerInteraction.Ignore);

        if (drawDebug)
        {
            DrawBoxGizmo(sourcePos, radius, capsuleHeight, sourceRotation, 0.75f);
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
    private void DrawBoxGizmo(Vector3 center, float radius, float height, Quaternion rotation, float duration)
    {
        float halfHeight = height * 0.5f;

        // Define the 8 corners of the box in local space
        Vector3[] localCorners = new Vector3[8];
        localCorners[0] = new Vector3(-radius, -halfHeight, -radius); // bottom front left
        localCorners[1] = new Vector3(radius, -halfHeight, -radius);  // bottom front right
        localCorners[2] = new Vector3(radius, -halfHeight, radius);   // bottom back right
        localCorners[3] = new Vector3(-radius, -halfHeight, radius);  // bottom back left
        localCorners[4] = new Vector3(-radius, halfHeight, -radius);  // top front left
        localCorners[5] = new Vector3(radius, halfHeight, -radius);   // top front right
        localCorners[6] = new Vector3(radius, halfHeight, radius);    // top back right
        localCorners[7] = new Vector3(-radius, halfHeight, radius);   // top back left

        // Rotate corners and move to world position
        Vector3[] corners = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            corners[i] = center + rotation * localCorners[i];
        }

        // Draw bottom face
        Debug.DrawLine(corners[0], corners[1], Color.cyan, duration);
        Debug.DrawLine(corners[1], corners[2], Color.cyan, duration);
        Debug.DrawLine(corners[2], corners[3], Color.cyan, duration);
        Debug.DrawLine(corners[3], corners[0], Color.cyan, duration);

        // Draw top face
        Debug.DrawLine(corners[4], corners[5], Color.cyan, duration);
        Debug.DrawLine(corners[5], corners[6], Color.cyan, duration);
        Debug.DrawLine(corners[6], corners[7], Color.cyan, duration);
        Debug.DrawLine(corners[7], corners[4], Color.cyan, duration);

        // Draw vertical edges
        Debug.DrawLine(corners[0], corners[4], Color.cyan, duration);
        Debug.DrawLine(corners[1], corners[5], Color.cyan, duration);
        Debug.DrawLine(corners[2], corners[6], Color.cyan, duration);
        Debug.DrawLine(corners[3], corners[7], Color.cyan, duration);
    }
}
