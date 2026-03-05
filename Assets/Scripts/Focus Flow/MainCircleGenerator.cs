using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MainCircleGenerator : MonoBehaviour
{
    private const int Segments = 128;
    private const float Radius  = 0.5f;

    private void Awake()
    {
        GetComponent<MeshFilter>().sharedMesh = BuildDiscMesh(Segments, Radius);
    }

    private static Mesh BuildDiscMesh(int segments, float radius)
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices  = new Vector3[segments + 1];
        int[]     triangles = new int[segments * 6];

        vertices[0] = Vector3.zero;

        for (int i = 0; i < segments; i++)
        {
            float angle  = i * 2f * Mathf.PI / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments + 1;
            triangles[i * 6]     = 0;
            triangles[i * 6 + 1] = i + 1;
            triangles[i * 6 + 2] = next;
            triangles[i * 6 + 3] = 0;
            triangles[i * 6 + 4] = next;
            triangles[i * 6 + 5] = i + 1;
        }

        mesh.vertices  = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }
}
