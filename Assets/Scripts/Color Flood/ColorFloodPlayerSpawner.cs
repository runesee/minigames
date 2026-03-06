using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ColorFloodPlayerSpawner : NetworkBehaviour
{
    public static ColorFloodPlayerSpawner Instance { get; private set; }

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private List<Vector3> greenSpawns = new List<Vector3>();
    [SerializeField] private List<Vector3> blueSpawns = new List<Vector3>();

    private int greenSpawnTally = 0;
    private int blueSpawnTally = 0;

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

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= SpawnPlayer;
    }

    private IEnumerator SpawnAllPlayersAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            SpawnPlayer(clientId);

        yield return new WaitUntil(() =>
        {
            var players = GetAllPlayers();
            return players.Count == 4 && players.All(p => p.guidNet.Value.Length > 0);
        });

        AssignTeams();
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (!IsServer) return;

        GameObject playerInstance = Instantiate(playerPrefab, new Vector3(0f, 1f, 0f), Quaternion.identity);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId, true);
    }

    private void AssignTeams()
    {
        List<PlayerColorFlood> players = GetAllPlayers();
        players.Sort((a, b) => string.Compare(
            a.guidNet.Value.ToString(),
            b.guidNet.Value.ToString(),
            System.StringComparison.Ordinal));

        for (int i = 0; i < players.Count; i++)
        {
            ColorFloodGameState.Team team = (i % 2 == 0)
                ? ColorFloodGameState.Team.Green
                : ColorFloodGameState.Team.Blue;

            players[i].teamNet.Value = team;

            Vector3 spawnPosition;
            if (team == ColorFloodGameState.Team.Green)
            {
                spawnPosition = greenSpawns.Count > greenSpawnTally
                    ? greenSpawns[greenSpawnTally++]
                    : new Vector3(-5f, 1f, 0f);
            }
            else
            {
                spawnPosition = blueSpawns.Count > blueSpawnTally
                    ? blueSpawns[blueSpawnTally++]
                    : new Vector3(5f, 1f, 0f);
            }

            players[i].TeleportClientRpc(spawnPosition, team);
        }
    }

    private List<PlayerColorFlood> GetAllPlayers()
    {
        return NetworkManager.Singleton.SpawnManager.SpawnedObjectsList
            .Select(obj => obj.GetComponent<PlayerColorFlood>())
            .Where(p => p != null)
            .ToList();
    }
}
