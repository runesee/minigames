using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Net;
using System.Net.Sockets;

public class MenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject setupMenuPanel;

    [Header("Setup Menu")]
    [SerializeField] private UnityEngine.UI.InputField nicknameInputField;
    [SerializeField] private UnityEngine.UI.Button generateNicknameButton;
    [SerializeField] private UnityEngine.UI.Dropdown colorDropdown;
    [SerializeField] private UnityEngine.UI.Image colorPreview;
    [SerializeField] private CharacterPreview characterPreview;
    [SerializeField] private UnityEngine.UI.Button confirmButton;
    [SerializeField] private UnityEngine.UI.Text confirmButtonText;
    [SerializeField] private GameObject ipLabel;
    [SerializeField] private UnityEngine.UI.InputField ipInputField;

    private bool isHostMode;
    private const ushort PORT = 7777;
    private static Sprite whiteSprite;

    private static readonly string[] randomNicknames = new string[]
    {
        "SpeedyPlayer", "CoolRunner", "QuickFox", "NinjaGamer", "ProPlayer",
        "MasterChief", "SwiftHawk", "ThunderBolt", "ShadowWalker", "IceBreaker",
        "FireStarter", "StormChaser", "WildCard", "StarGazer", "NightOwl"
    };

    private void Awake()
    {
        CreateWhiteSprite();
        FixImageSprites();
    }

    private void Start()
    {
        ShowMainMenu();
        colorDropdown.onValueChanged.AddListener(OnColorChanged);
        OnColorChanged(colorDropdown.value);
    }

    private void OnDestroy()
    {
        if (colorDropdown != null)
        {
            colorDropdown.onValueChanged.RemoveListener(OnColorChanged);
        }
    }

    private void OnColorChanged(int colorIndex)
    {
        Color selectedColor = PlayerColorManager.GetColor(colorIndex);
        
        if (colorPreview != null)
        {
            colorPreview.color = selectedColor;
        }
        
        if (characterPreview != null)
        {
            characterPreview.SetColor(selectedColor);
        }
    }

    private void CreateWhiteSprite()
    {
        if (whiteSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    private void FixImageSprites()
    {
        Image[] allImages = GetComponentsInChildren<Image>(true);
        foreach (Image img in allImages)
        {
            if (img.sprite == null)
            {
                img.sprite = whiteSprite;
            }
        }
    }

    public void OnHostButtonClicked()
    {
        isHostMode = true;
        ShowSetupMenu();
        confirmButtonText.text = "Host";
        if (ipLabel != null) ipLabel.SetActive(true);
        if (ipInputField != null)
        {
            ipInputField.gameObject.SetActive(true);
            ipInputField.text = GetLocalIPAddress();
            ipInputField.interactable = false;
        }
    }

    public void OnJoinButtonClicked()
    {
        isHostMode = false;
        ShowSetupMenu();
        confirmButtonText.text = "Join";
        if (ipLabel != null) ipLabel.SetActive(true);
        if (ipInputField != null)
        {
            ipInputField.gameObject.SetActive(true);
            if (string.IsNullOrEmpty(ipInputField.text))
            {
                ipInputField.text = "127.0.0.1";
            }
            ipInputField.interactable = true;
        }
    }

    public void OnSettingsButtonClicked()
    {
        Debug.Log("Settings button clicked - Placeholder");
    }

    public void OnQuitButtonClicked()
    {
        Debug.Log("Quit button clicked - Placeholder");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnGenerateNicknameButtonClicked()
    {
        string randomNickname = randomNicknames[Random.Range(0, randomNicknames.Length)];
        nicknameInputField.text = randomNickname;
    }

    public void OnConfirmButtonClicked()
    {
        string nickname = nicknameInputField.text;
        int colorIndex = colorDropdown.value;

        if (string.IsNullOrWhiteSpace(nickname))
        {
            Debug.LogWarning("Please enter a nickname!");
            return;
        }

        if (isHostMode)
        {
            Debug.Log($"Host confirmed with nickname: {nickname}, color index: {colorIndex}");
            StartHost();
        }
        else
        {
            if (ipInputField != null && string.IsNullOrWhiteSpace(ipInputField.text))
            {
                Debug.LogWarning("Please enter a host IP address!");
                return;
            }
            Debug.Log($"Join confirmed with nickname: {nickname}, color index: {colorIndex}, IP: {ipInputField.text}");
            StartClient();
        }
    }

    public void OnBackButtonClicked()
    {
        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        setupMenuPanel.SetActive(false);
    }

    private void ShowSetupMenu()
    {
        mainMenuPanel.SetActive(false);
        setupMenuPanel.SetActive(true);
        
        if (string.IsNullOrWhiteSpace(nicknameInputField.text))
        {
            OnGenerateNicknameButtonClicked();
        }
        
        if (ipLabel != null) ipLabel.SetActive(false);
        if (ipInputField != null) ipInputField.gameObject.SetActive(false);
    }

    private void StartHost()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager not found in scene!");
            return;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData(GetLocalIPAddress(), PORT);
        }

        NetworkManager.Singleton.StartHost();
        LoadGameScene();
    }

    private void StartClient()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager not found in scene!");
            return;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            string ipAddress = ipInputField != null ? ipInputField.text : "127.0.0.1";
            transport.SetConnectionData(ipAddress, PORT);
        }

        NetworkManager.Singleton.StartClient();
        LoadGameScene();
    }

    private void LoadGameScene()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("TagScene", LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene("TagScene");
        }
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
