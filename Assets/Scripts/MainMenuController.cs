using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 200));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Host"))
            {
                NetworkManager.Singleton.StartHost();
                NetworkManager.Singleton.SceneManager.LoadScene("TagScene", LoadSceneMode.Single);
            }

            if (GUILayout.Button("Client"))
            {
                NetworkManager.Singleton.StartClient();
            }
        }
        GUILayout.EndArea();
    }
}
