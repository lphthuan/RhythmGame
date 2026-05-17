using UnityEngine;

public class ChartGeneratorTester : MonoBehaviour
{
    [Header("Generation Settings")]
    [SerializeField] private ChartGenerationSettings generationSettings;

    [Header("Visualizer")]
    [SerializeField] private ChartVisualizer visualizer;

    [Header("Song Info")]
    [SerializeField] private string songName = "Test Song";
    [SerializeField] private float bpm = 120f;
    [SerializeField] private float songLength = 30f;
    [SerializeField] private AudioSource musicSource;

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
        if (musicSource != null && musicSource.clip != null)
        {
            songLength = musicSource.clip.length;
            songName = musicSource.clip.name;

            Debug.Log($"Using AudioClip: {songName} | Length: {songLength:F2}s");
        }
        else
        {
            Debug.LogWarning("Music Source or AudioClip is missing. Using manual song length.");
        }
        if (generationSettings != null)
        {
            generationSettings.ApplyPreset();

            laneCount = generationSettings.laneCount;
            subdivision = generationSettings.subdivision;
            noteChance = generationSettings.noteChance;
        }
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
        var spawnDataList = ChartRuntimeConverter.ConvertToSpawnData(loadedChart);

        if (spawnDataList.Count > 0)
        {
            Debug.Log($"First Spawn Data: ID = {spawnDataList[0].noteId} | Time = {spawnDataList[0].hitTime:F2} | Lane = {spawnDataList[0].laneIndex} | Type = {spawnDataList[0].noteType}");
        }
        if (ChartSpawnDataProvider.TryGetSpawnData(saveFileName, out var providerSpawnData))
        {
            Debug.Log($"Provider OK. Spawn Data Count: {providerSpawnData.Count}");
            ChartDebugPrinter.PrintSpawnData(providerSpawnData, 10);
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