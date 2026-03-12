using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Records pedal speed across the entire game session (Lobby to EndScreen)
/// and saves a color-coded graph PNG when the EndScreen is reached.
/// Attach this to the MinigameManager prefab so it persists across scenes.
/// </summary>
public class GameSessionSpeedGraph : MonoBehaviour
{
    [Header("Sampling")]
    [SerializeField] private float sampleInterval = 0.1f;

    [Header("Image Settings")]
    [SerializeField] private int textureWidth = 1920;
    [SerializeField] private int textureHeight = 400;

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.1f, 1f);
    [SerializeField] private Color gridColor = new Color(0.2f, 0.2f, 0.3f, 0.5f);
    [SerializeField] private Color dividerColor = new Color(1f, 1f, 1f, 0.3f);

    [Header("Scene Colors")]
    [SerializeField] private Color lobbyColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color tagColor = new Color(0.9f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color focusFlowColor = new Color(0.2f, 0.8f, 0.4f, 1f);
    [SerializeField] private Color colorFloodColor = new Color(0.9f, 0.6f, 0.1f, 1f);
    [SerializeField] private Color redLightColor = new Color(0.8f, 0.2f, 0.5f, 1f);
    [SerializeField] private Color balloonTagColor = new Color(0.3f, 0.6f, 1f, 1f);
    [SerializeField] private Color captureTheFlagColor = new Color(0.7f, 0.4f, 0.9f, 1f);
    [SerializeField] private Color scoreboardColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color tutorialColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color endScreenColor = new Color(1f, 1f, 1f, 1f);

    private readonly List<SpeedGraphRenderer.SpeedSample> samples = new();
    private readonly List<SpeedGraphRenderer.SceneTransition> transitions = new();
    private float nextSampleTime;
    private float sessionStartTime;
    private bool isRecording;
    private bool hasSaved;
    private int currentSceneIndex = -1;

    private readonly Dictionary<MinigameManager.MinigameScene, int> sceneToIndex = new();

    private void Awake()
    {
        BuildSceneIndexMap();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void BuildSceneIndexMap()
    {
        int index = 0;
        foreach (MinigameManager.MinigameScene scene in Enum.GetValues(typeof(MinigameManager.MinigameScene)))
        {
            sceneToIndex[scene] = index++;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (MinigameManager.Instance == null) return;

        var currentState = MinigameManager.Instance.currentGameState;

        if (!isRecording && currentState == MinigameManager.MinigameScene.Lobby)
        {
            StartRecording();
        }

        if (!isRecording) return;

        int newSceneIndex = sceneToIndex.GetValueOrDefault(currentState, 0);

        if (newSceneIndex != currentSceneIndex)
        {
            float elapsed = GetElapsedTime();
            transitions.Add(new SpeedGraphRenderer.SceneTransition
            {
                Time = elapsed,
                SceneIndex = newSceneIndex
            });
            currentSceneIndex = newSceneIndex;
        }

        if (currentState == MinigameManager.MinigameScene.EndScreen)
        {
            SaveGraph();
        }
    }

    private void StartRecording()
    {
        samples.Clear();
        transitions.Clear();
        sessionStartTime = Time.realtimeSinceStartup;
        nextSampleTime = 0f;
        isRecording = true;
        hasSaved = false;
        currentSceneIndex = -1;
    }

    private float GetElapsedTime()
    {
        return Time.realtimeSinceStartup - sessionStartTime;
    }

    private void Update()
    {
        if (!isRecording || hasSaved) return;

        if (Time.realtimeSinceStartup >= nextSampleTime)
        {
            float pedalSpeed = Mathf.Clamp01(PlayPulse.Input.Input.Speed);

            samples.Add(new SpeedGraphRenderer.SpeedSample
            {
                Time = GetElapsedTime(),
                Speed = pedalSpeed,
                SceneIndex = currentSceneIndex
            });

            nextSampleTime = Time.realtimeSinceStartup + sampleInterval;
        }
    }

    private void SaveGraph()
    {
        if (hasSaved || samples.Count < 2) return;
        hasSaved = true;
        isRecording = false;

        var settings = new SpeedGraphRenderer.GraphSettings
        {
            TextureWidth = textureWidth,
            TextureHeight = textureHeight,
            BackgroundColor = backgroundColor,
            GridColor = gridColor,
            DividerColor = dividerColor
        };

        SpeedGraphRenderer.RenderAndSave(
            samples,
            transitions,
            GetSceneColor,
            settings,
            "GameSessions",
            "GamePedalSpeed");
    }

    private Color GetSceneColor(int sceneIndex)
    {
        // Reverse lookup from index to scene enum
        foreach (var kvp in sceneToIndex)
        {
            if (kvp.Value == sceneIndex)
            {
                return GetColorForScene(kvp.Key);
            }
        }
        return Color.white;
    }

    private Color GetColorForScene(MinigameManager.MinigameScene scene)
    {
        return scene switch
        {
            MinigameManager.MinigameScene.MainMenu => lobbyColor,
            MinigameManager.MinigameScene.Lobby => lobbyColor,
            MinigameManager.MinigameScene.Scoreboard => scoreboardColor,
            MinigameManager.MinigameScene.TagTutorial => tutorialColor,
            MinigameManager.MinigameScene.Tag => tagColor,
            MinigameManager.MinigameScene.FocusFlowTutorial => tutorialColor,
            MinigameManager.MinigameScene.FocusFlow => focusFlowColor,
            MinigameManager.MinigameScene.ColorFloodTutorial => tutorialColor,
            MinigameManager.MinigameScene.ColorFlood => colorFloodColor,
            MinigameManager.MinigameScene.RedLightTutorial => tutorialColor,
            MinigameManager.MinigameScene.RedLight => redLightColor,
            MinigameManager.MinigameScene.BalloonTagTutorial => tutorialColor,
            MinigameManager.MinigameScene.BalloonTag => balloonTagColor,
            MinigameManager.MinigameScene.CaptureTheFlagTutorial => tutorialColor,
            MinigameManager.MinigameScene.CaptureTheFlag => captureTheFlagColor,
            MinigameManager.MinigameScene.EndScreen => endScreenColor,
            _ => Color.white
        };
    }
}
