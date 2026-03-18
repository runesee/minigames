using System;
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

    [Header("Warmup Settings")]
    [SerializeField] private float warmupDuration = 480f;
    private bool isWarmupPhase = true;

    private float currentTime;
    private int completedCycles;
    private bool isIntervalPhase = true;
    private bool isRunning;
    private float currentLabelSize;
    private Coroutine phaseShiftCoroutine;

    public bool IsIntervalPhase => isIntervalPhase;
    public bool IsRunning => isRunning;
    public int CompletedCycles => completedCycles;
    public float TotalSessionDuration => totalCycles * (intervalDuration + restDuration);
    public float ElapsedSessionTime { get; private set; }
    public bool IsWarmupPhase => isWarmupPhase;

    public event Action<bool> OnPhaseChanged;
    public event Action OnSessionComplete;

    private void Start()
    {
        currentTime = warmupDuration;
        completedCycles = 0;
        currentLabelSize = normalLabelSize;
        ElapsedSessionTime = 0f;
        isRunning = true;
        isWarmupPhase = true;

        UpdateTimerDisplay();

        PlayPulse.PlayPulseService.Initialize(
            string.Empty,
            connectToBikeService: true,
            appSocketPathOverride: "127.0.0.1:13337",
            shellSocketPathOverride: "127.0.0.1:13337",
            useTcpSocket: true
        );
    }

    private void Update()
    {
        if (!isRunning) return;

        currentTime -= Time.deltaTime;
        ElapsedSessionTime += Time.deltaTime;

        if (currentTime <= 0f)
        {
            OnPhaseComplete();
        }

        UpdateTimerDisplay();
    }

    private void OnPhaseComplete()
    {
        if (isWarmupPhase)
        {
            isWarmupPhase = false;
            isIntervalPhase = true;
            currentTime = intervalDuration;

            TriggerPhaseShiftAnimation();
            OnPhaseChanged?.Invoke(isIntervalPhase);
            return;
        }

        if (isIntervalPhase)
        {
            isIntervalPhase = false;
            currentTime = restDuration;
            TriggerPhaseShiftAnimation();
            OnPhaseChanged?.Invoke(isIntervalPhase);
        }
        else
        {
            completedCycles++;

            if (completedCycles >= totalCycles)
            {
                isRunning = false;
                currentTime = 0f;
                timerText.text = $"<size={Mathf.RoundToInt(normalLabelSize)}>DONE</size>\n00:00\nAll cycles complete";
                OnSessionComplete?.Invoke();
                return;
            }

            isIntervalPhase = true;
            currentTime = intervalDuration;
            TriggerPhaseShiftAnimation();
            OnPhaseChanged?.Invoke(isIntervalPhase);
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

        string phaseLabel;

        if (isWarmupPhase)
            phaseLabel = "WARM-UP";
        else
            phaseLabel = isIntervalPhase ? "INTERVAL" : "REST";

        int currentCycle = Mathf.Min(completedCycles + 1, totalCycles);
        string cycleInfo = isWarmupPhase ? "" : $"Cycle {currentCycle}/{totalCycles}";
        int labelSize = Mathf.RoundToInt(currentLabelSize);

        timerText.text = $"<size={labelSize}>{phaseLabel}</size>\n{minutes:00}:{seconds:00}\n{cycleInfo}";
    }
}
