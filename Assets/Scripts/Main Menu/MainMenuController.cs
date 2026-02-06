using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using static MinigameManager;
using System.Net;
using System.Net.Sockets;

public class MainMenuController : MonoBehaviour
{
    private string ipAddress = "127.0.0.1";
    private const ushort PORT = 7777;

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        if (NetworkManager.Singleton == null)
        {
            GUILayout.EndArea(); return;
        }

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            GUILayout.Label("Network Multiplayer", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
            GUILayout.Space(10);

            if (GUILayout.Button("Host", GUILayout.Height(40)))
            {
                MinigameManager.Instance.StartConnection(ipAddress, PORT, true);
            }

            GUILayout.Space(10);
            GUILayout.Label("Join Game:");
            GUILayout.Label("Enter Host IP Address:");  // TODO : Store previous host IP in playerprefs to make it faster to connect when testing/playing
            ipAddress = GUILayout.TextField(ipAddress, GUILayout.Width(200));

            if (GUILayout.Button("Join as Client", GUILayout.Height(40)))
            {
                MinigameManager.Instance.StartConnection(ipAddress, PORT, false);
            }
        }
        else
        {
            if (NetworkManager.Singleton.IsHost)
            {
                GUILayout.Label("Hosting Game", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
                GUILayout.Space(10);
                GUILayout.Label($"Your IP Address: {GetLocalIPAddress()}");
                GUILayout.Label($"Port: {PORT}");
                GUILayout.Label("Share this IP with other players!");
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                GUILayout.Label("Connected to Host", new GUIStyle(GUI.skin.label) { fontSize = 14 });
            }
        }
        GUILayout.EndArea();
    }

    private string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Could not get local IP address: {ex.Message}");
        }

        return "127.0.0.1";
    }
}
