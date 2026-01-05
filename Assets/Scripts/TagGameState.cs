using Unity.Netcode;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;

public class TagGameState : NetworkBehaviour
{
    public static TagGameState Instance { get; private set; }

    public NetworkVariable<ulong> taggedPlayerIdNet = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(
        GameState.Initializing,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public enum GameState
    {
        Initializing,
        Idling,
        Running,
        Stopped,
    }

    public override void OnNetworkSpawn()
    {
        Instance = this;
        if (!IsOwner) return;
        SetGameStateServerRpc(GameState.Idling);
    }
    
    /// <summary>
    /// Update the current GameState (Initializing, Idling, Running or Stopped).
    /// </summary>
    /// <param name="state">New GameState.</param>
    [ServerRpc]
    public void SetGameStateServerRpc(GameState state)
    {
        gameState.Value = state;
    }
}