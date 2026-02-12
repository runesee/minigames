using UnityEngine;

public class SpeedIndicator : MonoBehaviour
{
    [SerializeField] private ScoreMultiplierManager multiplierManager;

    private const float MinYPosition = -0.5f;
    private const float MaxYPosition = 0.5f;
    private const int ZoneCount = 5;
    private const float BrightnessMultiplier = 1.8f;
    private const float WalkThreshold = 0.1f;
    private const float SprintThreshold = 0.6f;

    private MeshRenderer[] zoneRenderers;
    private Color[] baseColors;
    private int currentActiveZone = -1;
    private Animator characterAnimator;
    private GameObject speedArrow;

    public int GetCurrentZoneIndex()
    {
        return ZoneCount - 1 - currentActiveZone;
    }

    private void Start()
    {
        zoneRenderers = new MeshRenderer[ZoneCount];
        baseColors = new Color[ZoneCount];

        string[] zoneNames = { "Zone1_Green", "Zone2_YellowGreen", "Zone3_Yellow", "Zone4_Orange", "Zone5_Red" };

        for (int i = 0; i < ZoneCount; i++)
        {
            Transform zoneTransform = transform.parent.Find(zoneNames[i]);
            zoneRenderers[i] = zoneTransform.GetComponent<MeshRenderer>();
            baseColors[i] = zoneRenderers[i].material.color;
        }

        characterAnimator = GetComponentInChildren<Animator>();
        CreateSpeedArrow();
    }

    private void CreateSpeedArrow()
    {
        speedArrow = new GameObject("SpeedArrow");
        speedArrow.transform.SetParent(transform);
        speedArrow.transform.localPosition = new Vector3(0f, 0f, 0.2f);
        speedArrow.transform.localRotation = Quaternion.Euler(0f, 50f, 180f);
        speedArrow.transform.localScale = new Vector3(3f, 3f, 3f);

        MeshFilter meshFilter = speedArrow.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = speedArrow.AddComponent<MeshRenderer>();

        Mesh arrowMesh = new Mesh();
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0f, 0.15f, 0f),
            new Vector3(0f, -0.15f, 0f),
            new Vector3(0.2f, 0f, 0f)
        };
        int[] triangles = new int[] { 0, 1, 2 };

        arrowMesh.vertices = vertices;
        arrowMesh.triangles = triangles;
        arrowMesh.RecalculateNormals();

        meshFilter.mesh = arrowMesh;

        Material arrowMaterial = new Material(Shader.Find("Standard"));
        arrowMaterial.color = Color.white;
        arrowMaterial.SetFloat("_Metallic", 0f);
        arrowMaterial.SetFloat("_Glossiness", 0.3f);
        meshRenderer.material = arrowMaterial;
    }

    private void Update()
    {
        float normalizedSpeed;
        
        if (multiplierManager != null && multiplierManager.IsTracking)
        {
            normalizedSpeed = multiplierManager.GetAverageNormalizedSpeed();
        }
        else
        {
            normalizedSpeed = Mathf.Clamp(PlayPulse.Input.Input.Speed, 0.0f, 1.0f);
        }

        float yPosition = Mathf.Lerp(MinYPosition, MaxYPosition, normalizedSpeed);

        transform.localPosition = new Vector3(
            transform.localPosition.x,
            yPosition,
            transform.localPosition.z
        );

        if (characterAnimator != null)
        {
            bool isSprinting = normalizedSpeed > SprintThreshold;
            bool isWalking = !isSprinting && normalizedSpeed > WalkThreshold;

            characterAnimator.SetBool("isWalking", isWalking);
            characterAnimator.SetBool("isSprinting", isSprinting);
        }

        UpdateZoneHighlight(normalizedSpeed);
    }

    private void UpdateZoneHighlight(float normalizedSpeed)
    {
        int activeZone = Mathf.Clamp(Mathf.FloorToInt(normalizedSpeed * ZoneCount), 0, ZoneCount - 1);

        if (activeZone != currentActiveZone)
        {
            if (currentActiveZone >= 0)
            {
                zoneRenderers[currentActiveZone].material.color = baseColors[currentActiveZone];
            }

            currentActiveZone = activeZone;
            zoneRenderers[currentActiveZone].material.color = baseColors[currentActiveZone] * BrightnessMultiplier;
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < ZoneCount; i++)
        {
            zoneRenderers[i].material.color = baseColors[i];
        }

        if (speedArrow != null)
        {
            Destroy(speedArrow);
        }
    }
}
