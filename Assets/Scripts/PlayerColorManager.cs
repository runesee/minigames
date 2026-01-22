using UnityEngine;

public class PlayerColorManager : MonoBehaviour
{
    public static readonly Color[] AvailableColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.cyan,
        Color.magenta,
        new Color(1f, 0.5f, 0f),
        new Color(0.5f, 0f, 1f),
        Color.white,
        Color.black
    };

    public static readonly string[] ColorNames = new string[]
    {
        "Red",
        "Blue",
        "Green",
        "Yellow",
        "Cyan",
        "Magenta",
        "Orange",
        "Purple",
        "White",
        "Black"
    };

    public static Color GetColor(int index)
    {
        if (index >= 0 && index < AvailableColors.Length)
        {
            return AvailableColors[index];
        }
        return Color.white;
    }

    public static string GetColorName(int index)
    {
        if (index >= 0 && index < ColorNames.Length)
        {
            return ColorNames[index];
        }
        return "Unknown";
    }
}
