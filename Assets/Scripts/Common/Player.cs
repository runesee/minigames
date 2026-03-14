using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public abstract class Player : NetworkBehaviour
{
    [SerializeField] protected SkinnedMeshRenderer PlayerSkinRenderer;
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
    public NetworkVariable<FixedString64Bytes> guidNet = new NetworkVariable<FixedString64Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        colorNet.OnValueChanged += OnSkinColorChanged;
        // Apply initial player-selected color
        var data = LocalPlayerStorage.Load();
        string color = IsOwner ? data.color : colorNet.Value.ToString();
        SetSkinColor(color);

        if (IsOwner)
        {
            // Apply player-selected nickname and color
            UpdateColorServerRpc(color);
            UpdateNicknameServerRpc(data.nickname);
            UpdateGuidServerRpc(data.guid);
        }
    }

    public override void OnNetworkDespawn()
    {
        colorNet.OnValueChanged -= OnSkinColorChanged;
    }

    public virtual void OnSkinColorChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
        SetSkinColor(newValue.Value.ToString());
    }

    protected virtual void SetSkinColor(string color)
    {
        if (PlayerSkinRenderer == null) return;
        UnityEngine.ColorUtility.TryParseHtmlString(color, out var skinColor);
        PlayerSkinRenderer.material.color = skinColor;
    }

    [ServerRpc]
    public virtual void UpdateColorServerRpc(string color)
    {
        colorNet.Value = new FixedString64Bytes(color);
    }

    [ServerRpc]
    public virtual void UpdateNicknameServerRpc(string nickname)
    {
        nicknameNet.Value = nickname;
    }

    [ServerRpc]
    public virtual void UpdateGuidServerRpc(string guid)
    {
        guidNet.Value = guid;
    }

    public abstract PlayerData GetPlayerData();
}
