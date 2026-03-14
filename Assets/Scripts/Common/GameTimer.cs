using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameTimer : NetworkBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float gameDurationInSeconds = 60f;
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

    public double GameEndServerTime => timerEndTime.Value;

    private NetworkVariable<double> timerEndTime = new NetworkVariable<double>(
        0.0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private int overtimeCount = 0;

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
        if (SceneManager.GetActiveScene().name == "TagScene")
        {
            while (TagGameState.Instance == null) yield return new WaitForSeconds(0.1f);
            TagGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
            if (IsServer) OnGameStateChanged(GameState.Initializing, TagGameState.Instance.gameState.Value);
        }
        else if (SceneManager.GetActiveScene().name == "BalloonTag")
        {
            while (BalloonTagGameState.Instance == null) yield return new WaitForSeconds(0.1f);
            BalloonTagGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
            if (IsServer) OnGameStateChanged(GameState.Initializing, BalloonTagGameState.Instance.gameState.Value);
        }
        else if (SceneManager.GetActiveScene().name == "CaptureTheFlag")
        {
            while (CtFGameState.Instance == null) yield return new WaitForSeconds(0.1f);
            CtFGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
            if (IsServer) OnGameStateChanged(GameState.Initializing, CtFGameState.Instance.gameState.Value);
        }
        else if (SceneManager.GetActiveScene().name == "ColorFlood")
        {
            while (ColorFloodGameState.Instance == null) yield return new WaitForSeconds(0.1f);
            ColorFloodGameState.Instance.gameState.OnValueChanged += OnGameStateChanged;
            if (IsServer) OnGameStateChanged(GameState.Initializing, ColorFloodGameState.Instance.gameState.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        remainingTime.OnValueChanged -= OnRemainingTimeChanged;
        if (TagGameState.Instance != null) TagGameState.Instance.gameState.OnValueChanged -= OnGameStateChanged;
        else if (BalloonTagGameState.Instance != null) BalloonTagGameState.Instance.gameState.OnValueChanged -= OnGameStateChanged;
        else if (CtFGameState.Instance != null) CtFGameState.Instance.gameState.OnValueChanged -= OnGameStateChanged;
        else if (ColorFloodGameState.Instance != null) ColorFloodGameState.Instance.gameState.OnValueChanged -= OnGameStateChanged;
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
        ToggleStartTextClientRpc(newState);

        if (newState == GameState.Running)
        {
            if (CtFGameState.Instance != null)
            {
                CTFToggleStartTextClientRpc();
                return;
            }
            timerEndTime.Value = NetworkManager.ServerTime.Time + gameDurationInSeconds;
            remainingTime.Value = gameDurationInSeconds;
            timerRunning.Value = true;
        }
        else if (newState == GameState.Stopped || newState == GameState.Idling)
        {
            timerRunning.Value = false;
        }
    }

    private IEnumerator DisplayStartText()
    {
        stateText.text = "GET READY!"; 
        yield return new WaitForSeconds(3f);
        timerEndTime.Value = NetworkManager.ServerTime.Time + gameDurationInSeconds;
        stateText.text = "";
        remainingTime.Value = gameDurationInSeconds;
        timerRunning.Value = true;
    }

    [ClientRpc]
    private void CTFToggleStartTextClientRpc()
    {
        StartCoroutine(DisplayStartText());
    }

    [ClientRpc]
    private void ToggleStartTextClientRpc(GameState state)
    {
        if (CtFGameState.Instance != null) return;
        if (state == GameState.Idling) stateText.text = "GET READY!"; 
        else stateText.text = "";
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
        else if (CtFGameState.Instance != null && IsServer)
        {
            if (CtFGameState.Instance.blueScore.Value == CtFGameState.Instance.greenScore.Value && overtimeCount < 2)
            {
                overtimeCount++;
                if (overtimeCount >= 2)
                {
                    CtFGameState.Instance.ToastMessageClientRpc(Team.None, "2nd Overtime! Last chance to avoid a tie!");
                    float[] scores = { 5f, 5f, 5f, 5f };
                    CtFGameState.Instance.SetScores(scores);
                    OvertimeServerRpc(31f);
                }
                else
                {
                    CtFGameState.Instance.ToastMessageClientRpc(Team.None, "Overtime! 45 seconds added to the clock!");
                    OvertimeServerRpc(46f);
                }
                return;
            }
            else CtFGameState.Instance.SetGameStateServerRpc(GameState.Stopped);
        }
        else if (ColorFloodGameState.Instance != null && IsServer)
        {
            ColorFloodGameState.Instance.SetGameStateServerRpc(GameState.Stopped);
        }
        StartCoroutine(Handover());
    }

    [ServerRpc]
    private void OvertimeServerRpc(float overTime)
    {
        remainingTime.Value = overTime;
        timerRunning.Value = true;
        timerEndTime.Value = NetworkManager.ServerTime.Time + overTime;
    }

    private IEnumerator Handover()
    {
        yield return new WaitForSeconds(8f);
        if (IsHost && SceneManager.GetActiveScene().name == "TagScene") TagGameState.Instance.SetGameStateServerRpc(GameState.Handover);
        else if (IsHost && SceneManager.GetActiveScene().name == "BalloonTag") BalloonTagGameState.Instance.SetGameStateServerRpc(GameState.Handover);
        else if (IsHost && SceneManager.GetActiveScene().name == "CaptureTheFlag") CtFGameState.Instance.SetGameStateServerRpc(GameState.Handover);
        else if (IsHost && SceneManager.GetActiveScene().name == "ColorFlood") ColorFloodGameState.Instance.SetGameStateServerRpc(GameState.Handover);
    }
}
