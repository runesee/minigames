using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SpeedometerDisplay : MonoBehaviour
{
    [Header("Arc Geometry")]
    [Tooltip("Inner radius of the ring arc in local units.")]
    [SerializeField] private float innerRadius = 0.55f;
    [Tooltip("Outer radius of the ring arc in local units.")]
    [SerializeField] private float outerRadius = 0.72f;
    [Tooltip("Total angle (degrees) covered by the arc. The remaining gap faces downward.")]
    [SerializeField] private float totalArcDegrees = 240f;
    [Tooltip("Angle (CCW from +X axis) at which the arc is centered. 90 = top of circle, gap at bottom.")]
    [SerializeField] private float arcCenterAngleDeg = 90f;
    [Tooltip("Number of quads per zone segment. Higher values produce a smoother arc.")]
    [SerializeField] private int arcSubdivisions = 20;
    [Tooltip("Angular gap in degrees between adjacent zone segments.")]
    [SerializeField] private float zoneGapDegrees = 2f;

    [Header("Needle")]
    [SerializeField] private float needleBaseWidth = 0.025f;
    [SerializeField] private Color needleColor = Color.white;

    [Header("Zone Colors")]
    [Tooltip("Index 0 = minimum speed (green), index 4 = maximum speed (red).")]
    [SerializeField]
    private Color[] zoneColors = new Color[]
    {
        new Color(0.00f, 0.80f, 0.20f),
        new Color(0.55f, 0.90f, 0.10f),
        new Color(1.00f, 0.85f, 0.00f),
        new Color(1.00f, 0.45f, 0.00f),
        new Color(0.90f, 0.10f, 0.10f),
    };

    private const int ZoneCount = 5;
    private const float BrightnessMultiplier = 1.8f;
    // Local Z offsets so each layer renders cleanly in front of the one behind it.
    // Negative Z = closer to the camera (which is at world Z = -10).
    private const float ArcDepthZ = 0.00f;
    private const float NeedleDepthZ = -0.05f;

    private Material[] zoneMaterials;
    private readonly List<Material> allRuntimeMaterials = new List<Material>();
    private readonly List<GameObject> generatedObjects = new List<GameObject>();
    private Transform needlePivot;
    private int currentActiveZone = -1;

    private float minSpeedAngleDeg;
    private float maxSpeedAngleDeg;

    private void OnEnable()
    {
        DestroyAllChildren();
        Rebuild();
    }

    private void OnDisable()
    {
        Cleanup();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && isActiveAndEnabled)
                Rebuild();
        };
    }
