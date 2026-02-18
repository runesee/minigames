using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections;

public class RedLightGameTimer : NetworkBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float gameDurationInSeconds = 90f;
    [SerializeField] private float waitingDuration = 3f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI stateText;

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
        
        StartCoroutine(WaitForGameState());
    }

    private IEnumerator WaitForGameState()
    {
        while (RedLightGameState.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        RedLightGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;

        if (IsServer)
        {
            StartCoroutine(StartGameAfterDelay());
        }
    }

    private IEnumerator StartGameAfterDelay()
    {
        yield return new WaitForSeconds(waitingDuration);
        RedLightGameState.Instance.SetGameStateServerRpc(RedLightGameState.GameState.Running);
    }

    public override void OnNetworkDespawn()
    {
        remainingTime.OnValueChanged -= OnRemainingTimeChanged;

        if (RedLightGameState.Instance != null)
        {
            RedLightGameState.Instance.gameState.OnValueChanged -= OnGameStateChanged;
        }
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

    private void OnGameStateChanged(RedLightGameState.GameState previousState, RedLightGameState.GameState newState)
    {
        UpdateStateDisplay(newState);

        if (!IsServer) return;

        if (newState == RedLightGameState.GameState.Running)
        {
            timerEndTime.Value = NetworkManager.ServerTime.Time + gameDurationInSeconds;
            remainingTime.Value = gameDurationInSeconds;
            timerRunning.Value = true;

            if (RedLightManager.Instance != null)
            {
                RedLightManager.Instance.StartGame();
            }
        }
        else if (newState == RedLightGameState.GameState.Finished)
        {
            timerRunning.Value = false;

            if (RedLightManager.Instance != null)
            {
                RedLightManager.Instance.StopGame();
            }
        }
    }

    private void OnRemainingTimeChanged(float previousTime, float newTime)
    {
        UpdateTimerDisplay(newTime);
    }

    private void UpdateTimerDisplay(float timeInSeconds)
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void UpdateStateDisplay(RedLightGameState.GameState state)
    {
        if (stateText == null) return;

        switch (state)
        {
            case RedLightGameState.GameState.Waiting:
                stateText.text = "GET READY!";
                break;
            case RedLightGameState.GameState.Running:
                stateText.text = "";
                break;
            case RedLightGameState.GameState.Finished:
                stateText.text = "GAME FINISHED!";
                break;
        }
    }

    private void StopGame()
    {
        if (RedLightGameState.Instance != null && IsServer)
        {
            RedLightGameState.Instance.SetGameStateServerRpc(RedLightGameState.GameState.Finished);
        }

        StartCoroutine(TransitionToScoreboard());
    }

    private IEnumerator TransitionToScoreboard()
    {
        yield return new WaitForSeconds(5f);
        
        if (IsHost && MinigameManager.Instance != null)
        {
            MinigameManager.Instance.SceneFinished();
        }
    }
}
