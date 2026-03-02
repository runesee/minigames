using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct SlopeSegmentData
{
    public float minSpeed;
    public float maxSpeed;
    public float widthUnits;
    public Color color;
    public float pointsMultiplier;
}

public class WarmupSpeedSlope : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform container;

    [Header("Scroll")]
    [SerializeField] private float scrollSpeed = 150f;

    [Header("Segment Pattern")]
    [SerializeField] private SlopeSegmentData[] pattern;

    private static readonly Color ColorNormal = new(0.2f, 0.85f, 0.2f, 0.5f);
    private static readonly Color ColorInterval = new(1.0f, 0.60f, 0.1f, 0.6f);
    private static readonly Color ColorSprint = new(1.0f, 0.25f, 0.25f, 0.6f);

    private struct SegmentInstance
    {
        public RectTransform rectTransform;
        public float pointsMultiplier;
    }

    private readonly List<SegmentInstance> activeSegments = new();
    private int nextPatternIndex;
    private float containerWidth;
    private float containerHeight;

    private void Awake()
    {
        if (pattern == null || pattern.Length == 0)
            pattern = BuildDefaultPattern();
    }

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        containerWidth = container.rect.width;
        containerHeight = container.rect.height;

        float fillX = 0f;
        while (fillX < containerWidth + 500f)
        {
            int idx = nextPatternIndex;
            SpawnSegmentAt(fillX);
            fillX += pattern[idx].widthUnits;
        }
    }

    private void Update()
    {
        containerWidth = container.rect.width;
        containerHeight = container.rect.height;

        float delta = scrollSpeed * Time.deltaTime;

        foreach (var seg in activeSegments)
            seg.rectTransform.anchoredPosition = new Vector2(seg.rectTransform.anchoredPosition.x - delta, seg.rectTransform.anchoredPosition.y);

        while (activeSegments.Count > 0 &&
               activeSegments[0].rectTransform.anchoredPosition.x + activeSegments[0].rectTransform.sizeDelta.x < 0f)
        {
            Destroy(activeSegments[0].rectTransform.gameObject);
            activeSegments.RemoveAt(0);
        }

        float rightmostX = GetRightmostX();
        while (rightmostX < containerWidth)
        {
            int idx = nextPatternIndex;
            SpawnSegmentAt(rightmostX);
            rightmostX += pattern[idx].widthUnits;
        }
    }

    public bool TryGetCurrentTarget(out float minSpeed, out float maxSpeed, out float pointsMultiplier)
    {
        foreach (var seg in activeSegments)
        {
            float left = seg.rectTransform.anchoredPosition.x;
            float right = left + seg.rectTransform.sizeDelta.x;
            if (left <= 0f && right > 0f)
            {
                minSpeed = containerHeight > 0f ? seg.rectTransform.anchoredPosition.y / containerHeight : 0f;
                maxSpeed = containerHeight > 0f ? minSpeed + seg.rectTransform.sizeDelta.y / containerHeight : 0f;
                pointsMultiplier = seg.pointsMultiplier;
                return true;
            }
        }
        minSpeed = maxSpeed = pointsMultiplier = 0f;
        return false;
    }

    private float GetRightmostX()
    {
        if (activeSegments.Count == 0) return 0f;
        var last = activeSegments[activeSegments.Count - 1];
        return last.rectTransform.anchoredPosition.x + last.rectTransform.sizeDelta.x;
    }

    private void SpawnSegmentAt(float x)
    {
        SlopeSegmentData data = pattern[nextPatternIndex];
        nextPatternIndex = (nextPatternIndex + 1) % pattern.Length;

        var go = new GameObject("SlopeSegment");
        go.layer = container.gameObject.layer;
        go.AddComponent<RectTransform>();
        go.transform.SetParent(container, false);
        go.transform.SetSiblingIndex(0);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.sizeDelta = new Vector2(data.widthUnits, (data.maxSpeed - data.minSpeed) * containerHeight);
        rt.anchoredPosition = new Vector2(x, data.minSpeed * containerHeight);

        var img = go.AddComponent<Image>();
        img.color = data.color;
        img.raycastTarget = false;

        activeSegments.Add(new SegmentInstance { rectTransform = rt, pointsMultiplier = data.pointsMultiplier });
    }

    private static SlopeSegmentData[] BuildDefaultPattern() => new SlopeSegmentData[]
    {
        new() { minSpeed = 0.50f, maxSpeed = 0.60f, widthUnits = 480f, color = ColorNormal,   pointsMultiplier = 1f },
        new() { minSpeed = 0.50f, maxSpeed = 0.60f, widthUnits = 360f, color = ColorNormal,   pointsMultiplier = 1f },
        new() { minSpeed = 0.70f, maxSpeed = 0.80f, widthUnits = 220f, color = ColorInterval, pointsMultiplier = 2f },
        new() { minSpeed = 0.50f, maxSpeed = 0.60f, widthUnits = 440f, color = ColorNormal,   pointsMultiplier = 1f },
        new() { minSpeed = 0.80f, maxSpeed = 0.90f, widthUnits = 160f, color = ColorSprint,   pointsMultiplier = 3f },
        new() { minSpeed = 0.50f, maxSpeed = 0.60f, widthUnits = 500f, color = ColorNormal,   pointsMultiplier = 1f },
        new() { minSpeed = 0.72f, maxSpeed = 0.82f, widthUnits = 200f, color = ColorInterval, pointsMultiplier = 2f },
        new() { minSpeed = 0.50f, maxSpeed = 0.60f, widthUnits = 400f, color = ColorNormal,   pointsMultiplier = 1f },
    };
}
