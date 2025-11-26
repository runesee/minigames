using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject playerPrefab;

    void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            SpawnPlayer();
        }
    }

    void SpawnPlayer()
    {
        GameObject player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        player.GetComponent<NetworkObject>().Spawn();
    }
}
