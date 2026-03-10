using UnityEngine;
using DG.Tweening;

public class SceneFadeIn : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 0.8f;

    private void Start()
    {
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
