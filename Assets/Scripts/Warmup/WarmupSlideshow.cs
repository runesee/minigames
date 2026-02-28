using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class WarmupSlideshow : MonoBehaviour
{
    [Header("Slides")]
    [Tooltip("Assign all slide root GameObjects in display order.")]
    [SerializeField] private GameObject[] slides;

    [Header("Navigation Arrows")]
    [SerializeField] private TextMeshProUGUI leftArrow;
    [SerializeField] private TextMeshProUGUI rightArrow;

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 0.4f;

    private const float ArrowActiveAlpha = 0.85f;
    private const float ArrowDimmedAlpha = 0.15f;

    private int currentIndex = 0;
    private bool isTransitioning = false;

    private bool leftBikeWasPressed = false;
    private bool rightBikeWasPressed = false;

    private void Start()
    {
        ShowSlideImmediate(0);
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (isTransitioning) return;

        bool rightTriggered = IsRightTriggered();
        bool leftTriggered = IsLeftTriggered();

        if (rightTriggered)
            NavigateNext();
        else if (leftTriggered)
            NavigatePrevious();

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
        if (isTransitioning || currentIndex >= slides.Length - 1) return;
        StartCoroutine(Transition(currentIndex + 1, direction: 1));
    }

    public void NavigatePrevious()
    {
        if (isTransitioning || currentIndex <= 0) return;
        StartCoroutine(Transition(currentIndex - 1, direction: -1));
    }

    private void ShowSlideImmediate(int index)
    {
        if (slides == null || slides.Length == 0)
        {
            Debug.LogWarning("[WarmupSlideshow] No slides assigned.");
            return;
        }

        for (int i = 0; i < slides.Length; i++)
        {
            if (slides[i] == null) continue;
            slides[i].SetActive(i == index);
            slides[i].GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        currentIndex = index;
        UpdateArrows();
    }

    private IEnumerator Transition(int targetIndex, int direction)
    {
        isTransitioning = true;

        GameObject outgoing = slides[currentIndex];
        GameObject incoming = slides[targetIndex];

        RectTransform outRT = outgoing.GetComponent<RectTransform>();
        RectTransform inRT = incoming.GetComponent<RectTransform>();

        float offset = ((RectTransform)outgoing.transform.parent).rect.width;

        inRT.anchoredPosition = new Vector2(direction * offset, 0f);
        outRT.anchoredPosition = Vector2.zero;
        incoming.SetActive(true);

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));

            outRT.anchoredPosition = new Vector2(Mathf.Lerp(0f, -direction * offset, t), 0f);
            inRT.anchoredPosition = new Vector2(Mathf.Lerp(direction * offset, 0f, t), 0f);

            yield return null;
        }

        inRT.anchoredPosition = Vector2.zero;
        outRT.anchoredPosition = Vector2.zero;
        outgoing.SetActive(false);

        currentIndex = targetIndex;
        UpdateArrows();
        isTransitioning = false;
    }

    private void UpdateArrows()
    {
        if (leftArrow != null)
        {
            var c = leftArrow.color;
            c.a = currentIndex > 0 ? ArrowActiveAlpha : ArrowDimmedAlpha;
            leftArrow.color = c;
        }

        if (rightArrow != null)
        {
            var c = rightArrow.color;
            c.a = currentIndex < slides.Length - 1 ? ArrowActiveAlpha : ArrowDimmedAlpha;
            rightArrow.color = c;
        }
    }
}
