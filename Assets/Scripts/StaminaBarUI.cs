using UnityEngine;
using UnityEngine.UI;

public class StaminaBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Gradient colorGradient;
    
    private void Awake()
    {
        if (colorGradient == null)
        {
            colorGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(Color.red, 0f);
            colorKeys[1] = new GradientColorKey(Color.green, 1f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            colorGradient.SetKeys(colorKeys, alphaKeys);
        }
    }
    
    public void UpdateStamina(float staminaPercent)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(staminaPercent);
            fillImage.color = colorGradient.Evaluate(staminaPercent);
        }
    }
}
