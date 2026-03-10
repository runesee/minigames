using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using DG.Tweening;

public class HomeScreenController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private CanvasGroup fadeOverlay;

    [Header("Character Showcase")]
    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private Transform characterParent;

    [Header("Settings")]
    [SerializeField] private float promptFadeDuration = 1.2f;
    [SerializeField] private float sceneTransitionDuration = 0.8f;
    [SerializeField] private float titleBounceAmplitude = 10f;
    [SerializeField] private float titleBounceDuration = 2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip transitionClip;

    private const string MainMenuSceneName = "MainMenu";

    private const float FrontRowScale = 0.7f;
    private const float BackRowScale = 0.6f;
    private const float BackRowY = 1.0f;
    private const float BackRowZ = 0.5f;

    // Front row (6): evenly spaced at 1.4 apart
    // Positions: -3.5, -2.1, -0.7, 0.7, 2.1, 3.5
    private static readonly float[] FrontRowPositionsX = { -3.5f, -2.1f, -0.7f, 0.7f, 2.1f, 3.5f };
    private static readonly int[] FrontRowColors = { 0, 2, 4, 6, 8, 9 };

    // Back row (4): placed at midpoints between front chars, skipping center for title framing
    // Midpoints: -2.8, -1.4, [0 skipped], 1.4, 2.8
    private static readonly float[] BackRowPositionsX = { -2.8f, -1.4f, 1.4f, 2.8f };
    private static readonly int[] BackRowColors = { 1, 3, 5, 7 };

    private bool isTransitioning;
    private Sequence promptSequence;
    private Sequence titleSequence;

    private void Start()
    {
        SetupFadeOverlay();
        SetupTitleAnimation();
        SetupPromptAnimation();
        SpawnCharacterShowcase();
        FadeIn();
    }

    private void Update()
    {
        if (isTransitioning) return;

        if (AnyInputDetected())
        {
            TransitionToMainMenu();
        }
    }

    private void OnDestroy()
    {
        promptSequence?.Kill();
        titleSequence?.Kill();
    }

    private void SetupFadeOverlay()
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 1f;
            fadeOverlay.blocksRaycasts = true;
        }
    }

    private void FadeIn()
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.DOFade(0f, sceneTransitionDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => fadeOverlay.blocksRaycasts = false);
        }
    }

    private void SetupTitleAnimation()
    {
        if (titleText == null) return;

        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        Vector2 originalPos = titleRect.anchoredPosition;

        titleSequence = DOTween.Sequence();
        titleSequence.Append(
            titleRect.DOAnchorPosY(originalPos.y + titleBounceAmplitude, titleBounceDuration)
                .SetEase(Ease.InOutSine)
        );
        titleSequence.Append(
            titleRect.DOAnchorPosY(originalPos.y, titleBounceDuration)
                .SetEase(Ease.InOutSine)
        );
        titleSequence.SetLoops(-1);
    }

    private void SetupPromptAnimation()
    {
        if (promptText == null) return;

        promptSequence = DOTween.Sequence();
        promptSequence.Append(
            promptText.DOFade(0.2f, promptFadeDuration).SetEase(Ease.InOutSine)
        );
        promptSequence.Append(
            promptText.DOFade(1f, promptFadeDuration).SetEase(Ease.InOutSine)
        );
        promptSequence.SetLoops(-1);
    }

    private void SpawnCharacterShowcase()
    {
        if (characterPrefab == null || characterParent == null) return;

        SpawnRow(FrontRowColors, FrontRowPositionsX, 0f, 0f, FrontRowScale);
        SpawnRow(BackRowColors, BackRowPositionsX, BackRowZ, BackRowY, BackRowScale);
    }

    private void SpawnRow(int[] colorIndices, float[] positionsX, float zOffset, float yOffset, float scale)
    {
        for (int i = 0; i < colorIndices.Length; i++)
        {
            GameObject character = Instantiate(characterPrefab, characterParent);
            character.transform.localPosition = new Vector3(positionsX[i], yOffset, zOffset);
            character.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            character.transform.localScale = Vector3.one * scale;

            CharacterPreview preview = character.GetComponent<CharacterPreview>();
            if (preview != null)
            {
                preview.SetColor(PlayerColorManager.GetColor(colorIndices[i]));
            }
        }
    }

    private bool AnyInputDetected()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
        {
            return true;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            if (gamepad.buttonSouth.wasPressedThisFrame ||
                gamepad.buttonNorth.wasPressedThisFrame ||
                gamepad.buttonEast.wasPressedThisFrame ||
                gamepad.buttonWest.wasPressedThisFrame ||
                gamepad.startButton.wasPressedThisFrame)
            {
                return true;
            }
        }

        try
        {
            if (PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.A) ||
                PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.B) ||
                PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.X) ||
                PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.Y))
            {
                return true;
            }
        }
        catch (System.Exception)
        {
            // PlayPulse not available
        }

        return false;
    }

    private void TransitionToMainMenu()
    {
        isTransitioning = true;

        if (transitionClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(transitionClip);
        }

        promptSequence?.Kill();
        if (promptText != null)
        {
            promptText.alpha = 1f;
        }

        if (fadeOverlay != null)
        {
            fadeOverlay.blocksRaycasts = true;
            fadeOverlay.DOFade(1f, sceneTransitionDuration)
                .SetEase(Ease.InQuad)
                .OnComplete(LoadMainMenu);
        }
        else
        {
            LoadMainMenu();
        }
    }

    private void LoadMainMenu()
    {
        SceneManager.LoadScene(MainMenuSceneName);
    }
}
