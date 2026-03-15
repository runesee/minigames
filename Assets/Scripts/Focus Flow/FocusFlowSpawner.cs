using UnityEngine;
using Unity.Netcode;

public class FocusFlowDataSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject focusFlowDataPrefab;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds) SpawnForClient(clientId);
        NetworkManager.Singleton.OnClientConnectedCallback += SpawnForClient;
    }

    private void SpawnForClient(ulong clientId)
    {
        if (!IsServer) return;
        GameObject obj = Instantiate(focusFlowDataPrefab);
        NetworkObject netObj = obj.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId, true);
    }

    public override void OnNetworkDespawn()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= SpawnForClient;
    }
}
