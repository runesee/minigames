using UnityEngine;
using System.Collections.Generic;

public class ScoreMultiplierManager : MonoBehaviour
{
    [SerializeField] private IntervalTimer intervalTimer;
    [SerializeField] private SpeedIndicator speedIndicator;

    private readonly float[] intervalMultipliers = { 2.5f, 2.0f, 1.5f, 1.0f, 0.5f };
    private readonly float[] restMultipliers = { 0.5f, 1.0f, 1.5f, 2.0f, 0.5f };

    private List<int> zoneIndexSamples = new List<int>();
    private Queue<float> normalizedSpeedSamples = new Queue<float>();
    private bool isTracking = false;

    public bool IsTracking => isTracking;
    public bool IsIntervalPhase => intervalTimer != null && intervalTimer.IsIntervalPhase;

    private void Update()
    {
        if (isTracking)
        {
            int currentZone = speedIndicator.GetCurrentZoneIndex();
            zoneIndexSamples.Add(currentZone);
            
            float normalizedSpeed = Mathf.Clamp(PlayPulse.Input.Input.Speed, 0.0f, 1.0f);
            if (normalizedSpeedSamples.Count > 100) normalizedSpeedSamples.Dequeue();   // TODO : adjust count after trying on bike
            normalizedSpeedSamples.Enqueue(normalizedSpeed);
        }
    }

    public void StartTracking()
    {
        zoneIndexSamples.Clear();
        normalizedSpeedSamples.Clear();
        isTracking = true;
    }

    public float GetAverageNormalizedSpeed()
    {
        if (normalizedSpeedSamples.Count == 0)
        {
            return 0f;
        }

        float total = 0f;
        foreach (float speed in normalizedSpeedSamples)
        {
            total += speed;
        }

        return total / normalizedSpeedSamples.Count;
    }

    public float GetAverageMultiplier()
    {
        if (zoneIndexSamples.Count == 0)
        {
            return 1.0f;
        }

        bool isInterval = intervalTimer.IsIntervalPhase;
        float[] currentMultipliers = isInterval ? intervalMultipliers : restMultipliers;

        float totalMultiplier = 0f;

        foreach (int zoneIndex in zoneIndexSamples)
        {
            if (zoneIndex >= 0 && zoneIndex < currentMultipliers.Length)
            {
                totalMultiplier += currentMultipliers[zoneIndex];
            }
            else
            {
                totalMultiplier += 1.0f;
            }
        }

        return totalMultiplier / zoneIndexSamples.Count;
    }

    public int ApplyAverageMultiplier(float baseScore)
    {
        float multiplier = GetAverageMultiplier();
        return Mathf.RoundToInt(baseScore * multiplier);
    }

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
