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
    public Team team;

    public PlayerData(FixedString64Bytes Guid, FixedString64Bytes nickname, FixedString64Bytes color, float score, Team team)
    {
        this.Guid = Guid;
        this.nickname = nickname;
        this.color = color;
        this.score = score;
        this.team = team;
    }

    public PlayerData(FixedString64Bytes Guid)
    {
        this.Guid = Guid;
        this.nickname = "";
        this.color = "";
        this.score = 0f;
        this.team = Team.None;
    }

    public bool Equals(PlayerData other)
    {
        return 
        (
            Guid.Equals(other.Guid) && 
            nickname.Equals(other.nickname) &&
            color.Equals(other.color) &&
            score.Equals(other.score) &&
            team.Equals(other.team)
        );    
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Guid);
        serializer.SerializeValue(ref nickname);
        serializer.SerializeValue(ref color);
        serializer.SerializeValue(ref score);
        serializer.SerializeValue(ref team);
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
        PlayerDataList.Clear();
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var player = client.PlayerObject.GetComponent<Player>();
            if (player == null) continue;
            PlayerData playerData = player.GetPlayerData();
            if (!PlayerDataList.Contains(playerData)) PlayerDataList.Add(player.GetPlayerData());
        }
        SaveData();
        shouldChangeScene = true;
    }

    protected virtual void SaveData()
    {
        var rankedPlayers = GetOrderedPlayerDataList();
        int previousPlacement = 0;
        float[] scores = GetScores();

        for (int i = 0; i < rankedPlayers.Count; i++)
        {
            int placement;
            if (i > 0 && Math.Abs(rankedPlayers[i].score - rankedPlayers[i-1].score) < 0.001f) placement = previousPlacement;
            else placement = i + 1;
            
            previousPlacement = placement;
            var player = rankedPlayers[i];
            var globalSessionData = SessionManager.Instance.GetDataByGuid(player.Guid);
            float scoreToAdd = placement - 1 < scores.Length ? scores[placement - 1] : 0f;

            var scoredPlayerData = new SessionManager.PlayerData(
                player.Guid,
                player.nickname,
                player.color,
                scoreToAdd + globalSessionData.Score,
                placement
            );
            SessionManager.Instance.SaveData(scoredPlayerData);
        }
    }

    protected virtual List<PlayerData> GetOrderedPlayerDataList()
    {
        return PlayerDataList.OrderByDescending(p => p.score).ToList();
    }

    protected virtual float[] GetScores()
    {
        return this.scores;
    }
}