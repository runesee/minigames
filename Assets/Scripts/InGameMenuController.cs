using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InGameMenuController : MonoBehaviour
{
    [Header("Menu Settings")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button firstButton;
    [SerializeField] private Key menuKey = Key.M;

    private bool isMenuOpen = false;

    private void Update()
    {
        if (Keyboard.current[menuKey].wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        menuPanel.SetActive(isMenuOpen);
        
        if (isMenuOpen && firstButton != null)
        {
            EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
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
