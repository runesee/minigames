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
    private readonly float[] winnerScores = { 6f, 6f };
    private readonly float[] loserScores = { 3f, 3f };

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

    public override void SaveData()
    {
        var rankedPlayers = PlayerDataList.OrderBy(p => p.ordering).ToList();
        if (greenTileCount.Value < blueTileCount.Value) rankedPlayers.Reverse();
        for (int i = 0; i < rankedPlayers.Count; i++)
        {
            float score = i < winnerScores.Length ? winnerScores[i] : loserScores[i - winnerScores.Length];
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
