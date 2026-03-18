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

    [Header("Warmup Settings")]
    [SerializeField] private float warmupDuration = 480f;

    [Header("UI Reference")]
    [SerializeField] private Text timerText;

    [Header("Phase Shift Animation")]
    [SerializeField] private float normalLabelSize = 110f;
    [SerializeField] private float shiftLabelSize = 180f;
    [SerializeField] private float shiftAnimationDuration = 1f;

    private float currentTime;
    private int completedCycles;

    private bool isIntervalPhase = true;
    private bool isWarmupPhase = true;
    private bool isRunning;
    private bool hasStarted = false;

    private float currentLabelSize;
    private Coroutine phaseShiftCoroutine;

    public bool IsIntervalPhase => isIntervalPhase;
    public bool IsWarmupPhase => isWarmupPhase;
    public bool IsRunning => isRunning;
    public int CompletedCycles => completedCycles;

    public float TotalSessionDuration => totalCycles * (intervalDuration + restDuration);
    public float ElapsedSessionTime { get; private set; }

    public event Action<bool> OnPhaseChanged;
    public event Action OnSessionComplete;

    private void Start()
    {
        currentTime = warmupDuration;
        completedCycles = 0;
        currentLabelSize = normalLabelSize;
        ElapsedSessionTime = 0f;

        isRunning = false;
        isWarmupPhase = true;
        isIntervalPhase = true;
        hasStarted = false;

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
        if (!hasStarted)
        {
            if (Input.GetKeyDown(KeyCode.Return) ||
                PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.A))
            {
                hasStarted = true;
                isRunning = true;
            }

            return;
        }

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
        if (phaseShiftCoroutine != null)
            StopCoroutine(phaseShiftCoroutine);

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
        if (!hasStarted)
        {
            int labelSize = Mathf.RoundToInt(currentLabelSize);
            timerText.text = $"<size={labelSize}>READY</size>\nPress A to start";
            return;
        }

        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        string phaseLabel;

        if (isWarmupPhase)
            phaseLabel = "WARM-UP";
        else
            phaseLabel = isIntervalPhase ? "INTERVAL" : "REST";

        int currentCycle = Mathf.Min(completedCycles + 1, totalCycles);
        string cycleInfo = isWarmupPhase ? "" : $"Cycle {currentCycle}/{totalCycles}";
        int labelSizeFinal = Mathf.RoundToInt(currentLabelSize);

        timerText.text =
            $"<size={labelSizeFinal}>{phaseLabel}</size>\n" +
            $"{minutes:00}:{seconds:00}\n" +
            $"{cycleInfo}";
    }
}