#endif

    private void Rebuild()
    {
        Cleanup();

        minSpeedAngleDeg = arcCenterAngleDeg + totalArcDegrees / 2f;
        maxSpeedAngleDeg = arcCenterAngleDeg - totalArcDegrees / 2f;

        BuildArcSegments();
        BuildNeedle();
    }

    private void DestroyAllChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
        generatedObjects.Clear();
        allRuntimeMaterials.Clear();
        zoneMaterials = null;
        needlePivot = null;
        currentActiveZone = -1;
    }

    private void Cleanup()
    {
        foreach (GameObject obj in generatedObjects)
        {
            if (obj == null) continue;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
        generatedObjects.Clear();

        foreach (Material mat in allRuntimeMaterials)
        {
            if (mat == null) continue;
            if (Application.isPlaying) Destroy(mat);
            else DestroyImmediate(mat);
        }
        allRuntimeMaterials.Clear();

        zoneMaterials = null;
        needlePivot = null;
        currentActiveZone = -1;
    }

    private void BuildArcSegments()
    {
        float degreesPerZone = totalArcDegrees / ZoneCount;
        float halfGap = zoneGapDegrees / 2f;

        zoneMaterials = new Material[ZoneCount];

        for (int i = 0; i < ZoneCount; i++)
        {
            float segStart = minSpeedAngleDeg - i * degreesPerZone + halfGap;
            float segEnd = minSpeedAngleDeg - (i + 1) * degreesPerZone - halfGap;

            GameObject segObj = new GameObject($"SpeedometerZone_{i}");
            segObj.transform.SetParent(transform, false);
            segObj.transform.localPosition = new Vector3(0f, 0f, ArcDepthZ);
            generatedObjects.Add(segObj);

            MeshFilter mf = segObj.AddComponent<MeshFilter>();
            MeshRenderer mr = segObj.AddComponent<MeshRenderer>();
            mf.sharedMesh = BuildArcMesh(segStart, segEnd, innerRadius, outerRadius, arcSubdivisions);

            Color baseColor = i < zoneColors.Length ? zoneColors[i] : Color.white;
            Material mat = CreateMaterial(baseColor);
            mr.sharedMaterial = mat;

            zoneMaterials[i] = mat;
        }
    }

    private Mesh BuildArcMesh(float startDeg, float endDeg, float inner, float outer, int steps)
    {
        int vertCount = (steps + 1) * 2;
        Vector3[] verts = new Vector3[vertCount];
        int[] tris = new int[steps * 12];

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            float rad = Mathf.Lerp(startDeg, endDeg, t) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            verts[i * 2] = new Vector3(cos * inner, sin * inner, 0f);
            verts[i * 2 + 1] = new Vector3(cos * outer, sin * outer, 0f);
        }

        for (int i = 0; i < steps; i++)
        {
            int bi = i * 12;
            int vi = i * 2;

            tris[bi] = vi;
            tris[bi + 1] = vi + 2;
            tris[bi + 2] = vi + 1;
            tris[bi + 3] = vi + 2;
            tris[bi + 4] = vi + 3;
            tris[bi + 5] = vi + 1;

            tris[bi + 6] = vi;
            tris[bi + 7] = vi + 1;
            tris[bi + 8] = vi + 2;
            tris[bi + 9] = vi + 2;
            tris[bi + 10] = vi + 1;
            tris[bi + 11] = vi + 3;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }

    private void BuildNeedle()
    {
        needlePivot = new GameObject("NeedlePivot").transform;
        needlePivot.SetParent(transform, false);
        needlePivot.localPosition = new Vector3(0f, 0f, NeedleDepthZ);
        generatedObjects.Add(needlePivot.gameObject);

        GameObject markerObj = new GameObject("SpeedometerMarker");
        markerObj.transform.SetParent(needlePivot, false);

        MeshFilter mf = markerObj.AddComponent<MeshFilter>();
        MeshRenderer mr = markerObj.AddComponent<MeshRenderer>();

        float halfWidth = needleBaseWidth * 0.5f;

        Mesh markerMesh = new Mesh();
        markerMesh.vertices = new Vector3[]
        {
            new Vector3(innerRadius,  halfWidth, 0f),
            new Vector3(outerRadius,  halfWidth, 0f),
            new Vector3(outerRadius, -halfWidth, 0f),
            new Vector3(innerRadius, -halfWidth, 0f),
        };
        markerMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2 };
        markerMesh.RecalculateNormals();
        mf.sharedMesh = markerMesh;

        mr.sharedMaterial = CreateMaterial(needleColor);
    }

    private void BuildHub()
    {
        const int hubSteps = 24;

        GameObject hubObj = new GameObject("SpeedometerHub");
        hubObj.transform.SetParent(transform, false);
        hubObj.transform.localPosition = new Vector3(0f, 0f, NeedleDepthZ);
        generatedObjects.Add(hubObj);

        MeshFilter mf = hubObj.AddComponent<MeshFilter>();
        MeshRenderer mr = hubObj.AddComponent<MeshRenderer>();

        Vector3[] verts = new Vector3[hubSteps + 1];
        int[] tris = new int[hubSteps * 6];
        verts[0] = Vector3.zero;

        for (int i = 0; i < hubSteps; i++)
        {
            float rad = i * (2f * Mathf.PI / hubSteps);
            verts[i + 1] = new Vector3(Mathf.Cos(rad) * 0.04f, Mathf.Sin(rad) * 0.04f, 0f);
        }

        for (int i = 0; i < hubSteps; i++)
        {
            int next = (i + 1) % hubSteps + 1;
            tris[i * 6] = 0;
            tris[i * 6 + 1] = i + 1;
            tris[i * 6 + 2] = next;
            tris[i * 6 + 3] = 0;
            tris[i * 6 + 4] = next;
            tris[i * 6 + 5] = i + 1;
        }

        Mesh hubMesh = new Mesh();
        hubMesh.vertices = verts;
        hubMesh.triangles = tris;
        hubMesh.RecalculateNormals();
        mf.sharedMesh = hubMesh;

        mr.sharedMaterial = CreateMaterial(needleColor);
    }

    private Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null)
        {
            Debug.LogError("[SpeedometerDisplay] Unlit/Color shader not found.");
            shader = Shader.Find("Standard");
        }

        Material mat = new Material(shader);
        mat.color = color;

        allRuntimeMaterials.Add(mat);
        return mat;
    }

    public void SetNormalizedSpeed(float normalizedSpeed)
    {
        if (needlePivot == null) return;
        float angle = Mathf.Lerp(minSpeedAngleDeg, maxSpeedAngleDeg, normalizedSpeed);
        needlePivot.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void HighlightZone(int zoneIndex)
    {
        if (zoneMaterials == null || zoneIndex == currentActiveZone) return;

        if (currentActiveZone >= 0 && currentActiveZone < ZoneCount)
            zoneMaterials[currentActiveZone].color = zoneColors[currentActiveZone];

        currentActiveZone = zoneIndex;

        if (currentActiveZone >= 0 && currentActiveZone < ZoneCount)
            zoneMaterials[currentActiveZone].color = zoneColors[currentActiveZone] * BrightnessMultiplier;
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}
