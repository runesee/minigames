using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Net;
using System.Net.Sockets;

public class MenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject setupMenuPanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private UnityEngine.UI.Button hostButton;
    [SerializeField] private UnityEngine.UI.Button joinButton;
    [SerializeField] private UnityEngine.UI.Button settingsButton;
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

    [Header("Joystick Navigation")]
    [SerializeField] private float joystickDeadzone = 0.5f;
    [SerializeField] private float joystickRepeatDelay = 0.25f;

    private bool isHostMode;
    private const ushort PORT = 7777;
    private static Sprite whiteSprite;
    private float joystickTimer = 0f;

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
        );}
        }

    private void Update()
    {
        joystickTimer -= Time.unscaledDeltaTime;
        
        HandleDropdownNavigation();
        HandleKeyboardNavigation();
        HandleJoystickNavigation();
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
        PlayerPrefs.SetString("Color", "#" + ColorUtility.ToHtmlStringRGB(PlayerColorManager.GetColor(colorIndex)));

        if (string.IsNullOrWhiteSpace(nickname))
        {
            Debug.LogWarning("Please enter a nickname!");
            return;
        }
        PlayerPrefs.SetString("Username", nickname);

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
        joystickTimer = 0f;
        SelectButton(hostButton);
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
        
        joystickTimer = 0f;
        SelectSelectable(nicknameInputField);
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
            string localIP = GetLocalIPAddress();
            Debug.Log($"[Host] Setting connection data - IP: {localIP}, Port: {PORT}");
            transport.SetConnectionData(localIP, PORT);
        }

        bool started = NetworkManager.Singleton.StartHost();
        if (started)
        {
            Debug.Log("[Host] Started successfully, loading TagScene...");
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
            NetworkManager.Singleton.SceneManager.LoadScene("TagScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("[Host] Failed to start!");
        }
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
            Debug.Log($"[Client] Setting connection data - IP: {ipAddress}, Port: {PORT}");
            transport.SetConnectionData(ipAddress, PORT);
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        bool started = NetworkManager.Singleton.StartClient();
        if (started)
        {
            Debug.Log("[Client] Connection attempt started...");
        }
        else
        {
            Debug.LogError("[Client] Failed to start connection!");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[Client] Connected with ID: {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.LogWarning($"[Client] Disconnected with ID: {clientId}");
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnSceneLoadCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
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

    private void HandleDropdownNavigation()
    {
        bool dropdownOpen = colorDropdown != null && 
                           colorDropdown.transform.Find("Dropdown List") != null;
        
        if (dropdownOpen)
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.sendNavigationEvents = true;
            }
            
            float y = PlayPulse.Input.Input.JoystickY;
            
            if (joystickTimer <= 0f)
            {
                if (y < -joystickDeadzone)
                {
                    AxisEventData axisData = new AxisEventData(EventSystem.current);
                    axisData.moveDir = MoveDirection.Up;
                    ExecuteEvents.Execute(colorDropdown.gameObject, axisData, ExecuteEvents.moveHandler);
                    joystickTimer = joystickRepeatDelay;
                }
                else if (y > joystickDeadzone)
                {
                    AxisEventData axisData = new AxisEventData(EventSystem.current);
                    axisData.moveDir = MoveDirection.Down;
                    ExecuteEvents.Execute(colorDropdown.gameObject, axisData, ExecuteEvents.moveHandler);
                    joystickTimer = joystickRepeatDelay;
                }
            }
            
            if (PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.A))
            {
                BaseEventData eventData = new BaseEventData(EventSystem.current);
                ExecuteEvents.Execute(colorDropdown.gameObject, eventData, ExecuteEvents.submitHandler);
            }
        }
        else if (EventSystem.current != null)
        {
            EventSystem.current.sendNavigationEvents = false;
        }
    }

    private void HandleKeyboardNavigation()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        InputField activeInputField = currentSelected != null ? currentSelected.GetComponent<InputField>() : null;
        bool isTypingInInputField = activeInputField != null && activeInputField.isFocused;

        if (keyboard.enterKey.wasPressedThisFrame ||
                 keyboard.spaceKey.wasPressedThisFrame ||
                 PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.A))
        {
            SubmitSelection();
        }
        else if (keyboard.upArrowKey.wasPressedThisFrame || (!isTypingInInputField && keyboard.wKey.wasPressedThisFrame))
        {
            NavigateUp();
        }
        else if (keyboard.downArrowKey.wasPressedThisFrame || (!isTypingInInputField && keyboard.sKey.wasPressedThisFrame))
        {
            NavigateDown();
        }
        else if (keyboard.escapeKey.wasPressedThisFrame && setupMenuPanel.activeSelf)
        {
            OnBackButtonClicked();
        }
    }

    private void HandleJoystickNavigation()
    {
        if (joystickTimer > 0f)
            return;

        float y = PlayPulse.Input.Input.JoystickY;

        if (y < -joystickDeadzone)
        {
            NavigateUp();
            joystickTimer = joystickRepeatDelay;
        }
        else if (y > joystickDeadzone)
        {
            NavigateDown();
            joystickTimer = joystickRepeatDelay;
        }
    }

    private void NavigateUp()
    {
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (mainMenuPanel.activeSelf)
        {
            if (currentSelected == null)
            {
                SelectButton(hostButton);
            }
            else if (currentSelected == hostButton.gameObject)
            {
                SelectButton(quitButton);
            }
            else if (currentSelected == joinButton.gameObject)
            {
                SelectButton(hostButton);
            }
            else if (currentSelected == settingsButton.gameObject)
            {
                SelectButton(joinButton);
            }
            else if (currentSelected == quitButton.gameObject)
            {
                SelectButton(settingsButton);
            }
        }
        else if (setupMenuPanel.activeSelf)
        {
            if (currentSelected == null)
            {
                SelectSelectable(nicknameInputField);
            }
            else if (currentSelected == nicknameInputField.gameObject)
            {
                SelectButton(backButton);
            }
            else if (currentSelected == backButton.gameObject)
            {
                SelectButton(confirmButton);
            }
            else if (currentSelected == confirmButton.gameObject)
            {
                if (ipInputField != null && ipInputField.gameObject.activeSelf)
                {
                    SelectSelectable(ipInputField);
                }
                else
                {
                    SelectSelectable(colorDropdown);
                }
            }
            else if (currentSelected == ipInputField.gameObject)
            {
                SelectSelectable(colorDropdown);
            }
            else if (currentSelected == colorDropdown.gameObject)
            {
                SelectButton(generateNicknameButton);
            }
            else if (currentSelected == generateNicknameButton.gameObject)
            {
                SelectSelectable(nicknameInputField);
            }
        }
    }

    private void NavigateDown()
    {
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (mainMenuPanel.activeSelf)
        {
            if (currentSelected == null)
            {
                SelectButton(hostButton);
            }
            else if (currentSelected == hostButton.gameObject)
            {
                SelectButton(joinButton);
            }
            else if (currentSelected == joinButton.gameObject)
            {
                SelectButton(settingsButton);
            }
            else if (currentSelected == settingsButton.gameObject)
            {
                SelectButton(quitButton);
            }
            else if (currentSelected == quitButton.gameObject)
            {
                SelectButton(hostButton);
            }
        }
        else if (setupMenuPanel.activeSelf)
        {
            if (currentSelected == null)
            {
                SelectSelectable(nicknameInputField);
            }
            else if (currentSelected == nicknameInputField.gameObject)
            {
                SelectButton(generateNicknameButton);
            }
            else if (currentSelected == generateNicknameButton.gameObject)
            {
                SelectSelectable(colorDropdown);
            }
            else if (currentSelected == colorDropdown.gameObject)
            {
                if (ipInputField != null && ipInputField.gameObject.activeSelf)
                {
                    SelectSelectable(ipInputField);
                }
                else
                {
                    SelectButton(confirmButton);
                }
            }
            else if (currentSelected == ipInputField.gameObject)
            {
                SelectButton(confirmButton);
            }
            else if (currentSelected == confirmButton.gameObject)
            {
                SelectButton(backButton);
            }
            else if (currentSelected == backButton.gameObject)
            {
                SelectSelectable(nicknameInputField);
            }
        }
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

        if (currentSelected == null)
            return;

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
            NavigateDown();
        }
    }
}
