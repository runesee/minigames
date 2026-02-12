using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
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
    private readonly float[] scores = { 10f, 6f, 3f, 1f };
    private bool shouldChangeScene = false;
    public List<PlayerData> PlayerDataList = new List<PlayerData>();

    public enum GameState
    {
        Initializing,
        Idling,
        Running,
        Stopped,
        Handover,
    }

    public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
    {
        public FixedString64Bytes Guid;
        public FixedString64Bytes nickname;
        public FixedString64Bytes color;
        public float XPos;
        public float ZPos;
        public double TimeSpentTagged;
        public double LastTagTime;
        public bool IsTagged;

        public PlayerData(FixedString64Bytes Guid, FixedString64Bytes nickname, FixedString64Bytes color, float XPos, float ZPos, double TimeSpentTagged, double LastTagTime, bool IsTagged)
        {
            this.Guid = Guid;
            this.nickname = nickname;
            this.color = color;
            this.XPos = XPos;
            this.ZPos = ZPos;
            this.TimeSpentTagged = TimeSpentTagged;
            this.LastTagTime = LastTagTime;
            this.IsTagged = IsTagged;
        }

        public PlayerData(FixedString64Bytes Guid, FixedString64Bytes value)
        {
            this.Guid = Guid;
            this.nickname = "";
            this.color = "";
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
                nickname.Equals(other.nickname) &&
                color.Equals(other.color) &&
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
            serializer.SerializeValue(ref nickname);
            serializer.SerializeValue(ref color);
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
        if (!IsHost || !shouldChangeScene) return;
        shouldChangeScene = false;
        MinigameManager.Instance.SceneFinished();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        SetGameStateServerRpc(GameState.Idling);
    }

    /// <summary>
    /// Update the current GameState (Initializing, Idling, Running or Stopped).
    /// Updates SessionManager scores based on each player's timeSpentTagged.
    /// </summary>
    /// <param name="state">New GameState.</param>
    [ServerRpc]
    public void SetGameStateServerRpc(GameState state)
    {
        gameState.Value = state;
        if (state == GameState.Handover)
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
                FixedString64Bytes nickname = rankedPlayers[i].nickname.ToSafeString();
                FixedString64Bytes color = rankedPlayers[i].color;
                SessionManager.PlayerData globalSessionData = SessionManager.Instance.GetDataByGuid(guid);
                SessionManager.PlayerData scoredPlayerData = new SessionManager.PlayerData(guid, nickname, color, score + globalSessionData.Score);
                SessionManager.Instance.SaveData(scoredPlayerData);
            }
            if (rankedPlayers.Count < 4)
            {
                for (int i = rankedPlayers.Count; i < 4; i++)
                {
                    SessionManager.Instance.SaveData(
                        new SessionManager.PlayerData(
                            new FixedString64Bytes(Guid.NewGuid().ToString()),
                            $"Player {i+1}",
                            new FixedString64Bytes("#" + PlayerColorManager.AvailableColors[i].ToHexString()),
                            0f
                        )
                    );
                }
            }
            shouldChangeScene = true;
        }
    }
}