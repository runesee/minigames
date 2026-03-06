using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerColorFlood : NetworkBehaviour
{
    public NetworkVariable<FixedString64Bytes> guidNet = new NetworkVariable<FixedString64Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<FixedString64Bytes> nicknameNet = new NetworkVariable<FixedString64Bytes>(
        "Player",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<FixedString64Bytes> colorNet = new NetworkVariable<FixedString64Bytes>(
        "#D6877F",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<ColorFloodGameState.Team> teamNet = new NetworkVariable<ColorFloodGameState.Team>(
        ColorFloodGameState.Team.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public ColorFloodGameState.PlayerData GetPlayerData()
    {
        int tilesOwned = teamNet.Value == ColorFloodGameState.Team.Green
            ? ColorFloodGameState.Instance.greenTileCount.Value
            : ColorFloodGameState.Instance.blueTileCount.Value;

        return new ColorFloodGameState.PlayerData(
            guidNet.Value,
            nicknameNet.Value,
            colorNet.Value,
            teamNet.Value,
            tilesOwned
        );
    }
}
