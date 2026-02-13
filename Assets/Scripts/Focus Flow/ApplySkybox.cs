using UnityEngine;

public class ApplySkybox : MonoBehaviour
{
    [SerializeField] private Material skyboxMaterial;

    private void Awake()
    {
        if (skyboxMaterial != null)
        {
            RenderSettings.skybox = skyboxMaterial;
            DynamicGI.UpdateEnvironment();
        }
    }
}
