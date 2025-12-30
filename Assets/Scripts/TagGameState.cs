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

    public NetworkVariable<bool> isGameStarted = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        Instance = this;
    }

    void OnGUI()
    {
        // Prevents OnGUI from running after shutdown or exit
        if (NetworkManager.Singleton == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 200, 200));

        if (GUILayout.Button("Shutdown"))
        {
            NetworkManager.Singleton.Shutdown();
        } 

        // Currently using just 2 players for testing
        if (NetworkManager.Singleton.ConnectedClientsList.Count >= 2 && !isGameStarted.Value && NetworkManager.Singleton.IsHost)
        {
            if (GUILayout.Button("Start Game"))
            {
                // We have to do a lot of parsing as custom class objects are not serializable with Netcode (currently)
                var players = NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values.ToList();
                var random  = UnityEngine.Random.Range(0, players.Count);
                var selectedPlayer = players[random];
                SetInitialTaggedPlayerServerRpc(selectedPlayer.NetworkObjectId);
            }
        }
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// Set a player as tagged on the server.
    /// Used for initializing the game state.
    /// </summary>
    /// <param name="playerId"></param> ID of player that starts tagged.
    [ServerRpc]
    private void SetInitialTaggedPlayerServerRpc(ulong playerId)
    {
        var playerObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[playerId]
                    .GetComponent<PlayerTagMovement>();
        playerObject.isTaggedNet.Value = true;
        Instance.taggedPlayerIdNet.Value = playerId;
        Instance.isGameStarted.Value = true;
    }
}