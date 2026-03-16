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
    [SerializeField] private RuntimeAnimatorController showcaseBaseController;
    [SerializeField] private AnimationClip[] showcaseAnimations;

    [Header("Settings")]
    [SerializeField] private float promptFadeDuration = 1.2f;
    [SerializeField] private float sceneTransitionDuration = 0.8f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip transitionClip;

    private const string MainMenuSceneName = "MainMenu";

    private const float FrontRowScale = 0.7f;
    private const float BackRowScale = 0.6f;
    private const float BackRowY = 1.0f;
    private const float BackRowZ = 0.5f;

    private static readonly float[] FrontRowPositionsX = { -3.5f, -2.1f, -0.7f, 0.7f, 2.1f, 3.5f };
    private static readonly int[] FrontRowColors = { 0, 2, 4, 6, 8, 9 };

    private static readonly float[] BackRowPositionsX = { -2.8f, -1.4f, 1.4f, 2.8f };
    private static readonly int[] BackRowColors = { 1, 3, 5, 7 };

    private bool isTransitioning;
    private Sequence promptSequence;

    private void Start()
    {
        fadeOverlay.alpha = 1f;
        fadeOverlay.blocksRaycasts = true;

        SetupTitleAnimator();
        SetupPromptAnimation();
        SpawnCharacterShowcase();

        fadeOverlay.DOFade(0f, sceneTransitionDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => fadeOverlay.blocksRaycasts = false);
    }

    private void Update()
    {
        if (isTransitioning) return;

        if (AnyInputDetected())
            TransitionToMainMenu();
    }

    private void OnDestroy()
    {
        promptSequence?.Kill();
    }

    private void SetupTitleAnimator()
    {
        if (titleText.GetComponent<TitleAnimator>() == null)
        {
            titleText.gameObject.AddComponent<TitleAnimator>();
        }
    }

    private void SetupPromptAnimation()
    {
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
        int animIndex = 0;
        animIndex = SpawnRow(FrontRowColors, FrontRowPositionsX, 0f, 0f, FrontRowScale, animIndex);
        SpawnRow(BackRowColors, BackRowPositionsX, BackRowZ, BackRowY, BackRowScale, animIndex);
    }

    private int SpawnRow(int[] colorIndices, float[] positionsX, float zOffset, float yOffset, float scale, int animStartIndex)
    {
        int animIndex = animStartIndex;

        for (int i = 0; i < colorIndices.Length; i++)
        {
            GameObject character = Instantiate(characterPrefab, characterParent);
            character.transform.localPosition = new Vector3(positionsX[i], yOffset, zOffset);
            character.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            character.transform.localScale = Vector3.one * scale;

            CharacterPreview preview = character.GetComponent<CharacterPreview>();
            preview.SetColor(PlayerColorManager.GetColor(colorIndices[i]));

            if (showcaseBaseController != null && showcaseAnimations != null && showcaseAnimations.Length > 0)
            {
                ShowcaseAnimator showcaseAnimator = character.AddComponent<ShowcaseAnimator>();
                AnimationClip clip = showcaseAnimations[animIndex % showcaseAnimations.Length];
                float timeOffset = (float)animIndex / showcaseAnimations.Length;
                showcaseAnimator.Initialize(showcaseBaseController, clip, timeOffset);
                animIndex++;
            }
        }

        return animIndex;
    }

    private bool AnyInputDetected()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            return true;

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            if (gamepad.buttonSouth.wasPressedThisFrame ||
                gamepad.buttonNorth.wasPressedThisFrame ||
                gamepad.buttonEast.wasPressedThisFrame ||
                gamepad.buttonWest.wasPressedThisFrame ||
                gamepad.startButton.wasPressedThisFrame)
                return true;
        }

        try
        {
            if (PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.A) ||
                PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.B) ||
                PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.X) ||
                PlayPulse.Input.Input.GetButtonDown(PlayPulse.Input.Input.Button.Y))
                return true;
        }
        catch (System.Exception) { }

        return false;
    }

    private void TransitionToMainMenu()
    {
        isTransitioning = true;

        audioSource.PlayOneShot(transitionClip);

        promptSequence?.Kill();
        promptText.alpha = 1f;

        fadeOverlay.blocksRaycasts = true;
        fadeOverlay.DOFade(1f, sceneTransitionDuration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => SceneManager.LoadScene(MainMenuSceneName));
    }
}
