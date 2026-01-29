using UnityEngine;

public class CharacterPreview : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer playerSkinRenderer;

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
