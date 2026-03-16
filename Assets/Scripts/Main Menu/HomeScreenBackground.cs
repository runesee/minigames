using UnityEngine;

public class HomeScreenBackground : MonoBehaviour
{
    [Header("Gradient Colors")]
    [SerializeField] private Color topColor = new Color(0.08f, 0.06f, 0.18f);
    [SerializeField] private Color bottomColor = new Color(0.02f, 0.02f, 0.06f);

    private void Start()
    {
        CreateGradientQuad();
    }

    private void CreateGradientQuad()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[HomeScreenBackground] No main camera found.");
            return;
        }

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "BackgroundGradient";
        quad.transform.SetParent(transform);

        Collider col = quad.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        float distance = cam.farClipPlane - 1f;
        float height = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * cam.aspect;

        quad.transform.position = cam.transform.position + cam.transform.forward * distance;
        quad.transform.rotation = cam.transform.rotation;
        quad.transform.localScale = new Vector3(width * 1.2f, height * 1.2f, 1f);

        Mesh mesh = quad.GetComponent<MeshFilter>().mesh;
        Color[] colors = new Color[mesh.vertexCount];
        colors[0] = bottomColor;
        colors[1] = bottomColor;
        colors[2] = topColor;
        colors[3] = topColor;
        mesh.colors = colors;

        Material gradientMat = new Material(Shader.Find("Sprites/Default"));
        gradientMat.renderQueue = 1000;
        quad.GetComponent<MeshRenderer>().material = gradientMat;
    }
}
