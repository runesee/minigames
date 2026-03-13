using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class FocusFlowGameState : NetworkBehaviour
{
    public static FocusFlowGameState Instance { get; private set; }
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
        public float totalScore;

        public PlayerData(FixedString64Bytes Guid, FixedString64Bytes nickname, FixedString64Bytes color, float totalScore)
        {
            this.Guid = Guid;
            this.nickname = nickname;
            this.color = color;
            this.totalScore = totalScore;
        }

        public PlayerData(FixedString64Bytes Guid)
        {
            this.Guid = Guid;
            this.nickname = "";
            this.color = "";
            this.totalScore = 0f;
        }

        public bool Equals(PlayerData other)
        {
            return 
            (
                Guid.Equals(other.Guid) && 
                nickname.Equals(other.nickname) &&
                color.Equals(other.color) &&
                totalScore.Equals(other.totalScore)
            );    
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Guid);
            serializer.SerializeValue(ref nickname);
            serializer.SerializeValue(ref color);
            serializer.SerializeValue(ref totalScore);
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

    [ServerRpc]
    public void SetGameStateServerRpc(GameState state)
    {
        gameState.Value = state;
        if (state == GameState.Handover)
        {
            foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
            {
                FocusFlowData data = obj.GetComponent<FocusFlowData>();
                if (data == null) continue;
                this.PlayerDataList.Add(data.GetFocusFlowData());
            }
            var rankedPlayers = PlayerDataList.OrderBy(p => p.totalScore).ToList();
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