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
        Tag,
    }

    private MinigameScene currentGameState = MinigameScene.MainMenu;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Boilerplate function that will be called by the Lobby once the host presses start (after enough players have connected.)
    // Should select the first game to play, load the scene, and save player data in sessionmanager.
    // The SessionManager already handles storing GUID and score on network spawn.
    // The MinigameManager needs to update the SessionManager's player scores after each minigame has concluded.
    public void StartGameSession()
    {
        if (!IsHost) return;
        // TODO : implement game selection once more games are in place.
        NetworkManager.Singleton.SceneManager.LoadScene("TagScene", LoadSceneMode.Single);
        currentGameState = MinigameScene.Tag;
    }

    // General method called by each scene's GameState class once finished.
    // Loads a Scoreboard scene, which will rely on the updated SessionManager scores.
    public void GameFinished()
    {
        // Based on the current scene, we already know which GameState called this function.
        NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        currentGameState = MinigameScene.Scoreboard;
    }


    /*
    Needs to control the flow:
    1. MenuManager lets user pick IP and host/join -> calls GameManager start function
    2. GameManager starts connection, loads Lobby scene on host.
    3. Lobby calls start function on GameManager

    Game over:
    1. TagGameState computes the winner of Tag
    2. TagGameState calls function in MinigameManager with this information
    3. MinigameManager updates the new total score in SessionManager
    4. MinigameManager also stores the previous store in SessionManager
    5. MinigameManager sets the scene to Scoreboard scene
    5. Some script in scoreboard scene gets new and previous scores, and displays the change in scores
    6. Scoreboard scene calls function in MinigameManager after ~20s

    Under the hood:
    - Only the host controls the MinigameManager.
    - MinigameManager maintains a list of all scenes, and which scene is currently loaded (state).
    - General function in MinigameManager that takes in a scene
    */


    public void StartConnection(string ipAddress, ushort portNumber, bool isUserHost)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null) return;
        transport.SetConnectionData(ipAddress, portNumber);

        if (isUserHost) {

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
