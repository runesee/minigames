using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;

public class BalloonTagGameState : NetworkBehaviour
{
    public static BalloonTagGameState Instance { get; private set; }

    public NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(
        GameState.Initializing,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly float[] scores = { 10f, 6f, 3f, 1f };
    private bool shouldChangeScene = false;
    public List<PlayerData> PlayerDataList = new List<PlayerData>();

    public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
    {
        public FixedString64Bytes Guid;
        public FixedString64Bytes nickname;
        public FixedString64Bytes color;
        public float balloonCount;
        public double LastTagTime;

        public PlayerData(FixedString64Bytes Guid, FixedString64Bytes nickname, FixedString64Bytes color, float balloonCount, double LastTagTime)
        {
            this.Guid = Guid;
            this.nickname = nickname;
            this.color = color;
            this.balloonCount = balloonCount;
            this.LastTagTime = LastTagTime;
        }

        public PlayerData(FixedString64Bytes Guid)
        {
            this.Guid = Guid;
            this.nickname = "";
            this.color = "";
            this.balloonCount = 2f;
            this.LastTagTime = 0d;
        }

        public bool Equals(PlayerData other)
        {
            return 
            (
                Guid.Equals(other.Guid) && 
                nickname.Equals(other.nickname) &&
                color.Equals(other.color) &&
                balloonCount.Equals(other.balloonCount) &&
                LastTagTime.Equals(other.LastTagTime)
            );    
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Guid);
            serializer.SerializeValue(ref nickname);
            serializer.SerializeValue(ref color);
            serializer.SerializeValue(ref balloonCount);
            serializer.SerializeValue(ref LastTagTime);
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
                var player = obj.GetComponent<PlayerBalloonTag>();
                if (!player) continue;
                PlayerData data = player.GetTagData();
                this.PlayerDataList.Add(data);
            }
            var rankedPlayers = PlayerDataList.OrderBy(p => p.balloonCount).ToList();
            rankedPlayers.Reverse();
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
            shouldChangeScene = true;
        }
    }
}