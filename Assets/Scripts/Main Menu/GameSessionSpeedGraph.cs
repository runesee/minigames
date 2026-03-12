using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSessionSpeedGraph : MonoBehaviour
{
    private const string LobbySceneName = "Lobby";
    private const string EndScreenSceneName = "EndScreen";

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
    [SerializeField] private Color defaultColor = Color.white;

    private readonly List<SpeedGraphRenderer.SpeedSample> samples = new();
    private readonly List<SpeedGraphRenderer.SceneTransition> transitions = new();
    private readonly Dictionary<string, int> sceneNameToIndex = new();
    private readonly Dictionary<int, string> indexToSceneName = new();
    private float nextSampleTime;
    private float sessionStartTime;
    private bool isRecording;
    private bool hasSaved;
    private int currentSceneIndex = -1;
    private int nextIndex;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;

        if (!isRecording && sceneName == LobbySceneName)
        {
            StartRecording();
        }

        if (!isRecording) return;

        int sceneIndex = GetOrCreateSceneIndex(sceneName);

        if (sceneIndex != currentSceneIndex)
        {
            float elapsed = GetElapsedTime();
            transitions.Add(new SpeedGraphRenderer.SceneTransition
            {
                Time = elapsed,
                SceneIndex = sceneIndex
            });
            currentSceneIndex = sceneIndex;
        }

        if (sceneName == EndScreenSceneName)
        {
            SaveGraph();
        }
    }

    private int GetOrCreateSceneIndex(string sceneName)
    {
        if (sceneNameToIndex.TryGetValue(sceneName, out int existing))
        {
            return existing;
        }

        int index = nextIndex++;
        sceneNameToIndex[sceneName] = index;
        indexToSceneName[index] = sceneName;
        return index;
    }

    private void StartRecording()
    {
        samples.Clear();
        transitions.Clear();
        sceneNameToIndex.Clear();
        indexToSceneName.Clear();
        nextIndex = 0;
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
        if (!indexToSceneName.TryGetValue(sceneIndex, out string sceneName))
        {
            return defaultColor;
        }

        return sceneName switch
        {
            "MainMenu" => lobbyColor,
            "Lobby" => lobbyColor,
            "Scoreboard" => scoreboardColor,
            "TagTutorial" => tutorialColor,
            "TagScene" => tagColor,
            "FocusFlowTutorial" => tutorialColor,
            "FocusFlow" => focusFlowColor,
            "ColorFloodTutorial" => tutorialColor,
            "ColorFlood" => colorFloodColor,
            "RedLightTutorial" => tutorialColor,
            "RedLight" => redLightColor,
            "BalloonTagTutorial" => tutorialColor,
            "BalloonTag" => balloonTagColor,
            "CaptureTheFlagTutorial" => tutorialColor,
            "CaptureTheFlag" => captureTheFlagColor,
            "EndScreen" => endScreenColor,
            _ => defaultColor
        };
    }
}
