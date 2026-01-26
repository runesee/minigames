using UnityEngine;
using Unity.Netcode;

public class NetworkManagerPersistence : MonoBehaviour
{
    private void Awake()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.gameObject == gameObject)
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
