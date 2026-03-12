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
    [SerializeField] private Color gridColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
    [SerializeField] private Color phaseDividerColor = new Color(1f, 1f, 1f, 0.3f);

    private const int IntervalSceneIndex = 0;
    private const int RestSceneIndex = 1;

    private readonly List<SpeedGraphRenderer.SpeedSample> samples = new();
    private readonly List<SpeedGraphRenderer.SceneTransition> transitions = new();
    private float nextSampleTime;
    private bool hasSaved;
    private bool lastIsInterval = true;

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
        lastIsInterval = true;
    }

    private void Update()
    {
        if (!controlTimer.IsRunning || hasSaved) return;

        if (Time.time >= nextSampleTime)
        {
            bool currentIsInterval = controlTimer.IsIntervalPhase;
            int sceneIndex = currentIsInterval ? IntervalSceneIndex : RestSceneIndex;

            if (currentIsInterval != lastIsInterval)
            {
                transitions.Add(new SpeedGraphRenderer.SceneTransition
                {
                    Time = controlTimer.ElapsedSessionTime,
                    SceneIndex = sceneIndex
                });
                lastIsInterval = currentIsInterval;
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
        return sceneIndex == IntervalSceneIndex ? intervalLineColor : restLineColor;
    }
}
