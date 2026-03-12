using System;
using System.Collections.Generic;
using System.IO;
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

    private readonly List<SpeedSample> samples = new();
    private float nextSampleTime;
    private bool hasSaved;

    private struct SpeedSample
    {
        public float Time;
        public float Speed;
        public bool IsIntervalPhase;
    }

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
    }

    private void Update()
    {
        if (!controlTimer.IsRunning || hasSaved) return;

        if (Time.time >= nextSampleTime)
        {
            float pedalSpeed = Mathf.Clamp01(PlayPulse.Input.Input.Speed);

            samples.Add(new SpeedSample
            {
                Time = controlTimer.ElapsedSessionTime,
                Speed = pedalSpeed,
                IsIntervalPhase = controlTimer.IsIntervalPhase
            });

            nextSampleTime = Time.time + sampleInterval;
        }
    }

    private void HandleSessionComplete()
    {
        if (hasSaved || samples.Count < 2) return;
        hasSaved = true;

        Texture2D graphTexture = RenderGraph();
        SaveGraphToDisk(graphTexture);
        Destroy(graphTexture);
    }

    private Texture2D RenderGraph()
    {
        var texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);

        ClearTexture(texture);
        DrawGrid(texture);
        DrawPhaseDividers(texture);
        DrawSpeedLine(texture);
        texture.Apply();

        return texture;
    }

    private void SaveGraphToDisk(Texture2D texture)
    {
        string directory = Path.Combine(Application.persistentDataPath, "ControlSessions");
        Directory.CreateDirectory(directory);

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"PedalSpeed_{timestamp}.png";
        string filePath = Path.Combine(directory, fileName);

        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(filePath, pngData);

        Debug.Log($"[PedalSpeedGraph] Graph saved to: {filePath}");
    }

    private float TotalSessionDuration => controlTimer.TotalSessionDuration;

    private void ClearTexture(Texture2D texture)
    {
        Color[] pixels = new Color[textureWidth * textureHeight];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = backgroundColor;
        }
        texture.SetPixels(pixels);
    }

    private void DrawGrid(Texture2D texture)
    {
        const int horizontalLines = 4;
        for (int i = 1; i <= horizontalLines; i++)
        {
            int y = i * textureHeight / (horizontalLines + 1);
            DrawHorizontalLine(texture, y, gridColor);
        }
    }

    private void DrawPhaseDividers(Texture2D texture)
    {
        if (TotalSessionDuration <= 0f || samples.Count == 0) return;

        bool lastPhase = samples[0].IsIntervalPhase;
        for (int i = 1; i < samples.Count; i++)
        {
            if (samples[i].IsIntervalPhase != lastPhase)
            {
                lastPhase = samples[i].IsIntervalPhase;
                int x = TimeToPixelX(samples[i].Time);
                DrawVerticalLine(texture, x, phaseDividerColor);
            }
        }
    }

    private void DrawSpeedLine(Texture2D texture)
    {
        if (samples.Count < 2) return;

        for (int i = 1; i < samples.Count; i++)
        {
            int x0 = TimeToPixelX(samples[i - 1].Time);
            int y0 = SpeedToPixelY(samples[i - 1].Speed);
            int x1 = TimeToPixelX(samples[i].Time);
            int y1 = SpeedToPixelY(samples[i].Speed);

            Color lineColor = samples[i].IsIntervalPhase ? intervalLineColor : restLineColor;
            DrawLine(texture, x0, y0, x1, y1, lineColor);
        }
    }

    private int TimeToPixelX(float time)
    {
        return Mathf.Clamp(
            Mathf.RoundToInt(time / TotalSessionDuration * (textureWidth - 1)),
            0, textureWidth - 1);
    }

    private int SpeedToPixelY(float speed)
    {
        const int padding = 8;
        return Mathf.Clamp(
            Mathf.RoundToInt(speed * (textureHeight - padding * 2) + padding),
            0, textureHeight - 1);
    }

    private void DrawHorizontalLine(Texture2D texture, int y, Color color)
    {
        if (y < 0 || y >= textureHeight) return;
        for (int x = 0; x < textureWidth; x++)
        {
            texture.SetPixel(x, y, color);
        }
    }

    private void DrawVerticalLine(Texture2D texture, int x, Color color)
    {
        if (x < 0 || x >= textureWidth) return;
        for (int y = 0; y < textureHeight; y++)
        {
            texture.SetPixel(x, y, color);
        }
    }

    private void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            SetPixelThick(texture, x0, y0, color);

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private void SetPixelThick(Texture2D texture, int x, int y, Color color)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int px = x + dx;
                int py = y + dy;
                if (px >= 0 && px < textureWidth && py >= 0 && py < textureHeight)
                {
                    texture.SetPixel(px, py, color);
                }
            }
        }
    }
}
