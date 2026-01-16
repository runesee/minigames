using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PyramidMesh : MonoBehaviour
{
    [SerializeField] private float size = 1f;
    [SerializeField] private bool pointDown = true;

    private void Awake()
    {
        CreatePyramid();
    }

    public void CreatePyramid()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.name = "Pyramid";

        float height = size;
        float baseSize = size;
        float tip = pointDown ? -height : height;

        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, tip, 0),
            new Vector3(-baseSize, 0, -baseSize),
            new Vector3(baseSize, 0, -baseSize),
            new Vector3(baseSize, 0, baseSize),
            new Vector3(-baseSize, 0, baseSize)
        };

        int[] triangles = new int[]
        {
            0, 2, 1,
            0, 3, 2,
            0, 4, 3,
            0, 1, 4,
            1, 2, 3,
            1, 3, 4
        };

        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0.5f, 1f),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
    }
}
