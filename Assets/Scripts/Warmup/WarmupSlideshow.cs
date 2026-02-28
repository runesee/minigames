using UnityEngine;
using UnityEngine.InputSystem;

public class WarmupSlideshow : MonoBehaviour
{
    [Header("Slides")]
    [Tooltip("Assign all slide root GameObjects in display order.")]
    [SerializeField] private GameObject[] slides;

    private int currentIndex = 0;

    private bool leftBikeWasPressed = false;
    private bool rightBikeWasPressed = false;

    private void Start()
    {
        ShowSlide(0);
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        bool rightTriggered = IsRightTriggered();
        bool leftTriggered = IsLeftTriggered();

        if (rightTriggered)
        {
            NavigateNext();
        }
        else if (leftTriggered)
        {
            NavigatePrevious();
        }

        rightBikeWasPressed = PlayPulse.Input.Input.GetButton(PlayPulse.Input.Input.Button.RightTrigger);
        leftBikeWasPressed = PlayPulse.Input.Input.GetButton(PlayPulse.Input.Input.Button.LeftTrigger);
    }

    private bool IsRightTriggered()
    {
        bool keyboardRight = Keyboard.current != null && Keyboard.current.rightArrowKey.wasPressedThisFrame;
        bool bikeRight = PlayPulse.Input.Input.GetButton(PlayPulse.Input.Input.Button.RightTrigger) && !rightBikeWasPressed;
        return keyboardRight || bikeRight;
    }

    private bool IsLeftTriggered()
    {
        bool keyboardLeft = Keyboard.current != null && Keyboard.current.leftArrowKey.wasPressedThisFrame;
        bool bikeLeft = PlayPulse.Input.Input.GetButton(PlayPulse.Input.Input.Button.LeftTrigger) && !leftBikeWasPressed;
        return keyboardLeft || bikeLeft;
    }

    public void NavigateNext()
    {
        if (currentIndex < slides.Length - 1)
        {
            ShowSlide(currentIndex + 1);
        }
    }

    public void NavigatePrevious()
    {
        if (currentIndex > 0)
        {
            ShowSlide(currentIndex - 1);
        }
    }

    private void ShowSlide(int index)
    {
        if (slides == null || slides.Length == 0)
        {
            Debug.LogWarning("[WarmupSlideshow] No slides assigned.");
            return;
        }

        for (int i = 0; i < slides.Length; i++)
        {
            if (slides[i] != null)
            {
                slides[i].SetActive(i == index);
            }
        }

        currentIndex = index;
    }
}
