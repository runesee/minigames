using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class TagGameState : NetworkBehaviour
{
    public static TagGameState Instance { get; private set; }

    public NetworkVariable<ulong> taggedPlayerIdNet = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(
        GameState.Initializing,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private readonly float[] scores = { 12f, 10f, 6f, 3f };

    public enum GameState
    {
        Initializing,
        Idling,
        Running,
        Stopped,
    }

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        SetGameStateServerRpc(GameState.Idling);
        TagSessionManager.Instance.PlayerDataList.OnListChanged += OnPlayerDataListChanged;
    }

    private void OnPlayerDataListChanged(NetworkListEvent<TagSessionManager.PlayerData> changeEvent)
    {
        Debug.Log("TGS playerlistchanged");
        if ((TagSessionManager.Instance.PlayerDataList.Count >= NetworkManager.ConnectedClients.Count) && gameState.Value == GameState.Stopped)
        {
            Debug.Log("TGS playerlistchanged within");
            // All clients should have written their data now.
            // Compute the winner based on the data.
            var playerList = new List<TagSessionManager.PlayerData>();
            for (int i = 0; i < TagSessionManager.Instance.PlayerDataList.Count; i++)
            {
                playerList.Add(TagSessionManager.Instance.PlayerDataList[i]);
            }
            var rankedPlayers = playerList.OrderBy(p => p.TimeSpentTagged).ToList();
            for (int i = 0; i < rankedPlayers.Count; i++)
            {
                float score = i < scores.Length ? scores[i] : 0f;
                FixedString64Bytes guid = rankedPlayers[i].Guid;
                SessionManager.PlayerData globalSessionData = SessionManager.Instance.GetDataByGuid(guid);
                float totalScore = score + globalSessionData.Score;
                SessionManager.PlayerData scoredPlayerData = new SessionManager.PlayerData(guid, totalScore);
                Debug.Log(scoredPlayerData);
                SessionManager.Instance.SaveDataServerRpc(scoredPlayerData);
            }
            MinigameManager.Instance.GameFinished();
        }
    }

    /// <summary>
    /// Update the current GameState (Initializing, Idling, Running or Stopped).
    /// </summary>
    /// <param name="state">New GameState.</param>
    [ServerRpc]
    public void SetGameStateServerRpc(GameState state)
    {
        gameState.Value = state;
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="playerData">Tag-specific data to save on disconnect.</param>
    [ServerRpc]
    public void SaveSessionDataServerRpc(TagSessionManager.PlayerData playerData)
    {
        Debug.Log("Saving data TGS ServerRPC");
        TagSessionManager.Instance.SaveDataServerRpc(playerData);
    }
}