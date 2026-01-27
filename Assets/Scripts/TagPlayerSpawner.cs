using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using PlayPulse.Api.Utils;

public class TagPlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab; 
    [SerializeField] private List<Vector3> spawnPoints = new List<Vector3>();
    public static TagPlayerSpawner Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        spawnPoints.Add(new Vector3(14f, 1f, -0.5f));
        spawnPoints.Add(new Vector3(-14f, 1f, -0.5f));
        spawnPoints.Add(new Vector3(0f, 1f, -5f));
        spawnPoints.Add(new Vector3(0f, 1f, 5f));
        spawnPoints.Add(new Vector3(-7f, 1f, 9f));
        spawnPoints.Add(new Vector3(7f, 1f, 9f));
        spawnPoints.Add(new Vector3(-7f, 1f, -10f));
        spawnPoints.Add(new Vector3(7f, 1f, -10f));

        NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayer;
        SpawnPlayer(NetworkManager.Singleton.LocalClientId); // explicitly spawn host player
    }


    private void SpawnPlayer(ulong clientId)
    {
        if (!IsServer) return;
        // TODO : Need to check if space is occupied for late-joining clients
        Vector3 spawnPoint = spawnPoints.GetRandom();
        spawnPoints.Remove(spawnPoint);
        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint, Quaternion.identity);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId, true);
    }

    private void OnNetworkDestroy()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= SpawnPlayer;
    }
}
