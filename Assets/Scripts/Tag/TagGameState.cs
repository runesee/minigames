using System.Collections.Generic;
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

    protected override List<PlayerData> GetOrderedPlayerDataList()
    {
        return PlayerDataList.OrderByDescending(p => p.score).Reverse().ToList();
    }
}