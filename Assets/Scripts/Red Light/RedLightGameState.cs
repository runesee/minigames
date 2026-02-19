using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class RedLightGameState : NetworkBehaviour
{
    public static RedLightGameState Instance { get; private set; }

    public NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(
        GameState.Initializing,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private float[] scores = { 10, 6, 3, 1 };

    public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
    {
        public FixedString64Bytes Guid;
        public FixedString64Bytes nickname;
        public FixedString64Bytes color;
        public float Distance;

        public PlayerData(FixedString64Bytes Guid, FixedString64Bytes nickname, FixedString64Bytes color, float Distance)
        {
            this.Guid = Guid;
            this.nickname = nickname;
            this.color = color;
            this.Distance = Distance;
        }

        public bool Equals(PlayerData other)
        {
            return 
            (
                Guid.Equals(other.Guid) && 
                nickname.Equals(other.nickname) &&
                color.Equals(other.color) &&
                Distance.Equals(other.Distance)
            );    
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Guid);
            serializer.SerializeValue(ref nickname);
            serializer.SerializeValue(ref color);
            serializer.SerializeValue(ref Distance);
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        gameState.OnValueChanged += OnGameStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        gameState.OnValueChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState previousState, GameState newState)
    {
        if (newState == GameState.Handover)
        {
            SaveScoresToSessionManager();
            MinigameManager.Instance.SceneFinished();
        }
    }

    private void SaveScoresToSessionManager()
    {
        if (!IsServer) return;

        List<PlayerData> rankedPlayers = GetPlayerDataSortedByDistance();

        for (int i = 0; i < rankedPlayers.Count; i++)
        {
            float score = i < scores.Length ? scores[i] : 0f;
            FixedString64Bytes guid = rankedPlayers[i].Guid;
            FixedString64Bytes nickname = rankedPlayers[i].nickname;
            FixedString64Bytes color = rankedPlayers[i].color;
            
            SessionManager.PlayerData globalSessionData = SessionManager.Instance.GetDataByGuid(guid);
            SessionManager.PlayerData scoredPlayerData = new SessionManager.PlayerData(
                guid, 
                nickname, 
                color, 
                score + globalSessionData.Score
            );
            SessionManager.Instance.SaveData(scoredPlayerData);
        }

        // Fill remaining slots if less than 4 players
        if (rankedPlayers.Count < 4)
        {
            for (int i = rankedPlayers.Count; i < 4; i++)
            {
                Color playerColor = PlayerColorManager.AvailableColors[i];
                string colorHex = $"#{ColorUtility.ToHtmlStringRGB(playerColor)}";
                
                SessionManager.Instance.SaveData(
                    new SessionManager.PlayerData(
                        new FixedString64Bytes(Guid.NewGuid().ToString()),
                        $"Player {i+1}",
                        new FixedString64Bytes(colorHex),
                        0f
                    )
                );
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetGameStateServerRpc(GameState newState)
    {
        gameState.Value = newState;
    }

    public List<PlayerData> GetAllPlayerData()
    {
        List<PlayerData> playerDataList = new List<PlayerData>();

        RedLightPlayerMovement[] allPlayers = FindObjectsByType<RedLightPlayerMovement>(FindObjectsSortMode.None);
        
        foreach (RedLightPlayerMovement player in allPlayers)
        {
            if (player != null)
            {
                playerDataList.Add(player.GetPlayerData());
            }
        }

        return playerDataList;
    }

    public List<PlayerData> GetPlayerDataSortedByDistance()
    {
        List<PlayerData> playerDataList = GetAllPlayerData();
        return playerDataList.OrderByDescending(p => p.Distance).ToList();
    }
}

