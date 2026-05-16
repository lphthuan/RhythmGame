using UnityEngine;

public class ChartGeneratorTester : MonoBehaviour
{
    [Header("Visualizer")]
    [SerializeField] private ChartVisualizer visualizer;

    [Header("Song Info")]
    [SerializeField] private string songName = "Test Song";
    [SerializeField] private float bpm = 120f;
    [SerializeField] private float songLength = 30f;

    [Header("Chart Settings")]
    [SerializeField] private int laneCount = 4;
    [SerializeField] private int subdivision = 2;
    [Range(0f, 1f)]
    [SerializeField] private float noteChance = 0.5f;

    [Header("Save")]
    [SerializeField] private string saveFileName = "test_chart";

    private ChartData generatedChart;

    private void Start()
    {
        generatedChart = SimpleChartGenerator.Generate(
            songName,
            bpm,
            songLength,
            laneCount,
            subdivision,
            noteChance
        );

        Debug.Log($"Generated chart: {generatedChart.songName}");
        Debug.Log($"Total Notes Before Save: {generatedChart.notes.Count}");

        ChartSaveLoad.Save(generatedChart, saveFileName);

        ChartSaveLoad.Save(generatedChart, saveFileName);

        if (!BeatmapParser.TryLoadChart(saveFileName, out ChartData loadedChart))
        {
            Debug.LogError("Load chart failed.");
            return;
        }

        Debug.Log($"Loaded chart: {loadedChart.songName}");
        Debug.Log($"Total Notes After Load: {loadedChart.notes.Count}");

        if (loadedChart.notes.Count > 0)
        {
            Debug.Log($"First note time: {loadedChart.notes[0].time:F2}");
        }

        if (visualizer != null)
        {
            visualizer.Draw(loadedChart);
        }

        PrintFirstNotes(loadedChart, 10);
    }

    private void PrintFirstNotes(ChartData chart, int count)
    {
        int max = Mathf.Min(count, chart.notes.Count);

        for (int i = 0; i < max; i++)
        {
            NoteData note = chart.notes[i];
            Debug.Log($"Note {i}: Time = {note.time:F2}s | Lane = {note.lane}");
        }
    }
}