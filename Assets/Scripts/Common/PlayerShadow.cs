using Unity.Netcode;
using UnityEngine;

public class PlayerShadow : NetworkBehaviour
{
    [SerializeField] public GameObject playerShadow;
    [SerializeField] public Material playerShadowColor;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            playerShadow.SetActive(true);
            playerShadow.GetComponentInChildren<MeshRenderer>().material.color = playerShadowColor.color;
        } 
    }
}
