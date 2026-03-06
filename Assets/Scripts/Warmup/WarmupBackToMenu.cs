using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class WarmupBackToMenu : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";

    private void Update()
    {
        bool keyboardBack = Keyboard.current != null && Keyboard.current[Key.Q].wasPressedThisFrame;
        bool bikeBack = PlayPulse.Input.Input.GetButton(PlayPulse.Input.Input.Button.X);

        if (keyboardBack || bikeBack)
        {
            GoBackToMainMenu();
        }
    }

    private void GoBackToMainMenu()
    {
        SceneManager.LoadScene(MainMenuSceneName, LoadSceneMode.Single);
    }
}
