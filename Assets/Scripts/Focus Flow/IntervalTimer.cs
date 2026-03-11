using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class IntervalTimer : NetworkBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float intervalDuration = 45f;
    [SerializeField] private float restDuration = 15f;
    [SerializeField] private int totalCycles = 3;

    [Header("UI Reference")]

    [SerializeField] private Text timerText;
    [SerializeField] private TextMeshProUGUI startText;

    [Header("Phase Shift Animation")]
    [SerializeField] private float normalLabelSize = 110f;
    [SerializeField] private float shiftLabelSize = 180f;
    [SerializeField] private float shiftAnimationDuration = 1f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip intervalChangeSound;

    private float currentTime;
    private int completedCycles;
    private bool isIntervalPhase = true;
    private bool isRunning = false;
    private float currentLabelSize;
    private Coroutine phaseShiftCoroutine;

    public bool IsIntervalPhase => isIntervalPhase;

    private void Start()
    {
        currentTime = intervalDuration;
        completedCycles = 0;
        currentLabelSize = normalLabelSize;
        UpdateTimerDisplay();
        StartCoroutine(DisplayStartText());
    }

    private IEnumerator DisplayStartText()
    {
        yield return new WaitForSeconds(1f);
        startText.text = "";
        isRunning = true;
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
                if (IsHost) FocusFlowGameState.Instance.SetGameStateServerRpc(GameState.Stopped);
                timerText.gameObject.SetActive(false);
                StartCoroutine(Handover());
            }
            else
            {
                isIntervalPhase = true;
                currentTime = intervalDuration;
                TriggerPhaseShiftAnimation();
            }
        }
    }

    private void TriggerPhaseShiftAnimation()
    {
        if (phaseShiftCoroutine != null) StopCoroutine(phaseShiftCoroutine);
        audioSource?.PlayOneShot(intervalChangeSound);
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

    private IEnumerator Handover()
    {
        yield return new WaitForSeconds(8f);
        if(IsHost) FocusFlowGameState.Instance.SetGameStateServerRpc(GameState.Handover);
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
