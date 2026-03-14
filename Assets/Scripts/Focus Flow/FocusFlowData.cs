using Unity.Netcode;

public class FocusFlowData : Player
{
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
    public void UpdateScoreServerRpc(float score)
    {
        totalScoreNet.Value = score;
    }
}
