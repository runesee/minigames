using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FocusFlowGame : MonoBehaviour
{
    [Header("Button References")]
    [SerializeField] private ButtonCircle buttonA;
    [SerializeField] private ButtonCircle buttonB;
    [SerializeField] private ButtonCircle buttonX;
    [SerializeField] private ButtonCircle buttonY;

    [Header("Game Settings")]
    [SerializeField] private float delayBetweenButtons = 0.5f;
    [SerializeField] private float delayAfterSequence = 0.5f;

    private List<ButtonCircle.ButtonType> currentSequence = new List<ButtonCircle.ButtonType>();
    private int playerInputIndex = 0;
    private bool waitingForInput = false;
    private bool isShowingSequence = false;

    private Dictionary<ButtonCircle.ButtonType, ButtonCircle> buttonMap;

    private void Awake()
    {
        buttonMap = new Dictionary<ButtonCircle.ButtonType, ButtonCircle>
        {
            { ButtonCircle.ButtonType.A, buttonA },
            { ButtonCircle.ButtonType.B, buttonB },
            { ButtonCircle.ButtonType.X, buttonX },
            { ButtonCircle.ButtonType.Y, buttonY }
        };
    }

    private void Start()
    {
        StartNewGame();
    }

    private void Update()
    {
        if (waitingForInput && !isShowingSequence)
        {
            CheckPlayerInput();
        }
    }

    private void StartNewGame()
    {
        currentSequence.Clear();
        playerInputIndex = 0;
        waitingForInput = false;
        StartCoroutine(StartNextRound());
    }

    private IEnumerator StartNextRound()
    {
        waitingForInput = false;
        playerInputIndex = 0;

        ButtonCircle.ButtonType randomButton = (ButtonCircle.ButtonType)Random.Range(0, 4);
        currentSequence.Add(randomButton);

        yield return new WaitForSeconds(delayAfterSequence);

        yield return StartCoroutine(ShowSequence());

        waitingForInput = true;
    }

    private IEnumerator ShowSequence()
    {
        isShowingSequence = true;

        foreach (ButtonCircle.ButtonType buttonType in currentSequence)
        {
            ButtonCircle button = buttonMap[buttonType];
            button.LightUp();

            yield return new WaitForSeconds(1.0f);

            while (button.IsLit)
            {
                yield return null;
            }

            yield return new WaitForSeconds(delayBetweenButtons);
        }

        isShowingSequence = false;
    }

    private void CheckPlayerInput()
    {
        ButtonCircle.ButtonType? pressedButton = null;

        if (KeyboardInputAdapter.GetButtonDownForTesting(PlayPulse.Input.Input.Button.A))
        {
            pressedButton = ButtonCircle.ButtonType.A;
        }
        else if (KeyboardInputAdapter.GetButtonDownForTesting(PlayPulse.Input.Input.Button.B))
        {
            pressedButton = ButtonCircle.ButtonType.B;
        }
        else if (KeyboardInputAdapter.GetButtonDownForTesting(PlayPulse.Input.Input.Button.X))
        {
            pressedButton = ButtonCircle.ButtonType.X;
        }
        else if (KeyboardInputAdapter.GetButtonDownForTesting(PlayPulse.Input.Input.Button.Y))
        {
            pressedButton = ButtonCircle.ButtonType.Y;
        }

        if (pressedButton.HasValue)
        {
            ProcessInput(pressedButton.Value);
        }
    }

    private void ProcessInput(ButtonCircle.ButtonType inputButton)
    {
        if (playerInputIndex >= currentSequence.Count)
        {
            return;
        }

        if (inputButton == currentSequence[playerInputIndex])
        {
            playerInputIndex++;

            if (playerInputIndex >= currentSequence.Count)
            {
                OnSequenceComplete();
            }
        }
        else
        {
            OnPlayerFailed();
        }
    }

    private void OnSequenceComplete()
    {
        waitingForInput = false;
        StartCoroutine(StartNextRound());
    }

    private void OnPlayerFailed()
    {
        waitingForInput = false;
        StartNewGame();
    }
}
