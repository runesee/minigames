using Unity.Netcode;
using UnityEngine;
using TMPro;

public class GameTimer : NetworkBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float gameDurationInSeconds = 60f;
    [SerializeField] private TextMeshProUGUI timerText;

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
        if (timerText == null)
        {
            GameObject timerTextObj = GameObject.Find("TimerText");
            if (timerTextObj != null)
            {
                timerText = timerTextObj.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                Debug.LogWarning("GameTimer: TimerText GameObject not found!");
            }
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
        if (!IsServer)
        {
            return;
        }
        
        if (!timerRunning.Value)
        {
            return;
        }

        remainingTime.Value -= Time.deltaTime;

        if (remainingTime.Value <= 0f)
        {
            remainingTime.Value = 0f;
            timerRunning.Value = false;
            Debug.Log("GameTimer: Time's up! Stopping game.");
            StopGame();
        }
    }

    private void OnGameStateChanged(TagGameState.GameState previousState, TagGameState.GameState newState)
    {
        Debug.Log($"GameTimer: Game state changed from {previousState} to {newState}. IsServer: {IsServer}");
        
        if (!IsServer)
        {
            return;
        }

        if (newState == TagGameState.GameState.Running)
        {
            remainingTime.Value = gameDurationInSeconds;
            timerRunning.Value = true;
            Debug.Log($"GameTimer: Timer started! Duration: {gameDurationInSeconds}s, Running: {timerRunning.Value}");
        }
        else if (newState == TagGameState.GameState.Stopped || newState == TagGameState.GameState.Idling)
        {
            timerRunning.Value = false;
            Debug.Log($"GameTimer: Timer stopped. State: {newState}");
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
            Debug.LogError("GameTimer: Cannot update timer display - timerText is null!");
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
