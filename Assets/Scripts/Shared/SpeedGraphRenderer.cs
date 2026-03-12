using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Reusable utility for rendering pedal speed data into a PNG graph image.
/// Used by both the Control session and the Game session speed graphs.
/// </summary>
public static class SpeedGraphRenderer
{
    public struct SpeedSample
    {
        public float Time;
        public float Speed;
        public int SceneIndex;
    }

    public struct SceneTransition
    {
        public float Time;
        public int SceneIndex;
    }

    public struct GraphSettings
    {
        public int TextureWidth;
        public int TextureHeight;
        public Color BackgroundColor;
        public Color GridColor;
        public Color DividerColor;
    }

    public static void RenderAndSave(
        List<SpeedSample> samples,
        List<SceneTransition> transitions,
        Func<int, Color> sceneColorProvider,
        GraphSettings settings,
        string subdirectory,
        string filePrefix)
    {
        if (samples.Count < 2) return;

        float totalDuration = samples[samples.Count - 1].Time;
        if (totalDuration <= 0f) return;

        var texture = new Texture2D(settings.TextureWidth, settings.TextureHeight, TextureFormat.RGBA32, false);

        ClearTexture(texture, settings);
        DrawGrid(texture, settings);
        DrawSceneDividers(texture, settings, transitions, totalDuration);
        DrawSpeedLine(texture, settings, samples, sceneColorProvider, totalDuration);
        texture.Apply();

        SaveToDisk(texture, subdirectory, filePrefix);
        UnityEngine.Object.Destroy(texture);
    }

    private static void ClearTexture(Texture2D texture, GraphSettings settings)
    {
        Color[] pixels = new Color[settings.TextureWidth * settings.TextureHeight];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = settings.BackgroundColor;
        }
        texture.SetPixels(pixels);
    }

    private static void DrawGrid(Texture2D texture, GraphSettings settings)
    {
        const int horizontalLines = 4;
        for (int i = 1; i <= horizontalLines; i++)
        {
            int y = i * settings.TextureHeight / (horizontalLines + 1);
            DrawHorizontalLine(texture, settings, y, settings.GridColor);
        }
    }

    private static void DrawSceneDividers(
        Texture2D texture,
        GraphSettings settings,
        List<SceneTransition> transitions,
        float totalDuration)
    {
        foreach (var transition in transitions)
        {
            int x = TimeToPixelX(transition.Time, totalDuration, settings.TextureWidth);
            DrawVerticalLine(texture, settings, x, settings.DividerColor);
        }
    }

    private static void DrawSpeedLine(
        Texture2D texture,
        GraphSettings settings,
        List<SpeedSample> samples,
        Func<int, Color> sceneColorProvider,
        float totalDuration)
    {
        for (int i = 1; i < samples.Count; i++)
        {
            int x0 = TimeToPixelX(samples[i - 1].Time, totalDuration, settings.TextureWidth);
            int y0 = SpeedToPixelY(samples[i - 1].Speed, settings.TextureHeight);
            int x1 = TimeToPixelX(samples[i].Time, totalDuration, settings.TextureWidth);
            int y1 = SpeedToPixelY(samples[i].Speed, settings.TextureHeight);

            Color lineColor = sceneColorProvider(samples[i].SceneIndex);
            DrawLine(texture, settings, x0, y0, x1, y1, lineColor);
        }
    }

    private static int TimeToPixelX(float time, float totalDuration, int width)
    {
        return Mathf.Clamp(Mathf.RoundToInt(time / totalDuration * (width - 1)), 0, width - 1);
    }

    private static int SpeedToPixelY(float speed, int height)
    {
        const int padding = 8;
        return Mathf.Clamp(Mathf.RoundToInt(speed * (height - padding * 2) + padding), 0, height - 1);
    }

    private static void SaveToDisk(Texture2D texture, string subdirectory, string filePrefix)
    {
        string directory = Path.Combine(Application.persistentDataPath, subdirectory);
        Directory.CreateDirectory(directory);

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"{filePrefix}_{timestamp}.png";
        string filePath = Path.Combine(directory, fileName);

        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(filePath, pngData);

        Debug.Log($"[SpeedGraphRenderer] Graph saved to: {filePath}");
    }

    private static void DrawHorizontalLine(Texture2D texture, GraphSettings settings, int y, Color color)
    {
        if (y < 0 || y >= settings.TextureHeight) return;
        for (int x = 0; x < settings.TextureWidth; x++)
        {
            texture.SetPixel(x, y, color);
        }
    }

    private static void DrawVerticalLine(Texture2D texture, GraphSettings settings, int x, Color color)
    {
        if (x < 0 || x >= settings.TextureWidth) return;
        for (int y = 0; y < settings.TextureHeight; y++)
        {
            texture.SetPixel(x, y, color);
        }
    }

    private static void DrawLine(Texture2D texture, GraphSettings settings, int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            SetPixelThick(texture, settings, x0, y0, color);

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

    private static void SetPixelThick(Texture2D texture, GraphSettings settings, int x, int y, Color color)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int px = x + dx;
                int py = y + dy;
                if (px >= 0 && px < settings.TextureWidth && py >= 0 && py < settings.TextureHeight)
                {
                    texture.SetPixel(px, py, color);
                }
            }
        }
    }
}
