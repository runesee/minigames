using UnityEngine;

public class WarmupPedalSpeedDisplay : MonoBehaviour
{
    [SerializeField] private RectTransform container;
    [SerializeField] private RectTransform speedIndicator;

    private void Update()
    {
        float speed = Mathf.Clamp01(PlayPulse.Input.Input.Speed);
        speedIndicator.anchoredPosition = new Vector2(speedIndicator.anchoredPosition.x, speed * container.rect.height);
    }
}
