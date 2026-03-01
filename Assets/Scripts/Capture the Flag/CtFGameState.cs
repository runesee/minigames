using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class CtFGameState : NetworkBehaviour
{
    public static CtFGameState Instance { get; private set; }

    public NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(
        GameState.Initializing,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<int> blueScore = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<int> greenScore = new NetworkVariable<int>(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );
    public TMP_Text blueScoreText;
    public TMP_Text greenScoreText;
    public readonly float[] scores = { 6f, 6f, 3f, 3f };
    private bool shouldChangeScene = false;
    public List<PlayerData> PlayerDataList = new List<PlayerData>();

    public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
    {
        public FixedString64Bytes Guid;
        public FixedString64Bytes nickname;
        public FixedString64Bytes color;
        public PlayerCtF.Team team;
        public double LastTagTime;

        public PlayerData(FixedString64Bytes Guid, FixedString64Bytes nickname, FixedString64Bytes color, PlayerCtF.Team team, double LastTagTime)
        {
            this.Guid = Guid;
            this.nickname = nickname;
            this.color = color;
            this.team = team;
            this.LastTagTime = LastTagTime;
        }

        public PlayerData(FixedString64Bytes Guid)
        {
            this.Guid = Guid;
            this.nickname = "";
            this.color = "";
            this.team = PlayerCtF.Team.None;
            this.LastTagTime = 0d;
        }

        public bool Equals(PlayerData other)
        {
            return 
            (
                Guid.Equals(other.Guid) && 
                nickname.Equals(other.nickname) &&
                color.Equals(other.color) &&
                team.Equals(other.team) &&
                LastTagTime.Equals(other.LastTagTime)
            );    
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Guid);
            serializer.SerializeValue(ref nickname);
            serializer.SerializeValue(ref color);
            serializer.SerializeValue(ref team);
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
                var player = obj.GetComponent<PlayerCtF>();
                if (!player) continue;
                PlayerData data = player.GetTagData();
                this.PlayerDataList.Add(data);
            }
            var rankedPlayers = PlayerDataList.OrderBy(p => p.team.ToString()).ToList();
            if (greenScore.Value > blueScore.Value) rankedPlayers.Reverse();
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