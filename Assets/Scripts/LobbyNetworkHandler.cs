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
        Debug.Log($"[LobbyNetworkHandler] Server received join request from client {clientId}, nickname: {nickname}");
        
        if (lobbyManager != null)
        {
            lobbyManager.OnPlayerJoinRequested(clientId, nickname, color);
        }
        else
        {
            Debug.LogError("[LobbyNetworkHandler] LobbyManager is null!");
        }
    }
    
    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void RemovePlayerServerRpc(ulong clientId)
    {
        Debug.Log($"[LobbyNetworkHandler] Server received remove request for client {clientId}");
        
        if (lobbyManager != null)
        {
            lobbyManager.OnPlayerLeaveRequested(clientId);
        }
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    public void AddPlayerClientRpc(ulong clientId, string nickname, Color color, int slotIndex)
    {
        Debug.Log($"[LobbyNetworkHandler] Client received add player: {nickname} at slot {slotIndex}");
        
        if (lobbyManager != null)
        {
            lobbyManager.AddPlayerToSlot(clientId, nickname, color, slotIndex);
        }
        else
        {
            Debug.LogError("[LobbyNetworkHandler] LobbyManager is null on client!");
        }
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    public void RemovePlayerClientRpc(ulong clientId, int slotIndex)
    {
        Debug.Log($"[LobbyNetworkHandler] Client received remove player for slot {slotIndex}");
        
        if (lobbyManager != null)
        {
            lobbyManager.RemovePlayerFromSlot(clientId, slotIndex);
        }
    }
}
