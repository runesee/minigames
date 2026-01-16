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

        Debug.Log($"GameTimer: OnNetworkSpawn called. IsServer: {IsServer}, IsClient: {IsClient}");

        if (IsServer)
        {
            remainingTime.Value = gameDurationInSeconds;
            Debug.Log($"GameTimer: Server initialized remaining time to {remainingTime.Value}s");
        }

        remainingTime.OnValueChanged += OnRemainingTimeChanged;
        OnRemainingTimeChanged(0f, remainingTime.Value);

        StartCoroutine(WaitForTagGameState());
    }

    private System.Collections.IEnumerator WaitForTagGameState()
    {
        while (TagGameState.Instance == null)
        {
            Debug.Log("GameTimer: Waiting for TagGameState.Instance...");
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log($"GameTimer: TagGameState found! Current state: {TagGameState.Instance.gameState.Value}");
        TagGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
        
        if (IsServer)
        {
            OnGameStateChanged(TagGameState.GameState.Initializing, TagGameState.Instance.gameState.Value);
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

    private float debugLogTimer = 0f;
    
    private void Update()
    {
        debugLogTimer += Time.deltaTime;
        if (debugLogTimer >= 2f)
        {
            Debug.Log($"GameTimer Update: IsServer={IsServer}, timerRunning={timerRunning.Value}, remainingTime={remainingTime.Value:F1}");
            debugLogTimer = 0f;
        }
        
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
