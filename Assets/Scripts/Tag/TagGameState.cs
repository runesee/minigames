using System.Linq;
using Unity.Netcode;

public class TagGameState : MinigameGameState
{
    public static TagGameState Instance { get; private set; }
    public NetworkVariable<ulong> taggedPlayerIdNet = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        Instance = this;
    }

    protected override void SaveData()
    {
        var rankedPlayers = PlayerDataList.OrderBy(p => p.ordering).ToList();
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