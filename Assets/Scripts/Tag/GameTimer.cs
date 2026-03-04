using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections;

public class GameTimer : NetworkBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float gameDurationInSeconds = 60f;
    [SerializeField] private TextMeshProUGUI timerText;
    public bool isTag;

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

    private NetworkVariable<double> timerEndTime = new NetworkVariable<double>(
        0.0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            remainingTime.Value = gameDurationInSeconds;
        }

        remainingTime.OnValueChanged += OnRemainingTimeChanged;
        OnRemainingTimeChanged(0f, remainingTime.Value);
        StartCoroutine(WaitForTagGameState());
    }

    private System.Collections.IEnumerator WaitForTagGameState()
    {
        if (isTag)
        {
            while (TagGameState.Instance == null) yield return new WaitForSeconds(0.1f);
            TagGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
            if (IsServer) OnGameStateChanged(GameState.Initializing, TagGameState.Instance.gameState.Value);
        }
        else
        {
            while (BalloonTagGameState.Instance == null) yield return new WaitForSeconds(0.1f);
            BalloonTagGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
            if (IsServer) OnGameStateChanged(GameState.Initializing, BalloonTagGameState.Instance.gameState.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        remainingTime.OnValueChanged -= OnRemainingTimeChanged;
        if (TagGameState.Instance != null) TagGameState.Instance.gameState.OnValueChanged -= OnGameStateChanged;
        else if (BalloonTagGameState.Instance != null) BalloonTagGameState.Instance.gameState.OnValueChanged -= OnGameStateChanged;
    }

    private void Update()
    {
        if (!timerRunning.Value) return;

        double currentTime = IsServer ? NetworkManager.ServerTime.Time : NetworkManager.LocalTime.Time;
        float newRemainingTime = Mathf.Max(0f, (float)(timerEndTime.Value - currentTime));

        if (IsServer)
        {
            remainingTime.Value = newRemainingTime;

            if (remainingTime.Value <= 0f)
            {
                timerRunning.Value = false;
                StopGame();
            }
        }
        else
        {
            UpdateTimerDisplay(newRemainingTime);
        }
    }

    private void OnGameStateChanged(GameState previousState, GameState newState)
    {
        if (!IsServer) return;

        if (newState == GameState.Running)
        {
            timerEndTime.Value = NetworkManager.ServerTime.Time + gameDurationInSeconds;
            remainingTime.Value = gameDurationInSeconds;
            timerRunning.Value = true;
        }
        else if (newState == GameState.Stopped || newState == GameState.Idling)
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
            TagGameState.Instance.SetGameStateServerRpc(GameState.Stopped);
        }
        else if (BalloonTagGameState.Instance != null && IsServer)
        {
            BalloonTagGameState.Instance.SetGameStateServerRpc(GameState.Stopped);
        }
        StartCoroutine(Handover());
    }

    private IEnumerator Handover()
    {
        yield return new WaitForSeconds(8f);
        if(IsHost && isTag) TagGameState.Instance.SetGameStateServerRpc(GameState.Handover);
        else if (IsHost) BalloonTagGameState.Instance.SetGameStateServerRpc(GameState.Handover);
    }
}
