using System.Collections.Generic;
using Dypsloom.RhythmTimeline.Core;
using Dypsloom.RhythmTimeline.Core.Playables;
using UnityEngine;
using UnityEngine.Timeline;

public static class ChartSpawnDataProvider
{
    public static bool TryGetSpawnData(string fileName, out List<ChartNoteSpawnData> spawnDataList)
    {
        spawnDataList = new List<ChartNoteSpawnData>();

        if (!BeatmapParser.TryLoadChart(fileName, out ChartData loadedChart))
        {
            Debug.LogError($"Failed to get spawn data. Cannot load chart: {fileName}");
            return false;
        }

        spawnDataList = ChartRuntimeConverter.ConvertToSpawnData(loadedChart);

        if (spawnDataList.Count == 0)
        {
            Debug.LogWarning($"Spawn data is empty: {fileName}");
            return false;
        }

        Debug.Log($"Spawn data ready. File: {fileName} | Count: {spawnDataList.Count}");
        return true;
    }
    public static bool TryGetChartAndSpawnData(
    string fileName,
    out ChartData loadedChart,
    out List<ChartNoteSpawnData> spawnDataList)
    {
        loadedChart = null;
        spawnDataList = new List<ChartNoteSpawnData>();

        if (!BeatmapParser.TryLoadChart(fileName, out loadedChart))
        {
            Debug.LogError($"Failed to get chart and spawn data. Cannot load chart: {fileName}");
            return false;
        }

        spawnDataList = ChartRuntimeConverter.ConvertToSpawnData(loadedChart);

        if (spawnDataList.Count == 0)
        {
            Debug.LogWarning($"Spawn data is empty: {fileName}");
            return false;
        }

        Debug.Log($"Chart and spawn data ready. File: {fileName} | Count: {spawnDataList.Count}");
        return true;
    }

    public static bool TryGetChartAndSpawnData(
        RhythmTimelineAsset timeline,
        out ChartData loadedChart,
        out List<ChartNoteSpawnData> spawnDataList)
    {
        loadedChart = ExportTimelineToChart(timeline);
        spawnDataList = new List<ChartNoteSpawnData>();

        if (loadedChart == null)
        {
            Debug.LogError("Failed to get chart and spawn data. Timeline is null.");
            return false;
        }

        spawnDataList = ChartRuntimeConverter.ConvertToSpawnData(loadedChart);

        if (spawnDataList.Count == 0)
        {
            Debug.LogWarning($"Spawn data is empty: {timeline.name}");
            return false;
        }

        Debug.Log($"Chart and spawn data ready. Timeline: {timeline.name} | Count: {spawnDataList.Count}");
        return true;
    }

    private static ChartData ExportTimelineToChart(RhythmTimelineAsset timeline)
    {
        if (timeline == null)
            return null;

        ChartData chart = new ChartData
        {
            songName = string.IsNullOrWhiteSpace(timeline.FullName) ? timeline.name : timeline.FullName,
            bpm = timeline.Bpm > 0f ? timeline.Bpm : 120f,
            offset = 0f,
            laneCount = 0
        };

        foreach (TrackAsset trackAsset in timeline.GetOutputTracks())
        {
            if (trackAsset is not RhythmTrack rhythmTrack)
                continue;

            int lane = Mathf.Max(0, rhythmTrack.ID);
            chart.laneCount = Mathf.Max(chart.laneCount, lane + 1);

            foreach (TimelineClip clip in rhythmTrack.GetClips())
            {
                if (clip.asset is not RhythmClip rhythmClip)
                    continue;

                NoteType noteType = GetNoteType(rhythmClip);
                NoteData note = new NoteData
                {
                    time = Mathf.Max(0f, (float)clip.start),
                    lane = lane,
                    type = noteType,
                    duration = GetDuration(noteType, clip),
                    flickDirection = FlickDirection.Any
                };

                ChartTimelineMetadata.Decode(rhythmClip.ClipParameters.StringParameter, note);
                note.duration = GetDuration(note.type, clip);
                chart.notes.Add(note);
            }
        }

        chart.notes.Sort((a, b) => a.time.CompareTo(b.time));
        return chart;
    }

    private static NoteType GetNoteType(RhythmClip rhythmClip)
    {
        int value = rhythmClip.ClipParameters.IntParameter;
        return System.Enum.IsDefined(typeof(NoteType), value)
            ? (NoteType)value
            : NoteType.Tap;
    }

    private static float GetDuration(NoteType noteType, TimelineClip clip)
    {
        if (noteType != NoteType.Hold && noteType != NoteType.Slide)
            return 0f;

        return Mathf.Max(0f, (float)clip.duration);
    }
}
