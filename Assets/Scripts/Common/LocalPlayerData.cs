using UnityEngine;
using System.IO;

[System.Serializable]
public class LocalPlayerData
{
    public string guid;
    public string nickname;
    public string color;
}

public static class LocalPlayerStorage
{
    // Linux workaround
    #if UNITY_EDITOR
    private static string FilePath => Path.Combine(Application.persistentDataPath, "playerdata_editor.json");
    #else
    private static string FilePath => Path.Combine(Application.persistentDataPath, "playerdata_build.json");
    #endif

    public static void Save(LocalPlayerData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(FilePath, json);
    }

    public static LocalPlayerData Load()
    {
        if (!File.Exists(FilePath)) return null;
        string json = File.ReadAllText(FilePath);
        return JsonUtility.FromJson<LocalPlayerData>(json);
    }

    public static void Clear()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
    }
}

