using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private UnityEngine.UI.Button confirmButton;
    [SerializeField] private UnityEngine.UI.Text confirmButtonText;

    private bool isHostMode;
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
        if (colorPreview != null)
        {
            colorPreview.color = PlayerColorManager.GetColor(colorIndex);
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
    }

    public void OnJoinButtonClicked()
    {
        isHostMode = false;
        ShowSetupMenu();
        confirmButtonText.text = "Join";
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
        }
        else
        {
            Debug.Log($"Join confirmed with nickname: {nickname}, color index: {colorIndex}");
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
    }
}
