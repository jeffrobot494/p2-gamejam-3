using UnityEngine;

public class ShowNormals : MonoBehaviour
{
    public float normalLength = 0.1f; // Length of the normal lines
    public Color normalColor = Color.yellow; // Color of the normal lines

    void OnDrawGizmos()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return;
        }

        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;

        Gizmos.color = normalColor;

        for (int i = 0; i < vertices.Length; i++)
        {
            // Transform vertex and normal from local space to world space
            Vector3 worldVertex = transform.TransformPoint(vertices[i]);
            Vector3 worldNormal = transform.TransformDirection(normals[i]);

            Gizmos.DrawRay(worldVertex, worldNormal * normalLength);
        }
    }
}
