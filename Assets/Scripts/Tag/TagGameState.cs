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
    private bool shouldFinalize = false;
    private List<SessionManager.PlayerData> scoredPlayers = new List<SessionManager.PlayerData>();
    public List<PlayerData> PlayerDataList = new List<PlayerData>();

    public enum GameState
    {
        Initializing,
        Idling,
        Running,
        Stopped,
    }

    public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
    {
        public FixedString64Bytes Guid;
        public float XPos;
        public float ZPos;
        public double TimeSpentTagged;
        public double LastTagTime;
        public bool IsTagged;

        public PlayerData(FixedString64Bytes Guid, float XPos, float ZPos, double TimeSpentTagged, double LastTagTime, bool IsTagged)
        {
            this.Guid = Guid;
            this.XPos = XPos;
            this.ZPos = ZPos;
            this.TimeSpentTagged = TimeSpentTagged;
            this.LastTagTime = LastTagTime;
            this.IsTagged = IsTagged;
        }

        public PlayerData(FixedString64Bytes Guid)
        {
            this.Guid = Guid;
            this.XPos = 0f;
            this.ZPos = 0f;
            this.TimeSpentTagged = 0d;
            this.LastTagTime = 0d;
            this.IsTagged = false;
        }

        public bool Equals(PlayerData other)
        {
            return 
            (
                Guid.Equals(other.Guid) && 
                XPos.Equals(other.XPos) &&
                ZPos.Equals(other.ZPos) &&
                TimeSpentTagged.Equals(other.TimeSpentTagged) &&
                LastTagTime.Equals(other.LastTagTime) && 
                IsTagged.Equals(other.IsTagged)
            );    
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Guid);
            serializer.SerializeValue(ref XPos);
            serializer.SerializeValue(ref ZPos);
            serializer.SerializeValue(ref TimeSpentTagged);
            serializer.SerializeValue(ref LastTagTime);
            serializer.SerializeValue(ref IsTagged);
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!IsHost || !shouldFinalize) return;
        shouldFinalize = false;
        MinigameManager.Instance.GameFinished();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        SetGameStateServerRpc(GameState.Idling);
    }

    /// <summary>
    /// Update the current GameState (Initializing, Idling, Running or Stopped).
    /// </summary>
    /// <param name="state">New GameState.</param>
    [ServerRpc]
    public void SetGameStateServerRpc(GameState state)
    {
        gameState.Value = state;
        if (state == GameState.Stopped)
        {
            foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
            {
                var player = obj.GetComponent<PlayerTagMovement>();
                if (!player) continue;
                PlayerData data = player.GetTagData();
                this.PlayerDataList.Add(data);
            }
            var rankedPlayers = PlayerDataList.OrderBy(p => p.TimeSpentTagged).ToList();
            for (int i = 0; i < rankedPlayers.Count; i++)
            {
                float score = i < scores.Length ? scores[i] : 0f;
                FixedString64Bytes guid = rankedPlayers[i].Guid;
                SessionManager.PlayerData globalSessionData = SessionManager.Instance.GetDataByGuid(guid);
                float totalScore = score + globalSessionData.Score;
                SessionManager.PlayerData scoredPlayerData = new SessionManager.PlayerData(guid, totalScore);
                scoredPlayers.Add(scoredPlayerData);
                SessionManager.Instance.SaveData(scoredPlayerData);
                shouldFinalize = true; // Changes scene on next update order
            }
        }
    }
}