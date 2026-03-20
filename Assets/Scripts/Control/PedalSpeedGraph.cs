using System.Collections.Generic;
using UnityEngine;

public class PedalSpeedGraph : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ControlTimer controlTimer;

    [Header("Sampling")]
    [SerializeField] private float sampleInterval = 0.1f;

    [Header("Image Settings")]
    [SerializeField] private int textureWidth = 1920;
    [SerializeField] private int textureHeight = 400;

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.1f, 1f);
    [SerializeField] private Color intervalLineColor = new Color(0.2f, 0.8f, 0.4f, 1f);
    [SerializeField] private Color restLineColor = new Color(0.3f, 0.6f, 1f, 1f);
    [SerializeField] private Color warmupLineColor = new Color(1f, 0.7f, 0.2f, 1f);
    [SerializeField] private Color gridColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
    [SerializeField] private Color phaseDividerColor = new Color(1f, 1f, 1f, 0.3f);

    private const int IntervalSceneIndex = 0;
    private const int RestSceneIndex = 1;
    private const int WarmupSceneIndex = 2;

    private readonly List<SpeedGraphRenderer.SpeedSample> samples = new();
    private readonly List<SpeedGraphRenderer.SceneTransition> transitions = new();

    private float nextSampleTime;
    private bool hasSaved;

    private int lastSceneIndex;
    private bool isFirstSample = true;

    private void OnEnable()
    {
        controlTimer.OnSessionComplete += HandleSessionComplete;
    }

    private void OnDisable()
    {
        controlTimer.OnSessionComplete -= HandleSessionComplete;
    }

    private void Start()
    {
        nextSampleTime = 0f;
        hasSaved = false;
        isFirstSample = true;
    }

    private void Update()
    {
        if (!controlTimer.IsRunning || hasSaved) return;

        if (Time.time >= nextSampleTime)
        {
            int sceneIndex;

            if (controlTimer.IsWarmupPhase)
                sceneIndex = WarmupSceneIndex;
            else if (controlTimer.IsIntervalPhase)
                sceneIndex = IntervalSceneIndex;
            else
                sceneIndex = RestSceneIndex;

            // Håndter transitions riktig mellom ALLE faser
            if (isFirstSample)
            {
                lastSceneIndex = sceneIndex;
                isFirstSample = false;
            }
            else if (sceneIndex != lastSceneIndex)
            {
                transitions.Add(new SpeedGraphRenderer.SceneTransition
                {
                    Time = controlTimer.ElapsedSessionTime,
                    SceneIndex = sceneIndex
                });

                lastSceneIndex = sceneIndex;
            }

            float pedalSpeed = Mathf.Clamp01(PlayPulse.Input.Input.Speed);

            samples.Add(new SpeedGraphRenderer.SpeedSample
            {
                Time = controlTimer.ElapsedSessionTime,
                Speed = pedalSpeed,
                SceneIndex = sceneIndex
            });

            nextSampleTime = Time.time + sampleInterval;
        }
    }

    private void HandleSessionComplete()
    {
        if (hasSaved || samples.Count < 2) return;
        hasSaved = true;

        var settings = new SpeedGraphRenderer.GraphSettings
        {
            TextureWidth = textureWidth,
            TextureHeight = textureHeight,
            BackgroundColor = backgroundColor,
            GridColor = gridColor,
            DividerColor = phaseDividerColor
        };

        SpeedGraphRenderer.RenderAndSave(
            samples,
            transitions,
            GetPhaseColor,
            settings,
            "ControlSessions",
            "PedalSpeed");
    }

    private Color GetPhaseColor(int sceneIndex)
    {
        if (sceneIndex == WarmupSceneIndex)
            return warmupLineColor;

        return sceneIndex == IntervalSceneIndex
            ? intervalLineColor
            : restLineColor;
    }
}