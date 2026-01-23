using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerTagMovement))]
public class TaggedPlayerVisualizer : NetworkBehaviour
{
    [Header("Glow Settings")]
    [SerializeField] private SkinnedMeshRenderer playerSkinRenderer;
    [SerializeField] private Color glowColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float glowIntensity = 2f;

    [Header("Marker Settings")]
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private Vector3 markerOffset = new Vector3(0f, 2.5f, 0f);

    private PlayerTagMovement playerMovement;
    private Material originalMaterial;
    private Material glowMaterial;
    private GameObject markerInstance;
    private Color originalEmissionColor;
    private bool wasTagged = false;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerTagMovement>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (playerSkinRenderer == null)
        {
            playerSkinRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        if (playerSkinRenderer != null)
        {
            originalMaterial = playerSkinRenderer.material;
            glowMaterial = new Material(originalMaterial);
            glowMaterial.EnableKeyword("_EMISSION");

            if (glowMaterial.HasProperty("_EmissionColor"))
            {
                originalEmissionColor = glowMaterial.GetColor("_EmissionColor");
            }
        }

        if (markerPrefab != null)
        {
            markerInstance = Instantiate(markerPrefab, transform);
            markerInstance.transform.localPosition = markerOffset;
            markerInstance.SetActive(false);
        }
        else
        {
            CreateDefaultMarker();
        }

        if (playerMovement != null)
        {
            playerMovement.isTaggedNet.OnValueChanged += OnTaggedStateChanged;
            UpdateVisualization(playerMovement.isTaggedNet.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (playerMovement != null)
        {
            playerMovement.isTaggedNet.OnValueChanged -= OnTaggedStateChanged;
        }

        if (glowMaterial != null && glowMaterial != originalMaterial)
        {
            Destroy(glowMaterial);
        }
    }

    private void OnTaggedStateChanged(bool previousValue, bool newValue)
    {
        UpdateVisualization(newValue);
    }

    private void UpdateVisualization(bool isTagged)
    {
        wasTagged = isTagged;

        if (playerSkinRenderer != null && glowMaterial != null)
        {
            if (isTagged)
            {
                Color emissionColor = glowColor * glowIntensity;
                glowMaterial.SetColor("_EmissionColor", emissionColor);
                playerSkinRenderer.material = glowMaterial;
            }
            else
            {
                glowMaterial.SetColor("_EmissionColor", originalEmissionColor);
                playerSkinRenderer.material = originalMaterial;
            }
        }

        if (markerInstance != null)
        {
            markerInstance.SetActive(isTagged);
        }
    }

    private void CreateDefaultMarker()
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "TaggedMarker";
        marker.transform.SetParent(transform);
        marker.transform.localPosition = markerOffset;
        marker.transform.localScale = Vector3.one * 0.5f;

        Destroy(marker.GetComponent<Collider>());

        MeshRenderer renderer = marker.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material markerMaterial = new Material(Shader.Find("Standard"));
            markerMaterial.color = glowColor;
            markerMaterial.EnableKeyword("_EMISSION");
            markerMaterial.SetColor("_EmissionColor", glowColor * 2f);
            renderer.material = markerMaterial;
        }

        markerInstance = marker;
        markerInstance.SetActive(false);
    }

    private void LateUpdate()
    {
        if (markerInstance != null && markerInstance.activeSelf)
        {
            markerInstance.transform.Rotate(Vector3.up, 90f * Time.deltaTime);
        }
    }
}
