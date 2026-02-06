using UnityEngine;

public class KeyboardInputAdapter : MonoBehaviour
{
    public static bool GetButtonDownForTesting(PlayPulse.Input.Input.Button button)
    {
        bool keyboardInput = false;

        switch (button)
        {
            case PlayPulse.Input.Input.Button.Y:
                keyboardInput = Input.GetKeyDown(KeyCode.UpArrow);
                break;
            case PlayPulse.Input.Input.Button.A:
                keyboardInput = Input.GetKeyDown(KeyCode.DownArrow);
                break;
            case PlayPulse.Input.Input.Button.B:
                keyboardInput = Input.GetKeyDown(KeyCode.RightArrow);
                break;
            case PlayPulse.Input.Input.Button.X:
                keyboardInput = Input.GetKeyDown(KeyCode.LeftArrow);
                break;
        }

        return PlayPulse.Input.Input.GetButtonDown(button) || keyboardInput;
    }
}
