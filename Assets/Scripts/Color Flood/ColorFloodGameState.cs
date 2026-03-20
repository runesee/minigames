using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;

public class ColorFloodGameState : MinigameGameState
{
    public static ColorFloodGameState Instance { get; private set; }
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
    protected float[] colorFloodScores = { 6f, 6f, 3f, 3f };

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        greenTileCount.OnValueChanged += OnGreenTileCountChanged;
        blueTileCount.OnValueChanged += OnBlueTileCountChanged;
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

    protected override List<PlayerData> GetOrderedPlayerDataList()
    {
        var rankedPlayers = PlayerDataList.OrderByDescending(p => p.team.ToString()).ToList();
        if (greenTileCount.Value < blueTileCount.Value) rankedPlayers.Reverse();
        return rankedPlayers;
    }

    protected override float[] GetScores()
    {
        return this.colorFloodScores;
    }

    public void SetScores(float[] scores)
    {
        this.colorFloodScores = scores;
    }
}
