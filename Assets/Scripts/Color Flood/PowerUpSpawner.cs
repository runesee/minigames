using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PowerUpSpawner : NetworkBehaviour
{
    public static PowerUpSpawner Instance { get; private set; }

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 8f;
    [SerializeField] private float initialDelay = 5f;
    [SerializeField] private int maxActivePowerUps = 5;

    [Header("Power-Up Appearance")]
    [SerializeField] private Material speedBoostMaterial;
    [SerializeField] private float powerUpScale = 0.6f;
    [SerializeField] private float floatHeight = 0.8f;

    private readonly HashSet<int> activeIds = new HashSet<int>();
    private readonly Dictionary<int, GameObject> localPowerUps = new Dictionary<int, GameObject>();
    private int nextPowerUpId;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(SpawnLoop());
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (ColorFloodGameState.Instance == null ||
               ColorFloodGameState.Instance.gameState.Value != GameState.Running)
        {
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(initialDelay);

        while (ColorFloodGameState.Instance != null &&
               ColorFloodGameState.Instance.gameState.Value == GameState.Running)
        {
            if (activeIds.Count < maxActivePowerUps)
            {
                SpawnRandomPowerUp();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private const int MaxSpawnAttempts = 20;

    private void SpawnRandomPowerUp()
    {
        if (TileGrid.Instance == null) return;

        int gridWidth = TileGrid.Instance.GridWidth;
        int gridHeight = TileGrid.Instance.GridHeight;
        float tileSize = TileGrid.Instance.TileSize;

        float gridOriginX = -(gridWidth * tileSize) / 2f + tileSize / 2f;
        float gridOriginZ = -(gridHeight * tileSize) / 2f + tileSize / 2f;

        Vector3 halfExtents = Vector3.one * (powerUpScale * 0.5f);

        for (int attempt = 0; attempt < MaxSpawnAttempts; attempt++)
        {
            int col = Random.Range(0, gridWidth);
            int row = Random.Range(0, gridHeight);

            Vector3 position = new Vector3(
                gridOriginX + col * tileSize,
                floatHeight,
                gridOriginZ + row * tileSize
            );

            if (Physics.CheckBox(position, halfExtents, Quaternion.identity,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            int id = nextPowerUpId++;
            activeIds.Add(id);
            SpawnPowerUpClientRpc(id, position);
            return;
        }
    }

    [ClientRpc]
    private void SpawnPowerUpClientRpc(int pickupId, Vector3 position)
    {
        GameObject powerUp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        powerUp.name = $"SpeedBoost_{pickupId}";
        powerUp.transform.position = position;
        powerUp.transform.localScale = Vector3.one * powerUpScale;
        powerUp.transform.rotation = Quaternion.Euler(45f, 0f, 45f);

        BoxCollider col = powerUp.GetComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = Vector3.one * 2f;

        MeshRenderer rend = powerUp.GetComponent<MeshRenderer>();
        if (speedBoostMaterial != null)
        {
            rend.material = speedBoostMaterial;
        }

        SpeedBoostPickup pickup = powerUp.AddComponent<SpeedBoostPickup>();
        pickup.pickupId = pickupId;

        localPowerUps[pickupId] = powerUp;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void CollectSpeedBoostServerRpc(int pickupId, RpcParams rpcParams = default)
    {
        if (!activeIds.Contains(pickupId)) return;
        activeIds.Remove(pickupId);

        ulong senderClientId = rpcParams.Receive.SenderClientId;

        foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            var player = obj.GetComponent<PlayerColorFlood>();
            if (player != null && player.OwnerClientId == senderClientId)
            {
                player.GrantSpeedBoostClientRpc();
                break;
            }
        }

        RemovePowerUpClientRpc(pickupId);
    }

    [ClientRpc]
    private void RemovePowerUpClientRpc(int pickupId)
    {
        if (localPowerUps.TryGetValue(pickupId, out GameObject powerUp))
        {
            Destroy(powerUp);
            localPowerUps.Remove(pickupId);
        }
    }

    public override void OnNetworkDespawn()
    {
        foreach (var kvp in localPowerUps)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }

        localPowerUps.Clear();
        activeIds.Clear();
    }
}
