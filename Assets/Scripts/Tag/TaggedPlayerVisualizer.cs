using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerTagMovement))]
public class TaggedPlayerVisualizer : NetworkBehaviour
{
    [Header("Glow Settings")]
    [SerializeField] private SkinnedMeshRenderer playerSkinRenderer;
    [SerializeField] private float glowIntensity;

    [Header("Marker Settings")]
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private Vector3 markerOffset = new Vector3(0f, 2.5f, 0f);

    public NetworkVariable<Color> glowColorNet = new NetworkVariable<Color>(
        Color.red,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private PlayerTagMovement playerMovement;
    private Material originalMaterial;
    private Material glowMaterial;
    private GameObject markerInstance;
    private Color originalEmissionColor;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerTagMovement>();
    }

    public override void OnNetworkSpawn()
    {
        if (playerSkinRenderer == null)
        {
            playerSkinRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        originalMaterial = playerSkinRenderer.material;
        glowMaterial = new Material(originalMaterial);
        glowMaterial.EnableKeyword("_EMISSION");

        if (glowMaterial.HasProperty("_EmissionColor"))
        {
            originalEmissionColor = glowMaterial.GetColor("_EmissionColor");
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
        
        if (IsOwner)
        {
            UnityEngine.ColorUtility.TryParseHtmlString(PlayerPrefs.GetString("Color"), out var skinColor);
            glowColorNet.Value = skinColor;
        }

        glowColorNet.OnValueChanged += OnGlowColorChanged;
    }

    private void OnGlowColorChanged(Color previousValue, Color newValue)
    {
        if (playerMovement != null)
        {
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
        if (playerSkinRenderer != null && glowMaterial != null)
        {
            if (isTagged)
            {
                Color emissionColor = glowColorNet.Value * glowIntensity;
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
            markerMaterial.color = glowColorNet.Value;
            markerMaterial.EnableKeyword("_EMISSION");
            markerMaterial.SetColor("_EmissionColor", glowColorNet.Value * 2f);
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
