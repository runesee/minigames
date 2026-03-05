using System.Collections.Generic;
using UnityEngine;

public class WarmupPedalSpeedDisplay : MonoBehaviour
{
    [SerializeField] private RectTransform container;
    [SerializeField] private RectTransform speedIndicator;
    [SerializeField] private float averagingWindow = 1f;

    private readonly Queue<(float time, float speed)> samples = new();

    private void Update()
    {
        float now = Time.time;
        samples.Enqueue((now, Mathf.Clamp01(PlayPulse.Input.Input.Speed)));

        while (samples.Count > 0 && now - samples.Peek().time > averagingWindow)
            samples.Dequeue();

        float sum = 0f;
        foreach (var s in samples) sum += s.speed;
        float averaged = sum / samples.Count;

        speedIndicator.anchoredPosition = new Vector2(speedIndicator.anchoredPosition.x, averaged * container.rect.height);
    }
}
