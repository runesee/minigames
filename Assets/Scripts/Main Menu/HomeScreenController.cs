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
    private const int CharacterCount = 4;
    private static readonly int[] ShowcaseColorIndices = { 0, 1, 2, 3 };

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

        float spacing = 1.5f;
        float startX = -((CharacterCount - 1) * spacing) / 2f;

        for (int i = 0; i < CharacterCount; i++)
        {
            GameObject character = Instantiate(characterPrefab, characterParent);
            character.transform.localPosition = new Vector3(startX + (i * spacing), 0f, 0f);
            character.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            character.transform.localScale = Vector3.one * 0.7f;

            int colorIndex = ShowcaseColorIndices[i % ShowcaseColorIndices.Length];
            CharacterPreview preview = character.GetComponent<CharacterPreview>();
            if (preview != null)
            {
                preview.SetColor(PlayerColorManager.GetColor(colorIndex));
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
