using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    [Header("Player Slots")]
    [SerializeField] private Transform[] playerSlots;
    [SerializeField] private TextMeshProUGUI[] nicknameTexts;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject characterPreviewPrefab;
    
    [Header("UI")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI waitingText;
    
    private const int MAX_PLAYERS = 4;
    private Dictionary<ulong, LobbyPlayerData> connectedPlayers = new Dictionary<ulong, LobbyPlayerData>();
    private Dictionary<ulong, GameObject> playerPreviews = new Dictionary<ulong, GameObject>();
    private LobbyNetworkHandler networkHandler;
    
    private struct LobbyPlayerData
    {
        public string nickname;
        public Color color;
        public int slotIndex;
    }
    
    private void Awake()
    {
        if (playerSlots.Length != MAX_PLAYERS || nicknameTexts.Length != MAX_PLAYERS)
        {
            Debug.LogError("Player slots and nickname texts must have exactly 4 elements!");
        }
        
        for (int i = 0; i < nicknameTexts.Length; i++)
        {
            if (nicknameTexts[i] != null)
            {
                nicknameTexts[i].text = "";
            }
        }
    }
    
    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            
            GameObject handlerObj = new GameObject("LobbyNetworkHandler");
            networkHandler = handlerObj.AddComponent<LobbyNetworkHandler>();
            networkHandler.Initialize(this);
            
            NetworkObject networkObject = handlerObj.AddComponent<NetworkObject>();
            handlerObj.GetComponent<NetworkObject>().Spawn();
        }
        
        bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
            startGameButton.gameObject.SetActive(isServer);
        }
        
        if (waitingText != null)
        {
            waitingText.gameObject.SetActive(!isServer);
        }
        
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            string nickname = PlayerPrefs.GetString("Username", "Player");
            string colorHex = PlayerPrefs.GetString("Color", "#FFFFFF");
            Color color = Color.white;
            ColorUtility.TryParseHtmlString(colorHex, out color);
            
            networkHandler.RequestJoinServerRpc(NetworkManager.Singleton.LocalClientId, nickname, color);
        }
    }
    
    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(OnStartGameClicked);
        }
        
        if (networkHandler != null)
        {
            Destroy(networkHandler.gameObject);
        }
    }
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[Lobby] Client connected: {clientId}");
        UpdatePlayerCount();
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[Lobby] Client disconnected: {clientId}");
        
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && networkHandler != null)
        {
            networkHandler.RemovePlayerServerRpc(clientId);
        }
        
        UpdatePlayerCount();
    }
    
    public void OnPlayerJoinRequested(ulong clientId, string nickname, Color color)
    {
        if (connectedPlayers.Count >= MAX_PLAYERS)
        {
            Debug.LogWarning($"[Lobby] Lobby is full, rejecting client {clientId}");
            return;
        }
        
        int slotIndex = GetNextAvailableSlot();
        if (slotIndex == -1)
        {
            Debug.LogError("[Lobby] No available slots!");
            return;
        }
        
        LobbyPlayerData playerData = new LobbyPlayerData
        {
            nickname = nickname,
            color = color,
            slotIndex = slotIndex
        };
        
        connectedPlayers[clientId] = playerData;
        Debug.Log($"[Lobby] Player {nickname} joined in slot {slotIndex}");
        
        if (networkHandler != null)
        {
            networkHandler.AddPlayerClientRpc(clientId, nickname, color, slotIndex);
        }
    }
    
    public void OnPlayerRemoveRequested(ulong clientId)
    {
        if (connectedPlayers.ContainsKey(clientId))
        {
            int slotIndex = connectedPlayers[clientId].slotIndex;
            connectedPlayers.Remove(clientId);
            
            if (networkHandler != null)
            {
                networkHandler.RemovePlayerClientRpc(clientId, slotIndex);
            }
        }
    }
    
    public void AddPlayerToSlot(ulong clientId, string nickname, Color color, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MAX_PLAYERS)
        {
            Debug.LogError($"[Lobby] Invalid slot index: {slotIndex}");
            return;
        }
        
        if (nicknameTexts[slotIndex] != null)
        {
            nicknameTexts[slotIndex].text = nickname;
        }
        
        if (playerSlots[slotIndex] != null && characterPreviewPrefab != null)
        {
            ClearSlot(slotIndex);
            
            GameObject preview = Instantiate(characterPreviewPrefab, playerSlots[slotIndex]);
            preview.transform.localPosition = Vector3.zero;
            preview.transform.localRotation = Quaternion.identity;
            
            CharacterPreview characterPreview = preview.GetComponent<CharacterPreview>();
            if (characterPreview != null)
            {
                characterPreview.SetColor(color);
            }
            
            playerPreviews[clientId] = preview;
        }
        
        UpdatePlayerCount();
    }
    
    public void RemovePlayerFromSlot(ulong clientId, int slotIndex)
    {
        if (playerPreviews.ContainsKey(clientId))
        {
            Destroy(playerPreviews[clientId]);
            playerPreviews.Remove(clientId);
        }
        
        if (slotIndex >= 0 && slotIndex < MAX_PLAYERS && nicknameTexts[slotIndex] != null)
        {
            nicknameTexts[slotIndex].text = "";
        }
        
        UpdatePlayerCount();
    }
    
    private void ClearSlot(int slotIndex)
    {
        if (playerSlots[slotIndex] == null) return;
        
        foreach (Transform child in playerSlots[slotIndex])
        {
            Destroy(child.gameObject);
        }
    }
    
    private int GetNextAvailableSlot()
    {
        bool[] usedSlots = new bool[MAX_PLAYERS];
        
        foreach (var playerData in connectedPlayers.Values)
        {
            if (playerData.slotIndex >= 0 && playerData.slotIndex < MAX_PLAYERS)
            {
                usedSlots[playerData.slotIndex] = true;
            }
        }
        
        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            if (!usedSlots[i])
            {
                return i;
            }
        }
        
        return -1;
    }
    
    private void UpdatePlayerCount()
    {
        if (playerCountText != null)
        {
            bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
            int playerCount = isServer ? connectedPlayers.Count : playerPreviews.Count;
            playerCountText.text = $"Players: {playerCount}/{MAX_PLAYERS}";
        }
    }
    
    private void OnStartGameClicked()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;
        
        if (connectedPlayers.Count == 0)
        {
            Debug.LogWarning("[Lobby] Cannot start game with no players");
            return;
        }
        
        Debug.Log("[Lobby] Starting game...");
        NetworkManager.Singleton.SceneManager.LoadScene("TagScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
