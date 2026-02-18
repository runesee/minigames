using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using PlayPulse.Api.Utils;
using System.Collections;

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
        NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayer;
        StartCoroutine(SpawnAllPlayersAfterDelay());
    }
    
    private IEnumerator SpawnAllPlayersAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnPlayer(clientId);
        }
    }


    private void SpawnPlayer(ulong clientId)
    {
        if (!IsServer) return;
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
