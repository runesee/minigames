using Unity.Netcode;
using UnityEngine;
using TMPro;

public class GameTimer : NetworkBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float gameDurationInSeconds = 60f;

    private TextMeshProUGUI timerText;

    private NetworkVariable<float> remainingTime = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> timerRunning = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Start()
    {
        GameObject timerTextObj = GameObject.Find("TimerText");
        if (timerTextObj != null)
        {
            timerText = timerTextObj.GetComponent<TextMeshProUGUI>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            remainingTime.Value = gameDurationInSeconds;
        }

        remainingTime.OnValueChanged += OnRemainingTimeChanged;
        OnRemainingTimeChanged(0f, remainingTime.Value);

        if (TagGameState.Instance != null)
        {
            TagGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
            if (IsServer)
            {
                OnGameStateChanged(TagGameState.GameState.Initializing, TagGameState.Instance.gameState.Value);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        remainingTime.OnValueChanged -= OnRemainingTimeChanged;

        if (TagGameState.Instance != null)
        {
            TagGameState.Instance.gameState.OnValueChanged -= OnGameStateChanged;
        }
    }

    private void Update()
    {
        if (!IsServer || !timerRunning.Value)
        {
            return;
        }

        remainingTime.Value -= Time.deltaTime;

        if (remainingTime.Value <= 0f)
        {
            remainingTime.Value = 0f;
            timerRunning.Value = false;
            StopGame();
        }
    }

    private void OnGameStateChanged(TagGameState.GameState previousState, TagGameState.GameState newState)
    {
        if (!IsServer)
        {
            return;
        }

        if (newState == TagGameState.GameState.Running)
        {
            remainingTime.Value = gameDurationInSeconds;
            timerRunning.Value = true;
        }
        else if (newState == TagGameState.GameState.Stopped || newState == TagGameState.GameState.Idling)
        {
            timerRunning.Value = false;
        }
    }

    private void OnRemainingTimeChanged(float previousTime, float newTime)
    {
        UpdateTimerDisplay(newTime);
    }

    private void UpdateTimerDisplay(float timeInSeconds)
    {
        if (timerText == null)
        {
            return;
        }

        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void StopGame()
    {
        if (TagGameState.Instance != null && IsServer)
        {
            TagGameState.Instance.SetGameStateServerRpc(TagGameState.GameState.Stopped);
        }
    }
}
