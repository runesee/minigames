using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class FocusFlowDataSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject focusFlowDataPrefab;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        StartCoroutine(SpawnPlayers());
    }

    private IEnumerator SpawnPlayers()
    {
        yield return new WaitForSeconds(2f);
        if (IsServer) foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds) SpawnForClient(clientId);
        while (NetworkManager.Singleton.ConnectedClientsList.Count < 2 || FocusFlowGameState.Instance == null) yield return new WaitForSeconds(0.1f);
        if (IsServer) FocusFlowGameState.Instance.SetGameStateServerRpc(GameState.Running);
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
