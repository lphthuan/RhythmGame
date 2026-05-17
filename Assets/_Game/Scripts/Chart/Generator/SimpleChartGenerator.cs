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

        if (bpm <= 0f || songLength <= 0f || laneCount <= 0 || subdivision <= 0)
        {
            Debug.LogError("Invalid chart generation settings.");
            return chart;
        }

        float beatDuration = 60f / bpm;
        float step = beatDuration / subdivision;

        int previousLane = -1;
        float previousNoteTime = -999f;
        float minSameLaneInterval = beatDuration;

        for (float time = 0f; time < songLength; time += step)
        {
            if (Random.value > noteChance)
            {
                continue;
            }

            int lane = PickLane(laneCount, previousLane, time, previousNoteTime, minSameLaneInterval);

            NoteData note = new NoteData
            {
                time = time,
                lane = lane,
                type = NoteType.Tap,
                duration = 0f,
                flickDirection = FlickDirection.Any,
                slidePath = null
            };

            chart.notes.Add(note);

            previousLane = lane;
            previousNoteTime = time;
        }

        return chart;
    }

    private static int PickLane(
        int laneCount,
        int previousLane,
        float currentTime,
        float previousNoteTime,
        float minSameLaneInterval)
    {
        if (laneCount <= 1)
        {
            return 0;
        }

        int lane = Random.Range(0, laneCount);

        bool tooSoonSameLane =
            lane == previousLane &&
            currentTime - previousNoteTime < minSameLaneInterval;

        if (!tooSoonSameLane)
        {
            return lane;
        }

        int attempts = 8;

        for (int i = 0; i < attempts; i++)
        {
            lane = Random.Range(0, laneCount);

            if (lane != previousLane)
            {
                return lane;
            }
        }

        return (previousLane + 1) % laneCount;
    }
}