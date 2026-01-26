using UnityEngine;

public class PlayerColorManager : MonoBehaviour
{
    public static readonly Color[] AvailableColors = new Color[]
    {
        ColorUtility.TryParseHtmlString("#D6877F", out Color red) ? red : Color.red,
        ColorUtility.TryParseHtmlString("#7fb3d6", out Color blue) ? blue : Color.blue,
        ColorUtility.TryParseHtmlString("#92d67f", out Color green) ? green : Color.green,
        ColorUtility.TryParseHtmlString("#fae989", out Color yellow) ? yellow : Color.yellow,
        ColorUtility.TryParseHtmlString("#7fd6b5", out Color cyan) ? cyan : Color.cyan,
        ColorUtility.TryParseHtmlString("#d67fd2", out Color magenta) ? magenta : Color.magenta,
        ColorUtility.TryParseHtmlString("#f0a660", out Color orange) ? orange : new Color(1f, 0.5f, 0f),
        ColorUtility.TryParseHtmlString("#a07fd6", out Color purple) ? purple : new Color(0.5f, 0f, 1f),
        ColorUtility.TryParseHtmlString("#ececec", out Color white) ? white : Color.white,
        ColorUtility.TryParseHtmlString("#222224", out Color black) ? black : Color.black,
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
