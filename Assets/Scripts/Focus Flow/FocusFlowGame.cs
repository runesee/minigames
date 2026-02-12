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

    [Header("Score Settings")]
    [SerializeField] private Transform scoreTextPosition;
    [SerializeField] private ScoreMultiplierManager multiplierManager;

    [Header("Game Settings")]
    [SerializeField] private float delayBetweenButtons = 0.5f;
    [SerializeField] private float delayAfterSequence = 2.0f;

    private List<ButtonCircle.ButtonType> currentSequence = new List<ButtonCircle.ButtonType>();
    private int playerInputIndex = 0;
    private bool waitingForInput = false;
    private bool isShowingSequence = false;

    private int totalScore = 0;
    private float currentSequencePoints = 100f;
    private TextMesh scoreTextMesh;
    private BonusScoreDisplay bonusScoreDisplay;
    private Transform bonusScoreTransform;

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

        CreateScoreDisplay();
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
        totalScore = 0;
        currentSequencePoints = 100f;
        UpdateScoreDisplay();
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
        multiplierManager.StartTracking();

        for (int i = 0; i < currentSequence.Count; i++)
        {
            ButtonCircle.ButtonType buttonType = currentSequence[i];
            ButtonCircle button = buttonMap[buttonType];
            button.ShowRing();

            yield return new WaitForSeconds(1.0f);

            while (button.IsShowingRing)
            {
                yield return null;
            }

            if (i < currentSequence.Count - 1)
            {
                yield return new WaitForSeconds(delayBetweenButtons);
            }
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
        multiplierManager.StopTracking();

        int finalPoints = multiplierManager.ApplyAverageMultiplier(currentSequencePoints);

        int pointsEarned = Mathf.RoundToInt(currentSequencePoints);
        totalScore += pointsEarned;

        if (bonusScoreDisplay != null)
        {
            bonusScoreDisplay.ShowBonus(pointsEarned);
        }

        currentSequencePoints *= 2f;
        UpdateScoreDisplay();

        ShowFeedback("Great!", Color.green);
        StartCoroutine(StartNextRound());
    }

    private void OnPlayerFailed()
    {
        waitingForInput = false;
        multiplierManager.StopTracking();
        currentSequencePoints = 100f;
        ShowFeedback("Failed", Color.red);
        StartCoroutine(RestartSequenceAfterDelay());
    }

    private IEnumerator RestartSequenceAfterDelay()
    {
        yield return new WaitForSeconds(2.0f);
        RestartSequence();
    }

    private void RestartSequence()
    {
        currentSequence.Clear();
        playerInputIndex = 0;
        waitingForInput = false;
        StartCoroutine(StartNextRound());
    }

    private void ShowFeedback(string message, Color color)
    {
        GameObject feedbackObject = new GameObject("Feedback");
        FeedbackText feedback = feedbackObject.AddComponent<FeedbackText>();
        feedback.Initialize(message, color, mainCircle.position);
    }

    private void CreateScoreDisplay()
    {
        GameObject scoreObject = new GameObject("ScoreDisplay");

        if (scoreTextPosition != null)
        {
            scoreObject.transform.SetParent(scoreTextPosition);
            scoreObject.transform.localPosition = Vector3.zero;
            scoreObject.transform.localRotation = Quaternion.identity;
        }
        else
        {
            scoreObject.transform.position = new Vector3(-5f, -2f, -1f);
        }

        scoreTextMesh = scoreObject.AddComponent<TextMesh>();
        scoreTextMesh.fontSize = 80;
        scoreTextMesh.characterSize = 0.15f;
        scoreTextMesh.anchor = TextAnchor.UpperLeft;
        scoreTextMesh.alignment = TextAlignment.Left;
        scoreTextMesh.color = Color.white;
        scoreTextMesh.text = "Score: 0";

        GameObject bonusObject = new GameObject("BonusScoreDisplay");
        bonusObject.transform.SetParent(scoreObject.transform);
        bonusObject.transform.localPosition = new Vector3(2.5f, 0f, 0f);
        bonusObject.transform.localRotation = Quaternion.identity;
        
        bonusScoreTransform = bonusObject.transform;

        TextMesh bonusTextMesh = bonusObject.AddComponent<TextMesh>();
        bonusTextMesh.fontSize = 80;
        bonusTextMesh.characterSize = 0.15f;
        bonusTextMesh.anchor = TextAnchor.UpperLeft;
        bonusTextMesh.alignment = TextAlignment.Left;
        bonusTextMesh.color = new Color(0.3f, 1f, 0.3f, 1f);
        bonusTextMesh.text = "";

        bonusScoreDisplay = bonusObject.AddComponent<BonusScoreDisplay>();
    }

    private void UpdateScoreDisplay()
    {
        scoreTextMesh.text = $"Score: {totalScore}";
        UpdateBonusPosition();
    }

    private void UpdateBonusPosition()
    {
        if (bonusScoreTransform != null && scoreTextMesh != null)
        {
            MeshRenderer renderer = scoreTextMesh.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                float textWidth = renderer.bounds.size.x;
                bonusScoreTransform.localPosition = new Vector3(textWidth + 0.3f, 0f, 0f);
            }
        }
    }
}
