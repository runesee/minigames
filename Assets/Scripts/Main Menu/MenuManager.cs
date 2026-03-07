using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static MinigameManager;

public class MenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject setupMenuPanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private UnityEngine.UI.Button hostButton;
    [SerializeField] private UnityEngine.UI.Button joinButton;
    [SerializeField] private UnityEngine.UI.Button settingsButton;
    [SerializeField] private UnityEngine.UI.Button warmupButton;
    [SerializeField] private UnityEngine.UI.Button quitButton;

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
    [SerializeField] private UnityEngine.UI.Button backButton;
    [SerializeField] private UnityEngine.UI.Text feedbackText;

    [Header("Joystick Navigation")]
    [SerializeField] private float joystickDeadzone = 0.5f;
    [SerializeField] private float joystickRepeatDelay = 0.25f;

    [Header("Audio Setup")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip plinkAudio;
    [SerializeField] private AudioClip plonkAudio;


    private bool isHostMode;
    private const ushort PORT = 7777;
    private static Sprite whiteSprite;
    private float joystickTimer = 0f;
    [SerializeField] private Selectable[] mainMenuOrder;
    [SerializeField] private Selectable[] setupMenuOrder;


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
        SelectButton(hostButton);
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

    private void Update()
    {
        joystickTimer -= Time.unscaledDeltaTime;
        
        bool dropdownOpen = HandleDropdownNavigation();
        
        if (!dropdownOpen)
        {
            HandleJoystickNavigation();
            if (PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.A)) {
                SubmitSelection();
            }
        }
        else {
            if (PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.A)) {
                GameObject currentItem = EventSystem.current.currentSelectedGameObject;
                if (currentItem != null) {
                    if (currentItem.TryGetComponent<Toggle>(out var toggle)) {
                        toggle.isOn = true;
                    }
                }
            }
        }
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

        ClearFeedback();
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
        audioSource?.PlayOneShot(plinkAudio);
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
        audioSource?.PlayOneShot(plinkAudio);
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
        audioSource?.PlayOneShot(plinkAudio);
    }

    public void OnWarmupButtonClicked()
    {
        audioSource?.PlayOneShot(plinkAudio);
        SceneManager.LoadScene("Warmup", LoadSceneMode.Single);
    }

    public void OnQuitButtonClicked()
    {
        audioSource?.PlayOneShot(plinkAudio);
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void OnGenerateNicknameButtonClicked()
    {
        audioSource?.PlayOneShot(plinkAudio);
        string randomNickname = randomNicknames[Random.Range(0, randomNicknames.Length)];
        nicknameInputField.text = randomNickname;
    }

    public void OnConfirmButtonClicked()
    {
        audioSource?.PlayOneShot(plinkAudio);
        string nickname = nicknameInputField.text;
        if (string.IsNullOrWhiteSpace(nickname)) return;

        var data = LocalPlayerStorage.Load() ?? new LocalPlayerData();
        var newData = new LocalPlayerData
        {
            guid = data.guid,
            nickname = nickname,
            color = "#" + ColorUtility.ToHtmlStringRGB(PlayerColorManager.GetColor(colorDropdown.value))
        };
        LocalPlayerStorage.Save(newData);

        if (ipInputField != null && string.IsNullOrWhiteSpace(ipInputField.text)) return;

        string colorHex = ColorUtility.ToHtmlStringRGB(PlayerColorManager.GetColor(colorDropdown.value));
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(colorHex);

        if (!isHostMode)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientConnectionDenied;
        }

        string ipAddress = isHostMode ? GetLocalIPAddress() : ipInputField != null ? ipInputField.text : "127.0.0.1";
        MinigameManager.Instance.StartConnection(ipAddress, PORT, isHostMode);
    }

    private void OnClientConnectionDenied(ulong clientId)
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientConnectionDenied;

        string reason = NetworkManager.Singleton.DisconnectReason;
        if (!string.IsNullOrEmpty(reason))
        {
            ShowFeedback(reason);
        }
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.gameObject.SetActive(true);
        }
    }

    private void ClearFeedback()
    {
        if (feedbackText != null)
        {
            feedbackText.text = string.Empty;
            feedbackText.gameObject.SetActive(false);
        }
    }

    public void OnBackButtonClicked()
    {
        audioSource?.PlayOneShot(plinkAudio);
        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        setupMenuPanel.SetActive(false);
        joystickTimer = 0f;
        SelectButton(hostButton);
    }

    private void ShowSetupMenu()
    {
        mainMenuPanel.SetActive(false);
        setupMenuPanel.SetActive(true);
        if (string.IsNullOrWhiteSpace(nicknameInputField.text)) OnGenerateNicknameButtonClicked();
        
        ipLabel?.SetActive(false);
        ipInputField?.gameObject.SetActive(false);
        joystickTimer = 0f;
        ClearFeedback();
        SelectSelectable(nicknameInputField);
    }

    private void StartHost()
    {
        if (NetworkManager.Singleton == null) return;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            string localIP = GetLocalIPAddress();
            transport.SetConnectionData(localIP, PORT);
        }

        bool started = NetworkManager.Singleton.StartHost();
        if (started)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("Lobby", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("[Host] Failed to start!");
        }
    }

    private void StartClient()
    {
        if (NetworkManager.Singleton == null) return;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            string ipAddress = ipInputField != null ? ipInputField.text : "127.0.0.1";
            transport.SetConnectionData(ipAddress, PORT);
        }

        bool started = NetworkManager.Singleton.StartClient();
        if (!started)
        {
            Debug.LogError("[Client] Failed to start connection!");
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

    private bool HandleDropdownNavigation()
    {
        if (colorDropdown == null) return false;
            
        Transform dropdownList = colorDropdown.transform.Find("Dropdown List");
        bool dropdownOpen = dropdownList != null;
        
        if (dropdownOpen)
        {    
            float y = PlayPulse.Input.Input.JoystickY;
            Keyboard keyboard = Keyboard.current;
            
            if (joystickTimer <= 0f)
            {
                if (y < -joystickDeadzone)
                {
                    GameObject currentItem = EventSystem.current.currentSelectedGameObject;
                    if (currentItem != null)
                    {
                        Selectable selectable = currentItem.GetComponent<Selectable>();
                        if (selectable != null)
                        {
                            Selectable nextSelectable = selectable.FindSelectableOnUp();
                            if (nextSelectable != null)
                            {
                                EventSystem.current.SetSelectedGameObject(nextSelectable.gameObject);
                            }
                        }
                    }
                    joystickTimer = joystickRepeatDelay;
                }
                else if (y > joystickDeadzone)
                {
                    GameObject currentItem = EventSystem.current.currentSelectedGameObject;
                    if (currentItem != null)
                    {
                        Selectable selectable = currentItem.GetComponent<Selectable>();
                        if (selectable != null)
                        {
                            Selectable nextSelectable = selectable.FindSelectableOnDown();
                            if (nextSelectable != null)
                            {
                                EventSystem.current.SetSelectedGameObject(nextSelectable.gameObject);
                            }
                        }
                    }
                    joystickTimer = joystickRepeatDelay;
                }
            }
            
            if (PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.A) ||
                (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)))
            {
                GameObject currentItem = EventSystem.current.currentSelectedGameObject;
                if (currentItem != null)
                {
                    Toggle toggle = currentItem.GetComponent<Toggle>();
                    if (toggle != null)
                    {
                        toggle.isOn = true;
                    }
                }
            }
        }
        return dropdownOpen;
    }

    private void HandleJoystickNavigation()
    {
        if (joystickTimer > 0f) return;

        float y = PlayPulse.Input.Input.JoystickY;
        bool down = y > joystickDeadzone;
        bool up = y < -joystickDeadzone;
        int direction = down ? 1 : up ? -1 : 0;

        if (direction == 0) return;
        joystickTimer = joystickRepeatDelay;
        if (mainMenuPanel.activeSelf)
        {
            Navigate(mainMenuOrder, direction);
        }
        else
            Navigate(setupMenuOrder, direction);
    }

    private void Navigate(Selectable[] order, int direction)
    {
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        int index = System.Array.FindIndex(order, s => s.gameObject == currentSelected);

        if (index < 0) index = 0;
        else index = (index + direction + order.Length) % order.Length;
        EventSystem.current.SetSelectedGameObject(order[index].gameObject);
    }

    private void SelectButton(Button button)
    {
        if (button != null)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }

    private void SelectSelectable(Selectable selectable)
    {
        if (selectable != null)
        {
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
    }

    private void SubmitSelection()
    {
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (currentSelected == null) return;

        Dropdown dropdown = currentSelected.GetComponent<Dropdown>();
        if (dropdown != null && dropdown.interactable)
        {
            dropdown.Show();
            return;
        }

        Button button = currentSelected.GetComponent<Button>();
        if (button != null && button.interactable)
        {
            button.onClick.Invoke();
            return;
        }

        InputField inputField = currentSelected.GetComponent<InputField>();
        if (inputField != null)
        {

            if (mainMenuPanel.activeSelf)  Navigate(mainMenuOrder, -1);
            else Navigate(setupMenuOrder, -1);
        }
    }
}
