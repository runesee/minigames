using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InGameMenuController : MonoBehaviour
{
    [Header("Menu Settings")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Key menuKey = Key.M;

    [Header("Button References")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button settingsButton;

    [Header("Joystick Navigation")]
    [SerializeField] private float joystickDeadzone = 0.5f;
    [SerializeField] private float joystickRepeatDelay = 0.25f;

    private bool isMenuOpen = false;
    private float joystickTimer = 0f;

    private void Update()
    {
        joystickTimer -= Time.unscaledDeltaTime;

        if (Keyboard.current[menuKey].wasPressedThisFrame ||
            PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.X))
        {
            ToggleMenu();
        }

        if (!isMenuOpen)
            return;

        HandleKeyboardNavigation();
        HandleJoystickNavigation();
    }

    private void HandleKeyboardNavigation()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
        {
            NavigateUp();
        }
        else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
        {
            NavigateDown();
        }
        else if (keyboard.enterKey.wasPressedThisFrame ||
                 keyboard.spaceKey.wasPressedThisFrame ||
                 PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.A))
        {
            SubmitSelection();
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

        if (currentSelected == null)
        {
            SelectButton(continueButton);
        }
        else if (currentSelected == continueButton.gameObject)
        {
            SelectButton(settingsButton);
        }
        else if (currentSelected == settingsButton.gameObject)
        {
            SelectButton(disconnectButton);
        }
        else if (currentSelected == disconnectButton.gameObject)
        {
            SelectButton(continueButton);
        }
    }

    private void NavigateDown()
    {
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (currentSelected == null)
        {
            SelectButton(continueButton);
        }
        else if (currentSelected == continueButton.gameObject)
        {
            SelectButton(disconnectButton);
        }
        else if (currentSelected == disconnectButton.gameObject)
        {
            SelectButton(settingsButton);
        }
        else if (currentSelected == settingsButton.gameObject)
        {
            SelectButton(continueButton);
        }
    }

    private void SelectButton(Button button)
    {
        if (button != null)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }

    private void SubmitSelection()
    {
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (currentSelected == null)
            return;

        Button button = currentSelected.GetComponent<Button>();
        if (button != null && button.interactable)
        {
            button.onClick.Invoke();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        menuPanel.SetActive(isMenuOpen);

        if (isMenuOpen)
        {
            joystickTimer = 0f;
            SelectButton(continueButton);
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
   
    public void ContinueGame()
    {
        ToggleMenu();
    }

    public void DisconnectGame()
    {
    }

    public void OpenSettings()
    {
    }
}
