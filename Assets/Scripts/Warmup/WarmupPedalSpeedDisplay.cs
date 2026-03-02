using UnityEngine;

public class WarmupPedalSpeedDisplay : MonoBehaviour
{
    [SerializeField] private RectTransform container;
    [SerializeField] private RectTransform speedLine;

    private void Update()
    {
        float speed = Mathf.Clamp01(PlayPulse.Input.Input.Speed);
        speedLine.anchoredPosition = new Vector2(0f, speed * container.rect.height);
    }
}
