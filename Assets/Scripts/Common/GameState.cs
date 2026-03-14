using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;

public enum GameState {
    Initializing,
    Idling,
    Running,
    Stopped,
    Handover,
}

public enum Team
{
    None,
    Green,
    Blue,
}

public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
{
    public FixedString64Bytes Guid;
    public FixedString64Bytes nickname;
    public FixedString64Bytes color;
    public float score;
    public float ordering;

    public PlayerData(FixedString64Bytes Guid, FixedString64Bytes nickname, FixedString64Bytes color, float score, float ordering)
    {
        this.Guid = Guid;
        this.nickname = nickname;
        this.color = color;
        this.score = score;
        this.ordering = ordering;
    }

    public PlayerData(FixedString64Bytes Guid)
    {
        this.Guid = Guid;
        this.nickname = "";
        this.color = "";
        this.score = 0f;
        this.ordering = 0f;
    }

    public bool Equals(PlayerData other)
    {
        return 
        (
            Guid.Equals(other.Guid) && 
            nickname.Equals(other.nickname) &&
            color.Equals(other.color) &&
            score.Equals(other.score) &&
            ordering.Equals(other.ordering)
        );    
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Guid);
        serializer.SerializeValue(ref nickname);
        serializer.SerializeValue(ref color);
        serializer.SerializeValue(ref score);
        serializer.SerializeValue(ref ordering);
    }
}

public abstract class MinigameGameState : NetworkBehaviour
{
    public NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(
        GameState.Initializing,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    protected float[] scores = { 10f, 6f, 3f, 1f };
    protected bool shouldChangeScene = false;
    protected List<PlayerData> PlayerDataList = new List<PlayerData>();

    protected virtual void Update()
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
        if (state != GameState.Handover) return;
        foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            var player = obj.GetComponent<Player>();
            if (player == null) continue;
            PlayerDataList.Add(player.GetPlayerData()); 
        }
        SaveData();
        shouldChangeScene = true;
    }

    protected virtual void SaveData()
    {
        var rankedPlayers = PlayerDataList.OrderByDescending(p => p.ordering).ToList();
        for (int i = 0; i < rankedPlayers.Count; i++)
        {
            float score = i < scores.Length ? scores[i] : 0f;
            var player = rankedPlayers[i];
            var globalSessionData = SessionManager.Instance.GetDataByGuid(player.Guid);
            var scoredPlayerData = new SessionManager.PlayerData(
                player.Guid,
                player.nickname,
                player.color,
                score + globalSessionData.Score
            );
            SessionManager.Instance.SaveData(scoredPlayerData);
        }
    }
}