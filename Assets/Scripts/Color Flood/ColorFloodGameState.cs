using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class ColorFloodGameState : NetworkBehaviour
{
    public static ColorFloodGameState Instance { get; private set; }

    public enum Team
    {
        None,
        Green,
        Blue,
    }

    public NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(
        GameState.Initializing,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> greenTileCount = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> blueTileCount = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public TMP_Text greenTileCountText;
    public TMP_Text blueTileCountText;

    private readonly float[] winnerScores = { 6f, 6f };
    private readonly float[] loserScores = { 3f, 3f };
    private bool shouldChangeScene = false;
    public List<PlayerData> PlayerDataList = new List<PlayerData>();

    public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
    {
        public FixedString64Bytes Guid;
        public FixedString64Bytes nickname;
        public FixedString64Bytes color;
        public Team team;
        public int tilesOwned;

        public PlayerData(FixedString64Bytes guid, FixedString64Bytes nickname, FixedString64Bytes color, Team team, int tilesOwned)
        {
            this.Guid = guid;
            this.nickname = nickname;
            this.color = color;
            this.team = team;
            this.tilesOwned = tilesOwned;
        }

        public PlayerData(FixedString64Bytes guid)
        {
            this.Guid = guid;
            this.nickname = "";
            this.color = "";
            this.team = Team.None;
            this.tilesOwned = 0;
        }

        public bool Equals(PlayerData other)
        {
            return
            (
                Guid.Equals(other.Guid) &&
                nickname.Equals(other.nickname) &&
                color.Equals(other.color) &&
                team.Equals(other.team) &&
                tilesOwned.Equals(other.tilesOwned)
            );
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Guid);
            serializer.SerializeValue(ref nickname);
            serializer.SerializeValue(ref color);
            serializer.SerializeValue(ref team);
            serializer.SerializeValue(ref tilesOwned);
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
        greenTileCount.OnValueChanged += OnGreenTileCountChanged;
        blueTileCount.OnValueChanged += OnBlueTileCountChanged;

        if (!IsOwner) return;
        SetGameStateServerRpc(GameState.Idling);
    }

    public override void OnNetworkDespawn()
    {
        greenTileCount.OnValueChanged -= OnGreenTileCountChanged;
        blueTileCount.OnValueChanged -= OnBlueTileCountChanged;
    }

    private void OnGreenTileCountChanged(int previousValue, int newValue)
    {
        if (greenTileCountText != null) greenTileCountText.text = newValue.ToString();
    }

    private void OnBlueTileCountChanged(int previousValue, int newValue)
    {
        if (blueTileCountText != null) blueTileCountText.text = newValue.ToString();
    }

    [ServerRpc]
    public void SetGameStateServerRpc(GameState state)
    {
        gameState.Value = state;

        if (state != GameState.Handover) return;

        foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            var player = obj.GetComponent<PlayerColorFlood>();
            if (player == null) continue;
            PlayerDataList.Add(player.GetPlayerData());
        }

        bool greenWins = greenTileCount.Value >= blueTileCount.Value;
        List<PlayerData> winners = PlayerDataList.Where(p => p.team == (greenWins ? Team.Green : Team.Blue)).ToList();
        List<PlayerData> losers = PlayerDataList.Where(p => p.team == (greenWins ? Team.Blue : Team.Green)).ToList();
        List<PlayerData> rankedPlayers = winners.Concat(losers).ToList();

        for (int i = 0; i < rankedPlayers.Count; i++)
        {
            float score = i < winnerScores.Length ? winnerScores[i] : loserScores[i - winnerScores.Length];
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
                        $"Player {i + 1}",
                        new FixedString64Bytes("#" + PlayerColorManager.AvailableColors[i].ToHexString()),
                        0f
                    )
                );
            }
        }

        shouldChangeScene = true;
    }
}
