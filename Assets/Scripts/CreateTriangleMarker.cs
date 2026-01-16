using UnityEngine;

public class CreateTriangleMarker : MonoBehaviour
{
    [ContextMenu("Setup Triangle Marker")]
    public void SetupMarker()
    {
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arrow.name = "Arrow";
        arrow.transform.SetParent(transform);
        arrow.transform.localPosition = new Vector3(0, 0, 0);
        arrow.transform.localRotation = Quaternion.identity;
        arrow.transform.localScale = new Vector3(0.6f, 0.05f, 0.6f);
        DestroyImmediate(arrow.GetComponent<Collider>());

        GameObject triangle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        triangle.name = "Triangle";
        triangle.transform.SetParent(transform);
        triangle.transform.localPosition = new Vector3(0, -0.3f, 0);
        triangle.transform.localRotation = Quaternion.identity;
        triangle.transform.localScale = new Vector3(0.8f, 0.4f, 0.8f);
        DestroyImmediate(triangle.GetComponent<Collider>());

        Material markerMat = Resources.Load<Material>("Materials/TaggedMarkerMaterial");
        if (markerMat == null)
        {
            Debug.LogWarning("Material not found, creating default red material");
            markerMat = new Material(Shader.Find("Standard"));
            markerMat.color = Color.red;
            markerMat.EnableKeyword("_EMISSION");
            markerMat.SetColor("_EmissionColor", Color.red * 2f);
        }

        arrow.GetComponent<MeshRenderer>().material = markerMat;
        triangle.GetComponent<MeshRenderer>().material = markerMat;
    }
}
