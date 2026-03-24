using System.Collections.Generic;
using System.Text;
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
    public static bool USING_PLAYPULSE = true; // Flag for dev/bike movement toggling.
    public static readonly int PLAYER_COUNT = 4;
    public enum MinigameScene
    {
        MainMenu,
        Lobby,
        Scoreboard,
        TagTutorial,
        Tag,
        FocusFlowTutorial,
        FocusFlow,
        ColorFloodTutorial,
        ColorFlood,
        RedLightTutorial,
        RedLight,
        BalloonTagTutorial,
        BalloonTag,
        CaptureTheFlagTutorial,
        CaptureTheFlag,
        EndScreen,
    }
    public MinigameScene currentGameState = MinigameScene.MainMenu;
    public MinigameScene previousGameState = MinigameScene.MainMenu;
    private readonly HashSet<string> _takenColors = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ulong, string> _clientColorMap = new Dictionary<ulong, string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        if (!USING_PLAYPULSE) return;
        try
        {
            // Initialize connection with PP-service
            if (!PlayPulse.PlayPulseService.IsInitialized)
            {
                PlayPulse.PlayPulseService.Initialize(
                string.Empty,
                connectToBikeService: true,
                appSocketPathOverride: "127.0.0.1:13337",
                shellSocketPathOverride: "127.0.0.1:13337",
                useTcpSocket: true
                );
            }
        }
        catch { USING_PLAYPULSE = false; } // Bike connection failed, overriding to use keyboard instead
    }

    // Boilerplate function that will be called by the Lobby once the host presses start (after enough players have connected.)
    // Each GameState class (e.g. TagGameState) should handle updating scores for all players after minigame end in the SessionManager.
    public void StartGameSession()
    {
        if (!IsHost) return;
        NetworkManager.Singleton.SceneManager.LoadScene("TagTutorial", LoadSceneMode.Single);
        currentGameState = MinigameScene.TagTutorial;
        MusicManager.Instance?.PlaySong(currentGameState);
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
            case MinigameScene.FocusFlowTutorial:
                NetworkManager.Singleton.SceneManager.LoadScene("FocusFlow", LoadSceneMode.Single);
                currentGameState = MinigameScene.FocusFlow;
                break;
            case MinigameScene.FocusFlow:
                NetworkManager.Singleton.SceneManager.LoadScene("Scoreboard", LoadSceneMode.Single);
                currentGameState = MinigameScene.Scoreboard;
                break;
            case MinigameScene.ColorFloodTutorial:
                NetworkManager.Singleton.SceneManager.LoadScene("ColorFlood", LoadSceneMode.Single);
                currentGameState = MinigameScene.ColorFlood;
                break;
            case MinigameScene.ColorFlood:
                NetworkManager.Singleton.SceneManager.LoadScene("Scoreboard", LoadSceneMode.Single);
                currentGameState = MinigameScene.Scoreboard;
                break;
            case MinigameScene.RedLightTutorial:
                NetworkManager.Singleton.SceneManager.LoadScene("RedLight", LoadSceneMode.Single);
                currentGameState = MinigameScene.RedLight;
                break;
            case MinigameScene.RedLight:
                NetworkManager.Singleton.SceneManager.LoadScene("Scoreboard", LoadSceneMode.Single);
                currentGameState = MinigameScene.Scoreboard;
                break;
            case MinigameScene.BalloonTagTutorial:
                NetworkManager.Singleton.SceneManager.LoadScene("BalloonTag", LoadSceneMode.Single);
                currentGameState = MinigameScene.BalloonTag;
                break;
            case MinigameScene.BalloonTag:
                NetworkManager.Singleton.SceneManager.LoadScene("Scoreboard", LoadSceneMode.Single);
                currentGameState = MinigameScene.Scoreboard;
                break;
            case MinigameScene.CaptureTheFlagTutorial:
                NetworkManager.Singleton.SceneManager.LoadScene("CaptureTheFlag", LoadSceneMode.Single);
                currentGameState = MinigameScene.CaptureTheFlag;
                break;
            case MinigameScene.CaptureTheFlag:
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
                    NetworkManager.Singleton.SceneManager.LoadScene("ColorFloodTutorial", LoadSceneMode.Single);
                    currentGameState = MinigameScene.ColorFloodTutorial;
                }
                else if (previousGameState == MinigameScene.ColorFlood)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene("RedLightTutorial", LoadSceneMode.Single);
                    currentGameState = MinigameScene.RedLightTutorial;
                }
                else if (previousGameState == MinigameScene.RedLight)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene("BalloonTagTutorial", LoadSceneMode.Single);
                    currentGameState = MinigameScene.BalloonTagTutorial;
                }
                else if (previousGameState == MinigameScene.BalloonTag)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene("CaptureTheFlagTutorial", LoadSceneMode.Single);
                    currentGameState = MinigameScene.CaptureTheFlagTutorial;
                }
                else if (previousGameState == MinigameScene.CaptureTheFlag)
                {
                    NetworkManager.Singleton.SceneManager.LoadScene("EndScreen", LoadSceneMode.Single);
                    currentGameState = MinigameScene.EndScreen;
                }
                previousGameState = MinigameScene.Scoreboard;
                break;
            default:
                NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
                currentGameState = MinigameScene.MainMenu;
                break;
        }
        MusicManager.Instance?.PlaySong(currentGameState);
    }

    public void StartConnection(string ipAddress, ushort portNumber, bool isUserHost)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null) return;
        transport.SetConnectionData(ipAddress, portNumber);

        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        NetworkManager.Singleton.ConnectionApprovalCallback = HandleConnectionApproval;

        if (isUserHost)
        {
            _takenColors.Clear();
            _clientColorMap.Clear();
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedFromServer;
            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        }
        else
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    private void HandleConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        string colorHex = Encoding.UTF8.GetString(request.Payload);

        if (_takenColors.Contains(colorHex))
        {
            response.Approved = false;
            response.Reason = "That color is already taken. Please choose a different one.";
        }
        else
        {
            response.Approved = true;
            response.CreatePlayerObject = false;
            _takenColors.Add(colorHex);
            _clientColorMap[request.ClientNetworkId] = colorHex;
        }
    }

    private void OnClientDisconnectedFromServer(ulong clientId)
    {
        if (_clientColorMap.TryGetValue(clientId, out string colorHex))
        {
            _takenColors.Remove(colorHex);
            _clientColorMap.Remove(clientId);
        }
    }

    public void TerminateConnection()
    {
        _takenColors.Clear();
        _clientColorMap.Clear();
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedFromServer;
        NetworkManager.Singleton.ConnectionApprovalCallback = null;
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}
