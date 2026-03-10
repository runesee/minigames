using UnityEngine;
using DG.Tweening;

/// <summary>
/// Attach to a CanvasGroup overlay to fade in from black when a scene loads.
/// Starts fully opaque and fades to transparent.
/// </summary>
public class SceneFadeIn : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 0.8f;

    private void Start()
    {
        if (fadeOverlay == null) return;

        fadeOverlay.alpha = 1f;
        fadeOverlay.blocksRaycasts = true;

        fadeOverlay.DOFade(0f, fadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                fadeOverlay.blocksRaycasts = false;
                fadeOverlay.interactable = false;
            });
    }
}
