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

    [Header("Feedback Settings")]
    [SerializeField] private Transform mainCircle;

    [Header("Game Settings")]
    [SerializeField] private float delayBetweenButtons = 0.5f;
    [SerializeField] private float delayAfterSequence = 2.0f;

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
            button.ShowRing();

            yield return new WaitForSeconds(1.0f);

            while (button.IsShowingRing)
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

        ButtonCircle button = buttonMap[inputButton];
        button.LightUp();

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
        ShowFeedback("Great!", Color.green);
        StartCoroutine(StartNextRound());
    }

    private void OnPlayerFailed()
    {
        waitingForInput = false;
        ShowFeedback("Failed", Color.red);
        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(2.0f);
        StartNewGame();
    }

    private void ShowFeedback(string message, Color color)
    {
        if (mainCircle == null)
        {
            return;
        }

        GameObject feedbackObject = new GameObject("Feedback");
        FeedbackText feedback = feedbackObject.AddComponent<FeedbackText>();
        feedback.Initialize(message, color, mainCircle.position);
    }
}
