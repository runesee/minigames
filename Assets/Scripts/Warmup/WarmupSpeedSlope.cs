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

    private readonly List<RectTransform> activeSegments = new();
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

        foreach (var rt in activeSegments)
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x - delta, rt.anchoredPosition.y);

        while (activeSegments.Count > 0 &&
               activeSegments[0].anchoredPosition.x + activeSegments[0].sizeDelta.x < 0f)
        {
            Destroy(activeSegments[0].gameObject);
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

    private float GetRightmostX()
    {
        if (activeSegments.Count == 0) return 0f;
        var last = activeSegments[activeSegments.Count - 1];
        return last.anchoredPosition.x + last.sizeDelta.x;
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

        activeSegments.Add(rt);
    }

    private static SlopeSegmentData[] BuildDefaultPattern() => new SlopeSegmentData[]
    {
        new() { minSpeed = 0.50f, maxSpeed = 0.60f, widthUnits = 480f, color = ColorNormal   },
        new() { minSpeed = 0.50f, maxSpeed = 0.60f, widthUnits = 360f, color = ColorNormal   },
        new() { minSpeed = 0.70f, maxSpeed = 0.80f, widthUnits = 220f, color = ColorInterval },
        new() { minSpeed = 0.50f, maxSpeed = 0.60f, widthUnits = 440f, color = ColorNormal   },
        new() { minSpeed = 0.80f, maxSpeed = 0.90f, widthUnits = 160f, color = ColorSprint   },
        new() { minSpeed = 0.50f, maxSpeed = 0.60f, widthUnits = 500f, color = ColorNormal   },
        new() { minSpeed = 0.72f, maxSpeed = 0.82f, widthUnits = 200f, color = ColorInterval },
        new() { minSpeed = 0.50f, maxSpeed = 0.60f, widthUnits = 400f, color = ColorNormal   },
    };
}
