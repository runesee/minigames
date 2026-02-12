using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

/// <summary>
/// This Manager class controls scene management.
/// </summary>
public class MinigameManager : NetworkBehaviour
{
    public static MinigameManager Instance { get; private set; }

    public enum MinigameScene
    {
        MainMenu,
        Lobby,
        Scoreboard,
        TagTutorial,
        Tag,
    }

    private MinigameScene currentGameState = MinigameScene.MainMenu;
    private MinigameScene previousGameState = MinigameScene.MainMenu;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Boilerplate function that will be called by the Lobby once the host presses start (after enough players have connected.)
    // Each GameState class (e.g. TagGameState) should handle updating scores for all players after minigame end in the SessionManager.
    public void StartGameSession()
    {
        if (!IsHost) return;
        NetworkManager.Singleton.SceneManager.LoadScene("TagTutorial", LoadSceneMode.Single);
        currentGameState = MinigameScene.TagTutorial;
    }

    // General method called by each scene's GameState class once finished.
    // Loads a Scoreboard scene, which will rely on the updated SessionManager scores.
    public void SceneFinished()
    {
        if (!IsHost) return;
        switch (currentGameState)
        {
            case MinigameScene.TagTutorial:
                NetworkManager.Singleton.SceneManager.LoadScene("TagScene", LoadSceneMode.Single);
                currentGameState = MinigameScene.Tag;
                break;
            case MinigameScene.Tag:
                NetworkManager.Singleton.SceneManager.LoadScene("Scoreboard", LoadSceneMode.Single);
                currentGameState = MinigameScene.Scoreboard;
                break;
            case MinigameScene.Scoreboard:
                if (previousGameState == MinigameScene.Tag)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
                    currentGameState = MinigameScene.MainMenu;
                }
                break;
            default:
                NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
                currentGameState = MinigameScene.MainMenu;
                break;
        }
        previousGameState = currentGameState;
    }

    public void StartConnection(string ipAddress, ushort portNumber, bool isUserHost)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null) return;
        transport.SetConnectionData(ipAddress, portNumber);

        if (isUserHost)
        {

            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        }
        else
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    public void TerminateConnection()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}
