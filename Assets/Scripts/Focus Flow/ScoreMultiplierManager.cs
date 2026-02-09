using UnityEngine;

public class ScoreMultiplierManager : MonoBehaviour
{
    [SerializeField] private IntervalTimer intervalTimer;
    [SerializeField] private SpeedIndicator speedIndicator;

    private readonly float[] intervalMultipliers = { 2.5f, 2.0f, 1.5f, 1.0f, 0.5f };
    private readonly float[] restMultipliers = { 0.5f, 1.0f, 1.5f, 2.0f, 0.5f };

    public float GetCurrentMultiplier()
    {
        int zoneIndex = speedIndicator.GetCurrentZoneIndex();
        bool isInterval = intervalTimer.IsIntervalPhase;

        float[] currentMultipliers = isInterval ? intervalMultipliers : restMultipliers;

        if (zoneIndex >= 0 && zoneIndex < currentMultipliers.Length)
        {
            return currentMultipliers[zoneIndex];
        }

        return 1.0f;
    }

    public int ApplyMultiplier(float baseScore)
    {
        float multiplier = GetCurrentMultiplier();
        return Mathf.RoundToInt(baseScore * multiplier);
    }
}
