using Unity.Collections;
using Unity.Netcode;

public class FocusFlowData : Player
{
    public NetworkVariable<FixedString64Bytes> guidNet = new NetworkVariable<FixedString64Bytes>(
    "",
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );
    public NetworkVariable<FixedString64Bytes> colorNet = new NetworkVariable<FixedString64Bytes>(
    "#D6877F",
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );
    public NetworkVariable<FixedString64Bytes> nicknameNet = new NetworkVariable<FixedString64Bytes>(
    "Player",
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );
    public NetworkVariable<float> totalScoreNet = new NetworkVariable<float>(
    0f,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
    );
    public static FocusFlowData LocalInstance;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            var data = LocalPlayerStorage.Load();
            LocalInstance = this;
            UpdateColorServerRpc(data.color);
            UpdateNicknameServerRpc(data.nickname);
            UpdateGuidServerRpc(data.guid);
        }
    }

    public override PlayerData GetPlayerData()
    {
        PlayerData playerData = new PlayerData(
            guidNet.Value,
            nicknameNet.Value,
            colorNet.Value,
            totalScoreNet.Value,
            totalScoreNet.Value
        );
        return playerData;
    }

    [ServerRpc]
    public void UpdateGuidServerRpc(string guid)
    {
        guidNet.Value = guid;
    }

    [ServerRpc]
    public void UpdateColorServerRpc(string color)
    {
        colorNet.Value = new FixedString64Bytes(color);
    }

    [ServerRpc]
    public void UpdateNicknameServerRpc(string nickname)
    {
        nicknameNet.Value = nickname;
    }
    [ServerRpc]
    public void UpdateScoreServerRpc(float score)
    {
        totalScoreNet.Value = score;
    }
}
