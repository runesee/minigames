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

    private const int RequiredPlayerCount = 2;
    private readonly HashSet<ulong> spawnedClients = new HashSet<ulong>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        StartCoroutine(SpawnAllPlayersAfterDelay());
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        SpawnPlayer(clientId);
    }

    private IEnumerator SpawnAllPlayersAfterDelay()
    {
        // Spawn any clients already connected before this object's OnNetworkSpawn ran.
        yield return new WaitForSeconds(0.5f);
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            SpawnPlayer(clientId);

        // Wait until all expected player objects are spawned — no GUID check so
        // empty LocalPlayerStorage never deadlocks the game.
        yield return new WaitUntil(() => GetAllPlayers().Count == RequiredPlayerCount);

        AssignTeams();

        while (ColorFloodGameState.Instance == null)
            yield return new WaitForSeconds(0.1f);

        ColorFloodGameState.Instance.SetGameStateServerRpc(GameState.Running);
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (!IsServer) return;
        // Guard: each client must only ever receive one player object.
        if (!spawnedClients.Add(clientId)) return;

        GameObject playerInstance = Instantiate(playerPrefab, new Vector3(0f, 1f, 0f), Quaternion.identity);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId, true);
    }

    private void AssignTeams()
    {
        List<PlayerColorFlood> players = GetAllPlayers();
        // Sort by OwnerClientId — always available on the server and deterministic,
        // unlike GUIDs which may be empty during testing.
        players.Sort((a, b) => a.OwnerClientId.CompareTo(b.OwnerClientId));

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
