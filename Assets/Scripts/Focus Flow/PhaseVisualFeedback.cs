using UnityEngine;

public class PhaseVisualFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private IntervalTimer intervalTimer;
    [SerializeField] private MeshRenderer mainCircleRenderer;
    
    [Header("Interval Phase Colors")]
    [SerializeField] private Color intervalColor = new Color(1f, 0.5f, 0.2f);
    
    [Header("Rest Phase Colors")]
    [SerializeField] private Color restColor = new Color(0.2f, 0.6f, 1f);
    
    private bool previousIntervalPhase;
    private Material mainCircleMaterial;
    private Color originalMainCircleColor;

    private void Start()
    {
        if (mainCircleRenderer != null)
        {
            mainCircleMaterial = mainCircleRenderer.material;
            originalMainCircleColor = mainCircleMaterial.color;
        }
        
        previousIntervalPhase = intervalTimer.IsIntervalPhase;
        UpdateVisuals();
    }

    private void Update()
    {
        if (intervalTimer.IsIntervalPhase != previousIntervalPhase)
        {
            previousIntervalPhase = intervalTimer.IsIntervalPhase;
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (mainCircleMaterial != null)
        {
            Color targetColor = intervalTimer.IsIntervalPhase ? intervalColor : restColor;
            mainCircleMaterial.color = targetColor;
        }
    }

    private void OnDestroy()
    {
        if (mainCircleMaterial != null)
        {
            mainCircleMaterial.color = originalMainCircleColor;
        }
    }
}
