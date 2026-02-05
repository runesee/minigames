using UnityEngine;
using UnityEngine.UI;

public class IntervalTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float intervalDuration = 45f;
    [SerializeField] private float restDuration = 15f;
    [SerializeField] private int totalCycles = 3;

    [Header("UI Reference")]
    [SerializeField] private Text timerText;

    private float currentTime;
    private int completedCycles;
    private bool isIntervalPhase = true;
    private bool isRunning = true;

    private void Start()
    {
        currentTime = intervalDuration;
        completedCycles = 0;
        UpdateTimerDisplay();
    }

    private void Update()
    {
        if (!isRunning) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            OnPhaseComplete();
        }

        UpdateTimerDisplay();
    }

    private void OnPhaseComplete()
    {
        if (isIntervalPhase)
        {
            isIntervalPhase = false;
            currentTime = restDuration;
        }
        else
        {
            completedCycles++;
            
            if (completedCycles >= totalCycles)
            {
                isRunning = false;
                currentTime = 0f;
            }
            else
            {
                isIntervalPhase = true;
                currentTime = intervalDuration;
            }
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        string phaseLabel = isIntervalPhase ? "INTERVAL" : "REST";
        int currentCycle = Mathf.Min(completedCycles + 1, totalCycles);
        string cycleInfo = $"Cycle {currentCycle}/{totalCycles}";

        timerText.text = $"{phaseLabel}\n{minutes:00}:{seconds:00}\n{cycleInfo}";
    }
}
