using Unity.Netcode;
using UnityEngine;

public class NetworkManagerController : MonoBehaviour
{
    void OnGUI()
    {
        // Prevents OnGUI from running after shutdown or exit
        if (NetworkManager.Singleton == null)
            return;

        GUILayout.BeginArea(new Rect(10, 10, 200, 200));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Host")) NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
            if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
        }
        else
        {
            if (GUILayout.Button("Shutdown")) NetworkManager.Singleton.Shutdown();
        }

        GUILayout.EndArea();
    }

    void OnDestroy()
{
    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        NetworkManager.Singleton.Shutdown();
}

}

