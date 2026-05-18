using UnityEngine;

public class ChartGeneratorTool : MonoBehaviour
{
    [Header("Generation")]
    [SerializeField] private ChartGenerationSettings generationSettings;
    [SerializeField] private AudioSource musicSource;

    [Header("Save")]
    [SerializeField] private string saveFileName = "test_chart";

    [Header("Preview Optional")]
    [SerializeField] private ChartVisualizer visualizer;

    public void GenerateAndSave()
    {
        if (generationSettings == null)
        {
            Debug.LogError("Generation settings is missing.");
            return;
        }

        if (musicSource == null || musicSource.clip == null)
        {
            Debug.LogError("Music source or AudioClip is missing.");
            return;
        }

        generationSettings.ApplyPreset();

        string songName = musicSource.clip.name;
        float songLength = musicSource.clip.length;

        ChartData chart = SimpleChartGenerator.Generate(
            songName,
            generationSettings.bpm,
            songLength,
            generationSettings.offset,
            generationSettings.laneCount,
            generationSettings.subdivision,
            generationSettings.noteChance
        );

        ChartSaveLoad.Save(chart, saveFileName);

        Debug.Log($"Generated and saved chart: {chart.songName} | Notes: {chart.notes.Count}");

        if (visualizer != null)
        {
            visualizer.Draw(chart);
        }
    }

    public void LoadAndPreview()
    {
        if (visualizer == null)
        {
            Debug.LogError("Visualizer is missing.");
            return;
        }

        if (!BeatmapParser.TryLoadChart(saveFileName, out ChartData loadedChart))
        {
            Debug.LogError($"Failed to load chart preview: {saveFileName}");
            return;
        }

        visualizer.Draw(loadedChart);

        Debug.Log($"Loaded preview chart: {loadedChart.songName} | Notes: {loadedChart.notes.Count}");
    }

    public void AutoDetectBpm()
    {
        if (musicSource == null || musicSource.clip == null)
        {
            Debug.LogError("ChartGeneratorTool: Music source or AudioClip is missing.");
            return;
        }

        if (generationSettings == null)
        {
            Debug.LogError("ChartGeneratorTool: Generation Settings is missing.");
            return;
        }

        float detectedBpm = BpmDetector.Detect(musicSource.clip);
        generationSettings.bpm = detectedBpm;

        Debug.Log($"ChartGeneratorTool: Auto-detected BPM = {detectedBpm:F1} " +
                  $"và đã điền vào Generation Settings.");
    }

    public void SaveEditedChart()
    {
        if (visualizer == null)
        {
            Debug.LogError("Visualizer is missing.");
            return;
        }

        ChartData chart = visualizer.GetCurrentChart();

        if (chart == null)
        {
            Debug.LogWarning("No chart loaded to save.");
            return;
        }

        ChartSaveLoad.Save(chart, saveFileName);

        Debug.Log($"Saved edited chart: {chart.songName} | Notes: {chart.notes.Count}");
    }
}