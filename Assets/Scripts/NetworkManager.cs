using Unity.Netcode;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;

public class NetworkManagerController : MonoBehaviour
{
    public enum ConnectionStatus
    {
        Connected,
        Disconnected
    }
    public bool isGameStarted = false;
    private Dictionary<uint, PlayerTagMovement> players = new Dictionary<uint, PlayerTagMovement>();
    public event Action<ulong, ConnectionStatus> OnClientConnectionNotification;

    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
    }

    void OnGUI()
    {
        // Prevents OnGUI from running after shutdown or exit
        if (NetworkManager.Singleton == null)
            return;

        GUILayout.BeginArea(new Rect(10, 10, 200, 200));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Host")) NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
            if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
        }
        else
        {
            if (GUILayout.Button("Shutdown"))
            {
                isGameStarted = false;
                NetworkManager.Singleton.Shutdown();
            } 
        }

        // Currently using just 2 players for testing
        if (NetworkManager.Singleton.ConnectedClientsList.Count >= 2 && !isGameStarted && NetworkManager.Singleton.IsHost && TagGameState.Instance != null)
        {
            if (GUILayout.Button("Start Game"))
            {
                isGameStarted = true;
                // We have to do a lot of parsing as custom class objects are not serializable with Netcode (currently)
                var players = NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values.ToList();
                var random  = UnityEngine.Random.Range(0, players.Count);
                var selectedPlayer = players[random];
                SetInitialTaggedPlayerServerRpc(selectedPlayer.NetworkObjectId);
            }
        }

        GUILayout.EndArea();
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
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
        TagGameState.Instance.taggedPlayerIdNet.Value = playerId;
        //TagGameState.Instance.isGameStarted.Value = true;
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        OnClientConnectionNotification?.Invoke(clientId, ConnectionStatus.Connected);
    }

    private void OnClientDisconnectCallback(ulong clientId)
    {
        OnClientConnectionNotification?.Invoke(clientId, ConnectionStatus.Disconnected);
    }
}

