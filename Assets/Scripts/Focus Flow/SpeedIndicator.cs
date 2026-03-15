using UnityEngine;

public class SpeedIndicator : MonoBehaviour
{
    [SerializeField] private ScoreMultiplierManager multiplierManager;
    [SerializeField] private SpeedometerDisplay speedometerDisplay;

    private const int ZoneCount = 5;
    private const float IntervalIdealSpeed = 1.0f;

    private static readonly Color[] RestZoneColors = new Color[]
    {
        new Color(0.90f, 0.10f, 0.10f),
        new Color(0.00f, 0.45f, 0.10f),
        new Color(0.00f, 0.85f, 0.20f),
        new Color(1.00f, 0.90f, 0.00f),
        new Color(1.00f, 0.50f, 0.00f),
    };

    private int currentActiveZone = -1;
    private bool lastKnownIntervalPhase;
    private bool phaseInitialized = false;

    public int GetCurrentZoneIndex()
    {
        return ZoneCount - 1 - currentActiveZone;
    }

    private void Start()
    {
        UpdatePhaseColors();
    }

    private void Update()
    {
        float normalizedSpeed = ReadNormalizedSpeed();
        speedometerDisplay?.SetNormalizedSpeed(normalizedSpeed);
        UpdateActiveZone(normalizedSpeed);
        UpdatePhaseColors();
    }

    private float ReadNormalizedSpeed()
    {
        if (multiplierManager != null && multiplierManager.IsTracking) return multiplierManager.GetAverageNormalizedSpeed();
        return Mathf.Clamp(PlayPulse.Input.Input.Speed, 0f, 1f);
    }

    private void UpdateActiveZone(float normalizedSpeed)
    {
        int activeZone = Mathf.Clamp(Mathf.FloorToInt(normalizedSpeed * ZoneCount), 0, ZoneCount - 1);
        if (activeZone == currentActiveZone) return;
        currentActiveZone = activeZone;
    }

    private void UpdatePhaseColors()
    {
        if (multiplierManager == null || speedometerDisplay == null) return;

        bool isInterval = multiplierManager.IsIntervalPhase;

        if (phaseInitialized && isInterval == lastKnownIntervalPhase) return;

        lastKnownIntervalPhase = isInterval;
        phaseInitialized = true;

        if (isInterval) speedometerDisplay.UpdateZoneColors(IntervalIdealSpeed);
        else speedometerDisplay.SetZoneColors(RestZoneColors);
    }
}
