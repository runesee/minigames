using UnityEngine;
using TMPro;

public class WarmupScoreCalculator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WarmupSpeedSlope slope;
    [SerializeField] private TextMeshProUGUI scoreLabel;

    [Header("Scoring")]
    [SerializeField] private float basePointsPerSecond = 10f;

    private float score;

    private void Update()
    {
        if (!slope.TryGetCurrentTarget(out float minSpeed, out float maxSpeed, out float multiplier)) return;

        float playerSpeed = PlayPulse.Input.Input.Speed;
        if (playerSpeed >= minSpeed && playerSpeed <= maxSpeed)
        {
            score += basePointsPerSecond * multiplier * Time.deltaTime;
            UpdateLabel();
        }
    }

    private void UpdateLabel()
    {
        scoreLabel.text = $"Score\n{Mathf.FloorToInt(score)}";
    }
}
