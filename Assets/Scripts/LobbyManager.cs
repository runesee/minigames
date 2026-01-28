using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class LobbyManager : NetworkBehaviour
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
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsClient)
        {
            string nickname = PlayerPrefs.GetString("Username", "Player");
            string colorHex = PlayerPrefs.GetString("Color", "#FFFFFF");
            Color color = Color.white;
            ColorUtility.TryParseHtmlString(colorHex, out color);
            
            Debug.Log($"[Lobby] Requesting to join with nickname: {nickname}, color: {colorHex}");
            RequestJoinServerRpc(NetworkManager.Singleton.LocalClientId, nickname, color);
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
    }
    
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[Lobby] Client connected: {clientId}");
        
        if (IsServer)
        {
            foreach (var kvp in connectedPlayers)
            {
                ulong existingClientId = kvp.Key;
                LobbyPlayerData data = kvp.Value;
                
                Debug.Log($"[Lobby] Informing all clients about existing player {data.nickname} (client {existingClientId})");
                AddPlayerClientRpc(existingClientId, data.nickname, data.color, data.slotIndex);
            }
        }
        
        UpdatePlayerCount();
    }
    
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[Lobby] Client disconnected: {clientId}");
        
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            OnPlayerLeaveRequested(clientId);
        }
        
        UpdatePlayerCount();
    }
    
    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void RequestJoinServerRpc(ulong clientId, string nickname, Color color)
    {
        Debug.Log($"[Lobby] Server received join request from client {clientId}, nickname: {nickname}");
        OnPlayerJoinRequested(clientId, nickname, color);
    }
    
    [Rpc(SendTo.NotServer)]
    public void AddPlayerClientRpc(ulong clientId, string nickname, Color color, int slotIndex)
    {
        Debug.Log($"[Lobby] Client received add player: {nickname} at slot {slotIndex} (LocalClientId: {NetworkManager.Singleton.LocalClientId})");
        AddPlayerToSlot(clientId, nickname, color, slotIndex);
    }
    
    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void RemovePlayerServerRpc(ulong clientId)
    {
        Debug.Log($"[Lobby] Server received remove request for client {clientId}");
        OnPlayerLeaveRequested(clientId);
    }
    
    [Rpc(SendTo.NotServer)]
    public void RemovePlayerClientRpc(ulong clientId, int slotIndex)
    {
        Debug.Log($"[Lobby] Client received remove player at slot {slotIndex}");
        RemovePlayerFromSlot(clientId, slotIndex);
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
        
        if (IsServer)
        {
            AddPlayerToSlot(clientId, nickname, color, slotIndex);
        }
        
        AddPlayerClientRpc(clientId, nickname, color, slotIndex);
    }
    
    public void OnPlayerLeaveRequested(ulong clientId)
    {
        if (connectedPlayers.ContainsKey(clientId))
        {
            int slotIndex = connectedPlayers[clientId].slotIndex;
            connectedPlayers.Remove(clientId);
            
            if (IsServer)
            {
                RemovePlayerFromSlot(clientId, slotIndex);
            }
            
            RemovePlayerClientRpc(clientId, slotIndex);
        }
    }
    
    public void AddPlayerToSlot(ulong clientId, string nickname, Color color, int slotIndex)
    {
        Debug.Log($"[Lobby] AddPlayerToSlot called - ClientId: {clientId}, Nickname: {nickname}, Slot: {slotIndex}");
        
        if (slotIndex < 0 || slotIndex >= MAX_PLAYERS)
        {
            Debug.LogError($"[Lobby] Invalid slot index: {slotIndex}");
            return;
        }
        
        if (playerPreviews.ContainsKey(clientId))
        {
            Debug.Log($"[Lobby] Player {clientId} already has a preview, skipping duplicate add");
            return;
        }
        
        if (nicknameTexts[slotIndex] != null)
        {
            nicknameTexts[slotIndex].text = nickname;
            Debug.Log($"[Lobby] Set nickname text for slot {slotIndex} to: {nickname}");
        }
        
        if (playerSlots[slotIndex] != null && characterPreviewPrefab != null)
        {
            ClearSlot(slotIndex);
            
            float worldXPosition = -1.5f + (slotIndex * 1f);
            Vector3 worldPosition = new Vector3(worldXPosition, 0f, -5f);
            
            GameObject preview = Instantiate(characterPreviewPrefab);
            preview.transform.position = worldPosition;
            preview.transform.rotation = Quaternion.Euler(0, 180, 0);
            preview.transform.localScale = Vector3.one * 0.7f;
            
            Debug.Log($"[Lobby] Instantiated character preview for slot {slotIndex} at world position {worldPosition}");
            
            CharacterPreview characterPreview = preview.GetComponent<CharacterPreview>();
            if (characterPreview != null)
            {
                characterPreview.SetColor(color);
                Debug.Log($"[Lobby] Set character color to: {color}");
            }
            else
            {
                Debug.LogWarning("[Lobby] CharacterPreview component not found on instantiated prefab!");
            }
            
            playerPreviews[clientId] = preview;
        }
        else
        {
            if (playerSlots[slotIndex] == null)
                Debug.LogError($"[Lobby] Player slot {slotIndex} is null!");
            if (characterPreviewPrefab == null)
                Debug.LogError("[Lobby] Character preview prefab is null!");
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
