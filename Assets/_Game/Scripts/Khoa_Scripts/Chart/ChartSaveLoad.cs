using System.IO;
using UnityEngine;

public static class ChartSaveLoad
{
    public static void Save(ChartData chart, string fileName)
    {
        string json = JsonUtility.ToJson(chart, true);
        string path = GetPath(fileName);

        File.WriteAllText(path, json);

        Debug.Log($"Chart saved to: {path}");
    }

    public static ChartData Load(string fileName)
    {
        string path = GetPath(fileName);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"Chart file not found: {path}");
            return null;
        }

        string json = File.ReadAllText(path);
        ChartData chart = JsonUtility.FromJson<ChartData>(json);

        Debug.Log($"Chart loaded from: {path}");

        return chart;
    }

    private static string GetPath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName + ".json");
    }
}