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

    private const float LIT_DURATION = 1.0f;
    private float litTimer = 0f;
    private bool isLit = false;

    public ButtonType Type => buttonType;
    public bool IsLit => isLit;

    private void Awake()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        if (defaultMaterial == null && meshRenderer != null)
        {
            defaultMaterial = meshRenderer.sharedMaterial;
        }
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
    }

    public void LightUp()
    {
        isLit = true;
        litTimer = 0f;
        
        if (meshRenderer != null && litMaterial != null)
        {
            meshRenderer.material = litMaterial;
        }
    }

    public void TurnOff()
    {
        isLit = false;
        litTimer = 0f;
        
        if (meshRenderer != null && defaultMaterial != null)
        {
            meshRenderer.material = defaultMaterial;
        }
    }
}
