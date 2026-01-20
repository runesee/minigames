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

    private bool isMenuOpen = false;

    private void Update()
    {
        if (Keyboard.current[menuKey].wasPressedThisFrame)
        {
            ToggleMenu();
        }

        if (isMenuOpen)
        {
            HandleKeyboardNavigation();
        }
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
        else if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
        {
            SubmitSelection();
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
        else if (currentSelected == disconnectButton.gameObject)
        {
            SelectButton(continueButton);
        }
        else if (currentSelected == settingsButton.gameObject)
        {
            SelectButton(disconnectButton);
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
        
        if (currentSelected != null)
        {
            Button button = currentSelected.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
            }
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        menuPanel.SetActive(isMenuOpen);

        if (isMenuOpen)
        {
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
