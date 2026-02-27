using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WallController : NetworkBehaviour
{
    public List<GameObject> orangeWalls;
    public List<GameObject> redWalls;
    private bool wall_toggle = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer) StartCoroutine(WallRoutine());
    }

    IEnumerator WallRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(4f);
            wall_toggle = !wall_toggle;
            UpdateWallsClientRpc(wall_toggle);
        }
    }

    private void UpdateWalls(bool wall_toggle)
    {
        foreach (var wall in orangeWalls)
        {
            wall.gameObject.SetActive(wall_toggle);
        }
        foreach (var wall in redWalls)
        {
            wall.gameObject.SetActive(!wall_toggle);
        }
    }

    [ClientRpc]
    void UpdateWallsClientRpc(bool wall_toggle)
    {
        UpdateWalls(wall_toggle);
    }
}

