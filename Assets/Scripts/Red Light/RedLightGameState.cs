using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class RedLightGameState : NetworkBehaviour
{
    public static RedLightGameState Instance { get; private set; }
    public List<PlayerData> PlayerDataList = new List<PlayerData>();
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
        gameState.OnValueChanged += OnGameStateChanged;
    }

    public override void OnNetworkDespawn()
    {
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

        foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            var player = obj.GetComponent<RedLightPlayerMovement>();
            if (!player) continue;
            PlayerData data = player.GetPlayerData();
            this.PlayerDataList.Add(data);
        }
        List<PlayerData> rankedPlayers = this.PlayerDataList.OrderByDescending(p => p.Distance).ToList();
        for (int i = 0; i < rankedPlayers.Count; i++)
        {
            float score = i < scores.Length ? scores[i] : 0f;
            FixedString64Bytes guid = rankedPlayers[i].Guid;
            FixedString64Bytes nickname = rankedPlayers[i].nickname;
            FixedString64Bytes color = rankedPlayers[i].color;
            
            SessionManager.PlayerData globalSessionData = SessionManager.Instance.GetDataByGuid(guid);
            SessionManager.PlayerData scoredPlayerData = new SessionManager.PlayerData(guid, nickname, color, score + globalSessionData.Score);
            SessionManager.Instance.SaveData(scoredPlayerData);
        }
    }

    [ServerRpc]
    public void SetGameStateServerRpc(GameState newState)
    {
        gameState.Value = newState;
    }
}

