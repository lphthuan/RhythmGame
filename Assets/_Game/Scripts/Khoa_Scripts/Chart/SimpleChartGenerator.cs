using UnityEngine;

public static class SimpleChartGenerator
{
    public static ChartData Generate(
        string songName,
        float bpm,
        float songLength,
        int laneCount,
        int subdivision,
        float noteChance)
    {
        ChartData chart = new ChartData
        {
            songName = songName,
            bpm = bpm,
            offset = 0f,
            laneCount = laneCount
        };

        float beatDuration = 60f / bpm;
        float step = beatDuration / subdivision;

        for (float time = 0f; time < songLength; time += step)
        {
            if (Random.value > noteChance)
                continue;

            NoteData note = new NoteData
            {
                time = time,
                lane = Random.Range(0, laneCount),
                type = ChartNoteType.Tap,
                duration = 0f,
                flickDirection = ChartFlickDirection.Any,
                slidePath = null
            };

            chart.notes.Add(note);
        }

        return chart;
    }
}