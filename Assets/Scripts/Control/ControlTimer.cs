using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ControlTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float intervalDuration = 60f;
    [SerializeField] private float restDuration = 30f;
    [SerializeField] private int totalCycles = 5;

    [Header("UI Reference")]
    [SerializeField] private Text timerText;

    [Header("Phase Shift Animation")]
    [SerializeField] private float normalLabelSize = 110f;
    [SerializeField] private float shiftLabelSize = 180f;
    [SerializeField] private float shiftAnimationDuration = 1f;

    private float currentTime;
    private int completedCycles;
    private bool isIntervalPhase = true;
    private bool isRunning;
    private float currentLabelSize;
    private Coroutine phaseShiftCoroutine;

    public bool IsIntervalPhase => isIntervalPhase;
    public bool IsRunning => isRunning;
    public int CompletedCycles => completedCycles;

    private void Start()
    {
        currentTime = intervalDuration;
        completedCycles = 0;
        currentLabelSize = normalLabelSize;
        isRunning = true;
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
            TriggerPhaseShiftAnimation();
        }
        else
        {
            completedCycles++;

            if (completedCycles >= totalCycles)
            {
                isRunning = false;
                currentTime = 0f;
                timerText.text = $"<size={Mathf.RoundToInt(normalLabelSize)}>DONE</size>\n00:00\nAll cycles complete";
                return;
            }

            isIntervalPhase = true;
            currentTime = intervalDuration;
            TriggerPhaseShiftAnimation();
        }
    }

    private void TriggerPhaseShiftAnimation()
    {
        if (phaseShiftCoroutine != null) StopCoroutine(phaseShiftCoroutine);
        phaseShiftCoroutine = StartCoroutine(PhaseShiftAnimation());
    }

    private IEnumerator PhaseShiftAnimation()
    {
        currentLabelSize = shiftLabelSize;

        float elapsed = 0f;
        while (elapsed < shiftAnimationDuration)
        {
            elapsed += Time.deltaTime;
            currentLabelSize = Mathf.Lerp(shiftLabelSize, normalLabelSize, elapsed / shiftAnimationDuration);
            yield return null;
        }

        currentLabelSize = normalLabelSize;
        phaseShiftCoroutine = null;
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        string phaseLabel = isIntervalPhase ? "INTERVAL" : "REST";
        int currentCycle = Mathf.Min(completedCycles + 1, totalCycles);
        string cycleInfo = $"Cycle {currentCycle}/{totalCycles}";
        int labelSize = Mathf.RoundToInt(currentLabelSize);

        timerText.text = $"<size={labelSize}>{phaseLabel}</size>\n{minutes:00}:{seconds:00}\n{cycleInfo}";
    }
}
