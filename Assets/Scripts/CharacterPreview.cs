using UnityEngine;

public class CharacterPreview : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer playerSkinRenderer;

    private void Awake()
    {
        if (playerSkinRenderer == null)
        {
            SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.name == "Beta_Surface")
                {
                    playerSkinRenderer = renderer;
                    break;
                }
            }
        }

        DisableNetworkComponents();
    }

    private void DisableNetworkComponents()
    {
        var networkComponents = GetComponentsInChildren<Unity.Netcode.NetworkBehaviour>(true);
        foreach (var component in networkComponents)
        {
            if (component != null)
            {
                Destroy(component);
            }
        }

        var networkObject = GetComponent<Unity.Netcode.NetworkObject>();
        if (networkObject != null)
        {
            Destroy(networkObject);
        }

        var rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }

        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    public void SetColor(Color color)
    {
        if (playerSkinRenderer != null)
        {
            playerSkinRenderer.material.color = color;
        }
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, 30f * Time.deltaTime);
    }
}
