using UnityEngine;

public class ButtonCircle : MonoBehaviour
{
    public enum ButtonType
    {
        A,
        B,
        X,
        Y
    }

    [Header("Button Settings")]
    [SerializeField] private ButtonType buttonType;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Material litMaterial;

    [Header("Ring Indicator")]
    [SerializeField] private Material redRingMaterial;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip buttonSound;
    [SerializeField] private AudioSource audioSource;

    private const float LIT_DURATION = 0.2f;
    private const float RING_DURATION = 1.0f;
    private float litTimer = 0f;
    private bool isLit = false;
    private bool isShowingRing = false;
    private float ringTimer = 0f;
    private GameObject ringObject;
    private Vector3 originalScale;

    public bool IsShowingRing => isShowingRing;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void Update()
    {
        if (isLit)
        {
            litTimer += Time.deltaTime;
            if (litTimer >= LIT_DURATION)
            {
                TurnOff();
            }
        }

        if (isShowingRing)
        {
            ringTimer += Time.deltaTime;
            if (ringTimer >= RING_DURATION)
            {
                HideRing();
            }
        }
    }

    public void LightUp()
    {
        isLit = true;
        litTimer = 0f;
        meshRenderer.material = litMaterial;
        PlayButtonSound();
    }

    private void PlayButtonSound()
    {
        if (buttonSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonSound);
        }
    }

    public void TurnOff()
    {
        isLit = false;
        litTimer = 0f;
        meshRenderer.material = defaultMaterial;
    }

    public void ShowRing()
    {
        isShowingRing = true;
        ringTimer = 0f;

        if (ringObject == null)
        {
            ringObject = new GameObject("RedRing");
            ringObject.transform.SetParent(transform);
            ringObject.transform.localPosition = new Vector3(0f, 0f, -0.3f);
            ringObject.transform.localRotation = Quaternion.identity;
            ringObject.transform.localScale = Vector3.one;

            CreateTorusRing(ringObject);
        }
        else
        {
            ringObject.SetActive(true);
        }
        PlayButtonSound();
    }

    private void CreateTorusRing(GameObject parent)
    {
        int segments = 32;
        int tubeSegments = 16;
        float radius = 0.65f;
        float tubeRadius = 0.08f;

        MeshFilter meshFilter = parent.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = parent.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(segments + 1) * (tubeSegments + 1)];
        int[] triangles = new int[segments * tubeSegments * 6];

        int vertIndex = 0;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            for (int j = 0; j <= tubeSegments; j++)
            {
                float tubeAngle = (float)j / tubeSegments * Mathf.PI * 2f;
                float tubeX = Mathf.Cos(tubeAngle) * tubeRadius;
                float tubeZ = Mathf.Sin(tubeAngle) * tubeRadius;

                Vector3 normal = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                vertices[vertIndex] = new Vector3(x + normal.x * tubeX, y + normal.y * tubeX, tubeZ);
                vertIndex++;
            }
        }

        int triIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            for (int j = 0; j < tubeSegments; j++)
            {
                int current = i * (tubeSegments + 1) + j;
                int next = current + tubeSegments + 1;

                triangles[triIndex++] = current;
                triangles[triIndex++] = next;
                triangles[triIndex++] = current + 1;

                triangles[triIndex++] = current + 1;
                triangles[triIndex++] = next;
                triangles[triIndex++] = next + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        meshRenderer.material = redRingMaterial;
        meshRenderer.material.renderQueue = 3000;
    }

    public void HideRing()
    {
        isShowingRing = false;
        ringTimer = 0f;

        if (ringObject != null)
        {
            ringObject.SetActive(false);
        }
    }
}
