using System.Collections.Generic;
using UnityEngine;

public class WarmupPedalSpeedGraph : MonoBehaviour
{
    [Header("Sampling")]
    [SerializeField] private float sampleInterval = 0.1f;

    [Header("Image Settings")]
    [SerializeField] private int textureWidth = 1920;
    [SerializeField] private int textureHeight = 400;

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.1f, 1f);
    [SerializeField] private Color lineColor = new Color(1f, 0.7f, 0.2f, 1f);
    [SerializeField] private Color gridColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);

    private readonly List<SpeedGraphRenderer.SpeedSample> samples = new();
    private readonly List<SpeedGraphRenderer.SceneTransition> transitions = new();

    private float startTime;
    private float nextSampleTime;
    private bool hasSaved;

    private void Start()
    {
        startTime = Time.time;
        nextSampleTime = 0f;
        hasSaved = false;
    }

    private void Update()
    {
        if (hasSaved) return;

        if (Time.time >= nextSampleTime)
        {
            float elapsed = Time.time - startTime;
            float speed = Mathf.Clamp01(PlayPulse.Input.Input.Speed);

            samples.Add(new SpeedGraphRenderer.SpeedSample
            {
                Time = elapsed,
                Speed = speed,
                SceneIndex = 2
            });

            nextSampleTime = Time.time + sampleInterval;
        }
    }

    private void OnDestroy()
    {
        if (!hasSaved)
        {
            SaveGraph();
        }
    }

    private void SaveGraph()
    {
        if (samples.Count < 2)
        {
            return;
        }

        hasSaved = true;

        var settings = new SpeedGraphRenderer.GraphSettings
        {
            TextureWidth = textureWidth,
            TextureHeight = textureHeight,
            BackgroundColor = backgroundColor,
            GridColor = gridColor,
            DividerColor = Color.clear
        };

        SpeedGraphRenderer.RenderAndSave(
            samples,
            transitions,
            _ => lineColor,
            settings,
            "WarmupSessions",
            "PedalSpeed_Warmup");

        Debug.Log("Warmup graph saved!");
    }
}