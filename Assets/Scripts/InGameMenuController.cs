using UnityEngine;
using UnityEngine.InputSystem;

public class InGameMenuController : MonoBehaviour
{
    [Header("Menu Settings")]
    [SerializeField] private GameObject menuPanel;
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
