using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class RedLightPlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private TrafficLightController[] trafficLights;
    [SerializeField] public GameObject[] tracks;

    public static RedLightPlayerSpawner Instance { get; private set; }

    private HashSet<ulong> spawnedClients = new HashSet<ulong>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayer;
        
        StartCoroutine(SpawnAllPlayersAfterDelay());
    }
    
    private IEnumerator SpawnAllPlayersAfterDelay()
    {
        yield return new WaitForSeconds(2.0f);
        
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnPlayer(clientId);
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (!IsServer) return;

        if (spawnedClients.Contains(clientId))
        {
            return;
        }

        int playerIndex = (int)clientId;
        if (playerIndex >= spawnPoints.Length)
        {
            return;
        }

        Vector3 spawnPosition = spawnPoints[playerIndex].position;
        GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId, true);

        RedLightPlayerMovement playerMovement = playerInstance.GetComponent<RedLightPlayerMovement>();
        if (playerMovement != null && playerIndex < trafficLights.Length)
        {
            playerMovement.AssignTrafficLightAndTrack(trafficLights[playerIndex], playerIndex);
        }

        spawnedClients.Add(clientId);
    }

    private void OnNetworkDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= SpawnPlayer;
        }
    }
}
