using UnityEngine;
using Unity.Netcode;

public class LobbyNetworkHandler : NetworkBehaviour
{
    private LobbyManager lobbyManager;
    
    public void Initialize(LobbyManager manager)
    {
        lobbyManager = manager;
    }
    
    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void RequestJoinServerRpc(ulong clientId, string nickname, Color color)
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnPlayerJoinRequested(clientId, nickname, color);
        }
    }
    
    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void RemovePlayerServerRpc(ulong clientId)
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnPlayerRemoveRequested(clientId);
        }
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    public void AddPlayerClientRpc(ulong clientId, string nickname, Color color, int slotIndex)
    {
        if (lobbyManager != null)
        {
            lobbyManager.AddPlayerToSlot(clientId, nickname, color, slotIndex);
        }
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    public void RemovePlayerClientRpc(ulong clientId, int slotIndex)
    {
        if (lobbyManager != null)
        {
            lobbyManager.RemovePlayerFromSlot(clientId, slotIndex);
        }
    }
}
