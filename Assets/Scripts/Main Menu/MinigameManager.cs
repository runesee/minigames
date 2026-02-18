using System.Collections.Generic;
using PlayPulse.Core.Api.Dtos;
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
        FocusFlow,
        FocusFlowTutorial,
        BalloonTag,
        BalloonTagTutorial,
    }

    public MinigameScene currentGameState = MinigameScene.MainMenu;
    public MinigameScene previousGameState = MinigameScene.MainMenu;

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
        NetworkManager.Singleton.SceneManager.LoadScene("TagScene", LoadSceneMode.Single);
        currentGameState = MinigameScene.Tag;
    }

    // General method called by each scene's GameState class once finished.
    // Loads a Scoreboard scene, which will rely on the updated SessionManager scores.
    public void SceneFinished()
    {
        if (!IsHost) return;
        if (!(currentGameState == MinigameScene.Scoreboard)) previousGameState = currentGameState;
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
            case MinigameScene.FocusFlow:
                NetworkManager.Singleton.SceneManager.LoadScene("Scoreboard", LoadSceneMode.Single);
                currentGameState = MinigameScene.Scoreboard;
                break;
            case MinigameScene.FocusFlowTutorial:
                NetworkManager.Singleton.SceneManager.LoadScene("FocusFlow", LoadSceneMode.Single);
                currentGameState = MinigameScene.FocusFlow;
                break;
            case MinigameScene.BalloonTag:
                NetworkManager.Singleton.SceneManager.LoadScene("Scoreboard", LoadSceneMode.Single);
                currentGameState = MinigameScene.Scoreboard;
                break;
            case MinigameScene.Scoreboard:
                if (previousGameState == MinigameScene.Tag)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene("FocusFlowTutorial", LoadSceneMode.Single);
                    currentGameState = MinigameScene.FocusFlowTutorial;
                }
                else if (previousGameState == MinigameScene.FocusFlow)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene("TailTagTutorial", LoadSceneMode.Single);
                    currentGameState = MinigameScene.BalloonTagTutorial;
                }
                else if (previousGameState == MinigameScene.BalloonTag)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
                    currentGameState = MinigameScene.MainMenu;
                }
                previousGameState = MinigameScene.Scoreboard;
                break;
            default:
                NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
                currentGameState = MinigameScene.MainMenu;
                break;
        }
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
