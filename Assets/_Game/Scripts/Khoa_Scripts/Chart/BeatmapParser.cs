using UnityEngine;

public static class BeatmapParser
{
    public static bool TryLoadChart(string fileName, out ChartData chart)
    {
        chart = ChartSaveLoad.Load(fileName);

        if (chart == null)
        {
            Debug.LogError($"Failed to load chart: {fileName}");
            return false;
        }

        if (chart.notes == null)
        {
            Debug.LogError("Chart notes is null.");
            return false;
        }

        chart.notes.Sort((a, b) => a.time.CompareTo(b.time));

        Debug.Log($"Parsed chart: {chart.songName} | Notes: {chart.notes.Count}");
        return true;
    }
}