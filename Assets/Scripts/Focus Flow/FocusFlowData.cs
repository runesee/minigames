using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class FocusFlowData : NetworkBehaviour

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
            LocalInstance = this;
            // Save player data for scoreboard handover
            string color = PlayerPrefs.GetString("Color");
            string nickname = PlayerPrefs.GetString("Username");
            string guid = PlayerPrefs.GetString("Guid");
            UpdateColorServerRpc(color);
            UpdateNicknameServerRpc(nickname);
            UpdateGuidServerRpc(guid);
        }
    }

    public FocusFlowGameState.PlayerData GetFocusFlowData()
    {
        FocusFlowGameState.PlayerData playerData = new FocusFlowGameState.PlayerData(
            guidNet.Value,
            nicknameNet.Value,
            colorNet.Value,
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
