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
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        Vector3 directionToCamera = mainCamera.transform.position - transform.position;
        directionToCamera.y = 0f;

        if (directionToCamera.sqrMagnitude > 0f)
            transform.rotation = Quaternion.LookRotation(directionToCamera);
    }
}
