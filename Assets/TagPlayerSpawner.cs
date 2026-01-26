using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

public class TagPlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab; 
    [SerializeField] private List<Vector3> spawnPoints = new List<Vector3>();
    public static TagPlayerSpawner Instance { get; private set; }


    void Start()
    {
        spawnPoints.Add(new Vector3(14f, 1f, -0.5f));
        spawnPoints.Add(new Vector3(-14f, 1f, -0.5f));
        spawnPoints.Add(new Vector3(0f, 1f, -5f));
        spawnPoints.Add(new Vector3(0f, 1f, 5f));
    }

    public override void OnNetworkSpawn()
    {
        Instance = this;
        if (IsServer) NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayer;
    }

    private void SpawnPlayer(ulong clientId)
    {
        // Need to check if space is occupied for late-joining clients
        System.Random rnd  = new System.Random();
        var index = rnd.Next(0, 3);
        Vector3 _point = spawnPoints[index];
        Debug.Log(_point);

        /*Vector3 spawnPoint = GetNextSpawnPoint();
        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint, spawnPoint.rotation);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId, true);*/
    }

    private void GetNextSpawnPoint(){}

    private void OnNetworkDestroy()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= SpawnPlayer;
    }
}
