using System;
using UnityEngine;

public readonly struct GeneratedChartPreview
{
    public readonly int NoteCount;
    public readonly int TapCount;
    public readonly int HoldCount;
    public readonly int FlickCount;
    public readonly float StrongestOnset;
    public readonly float AverageOnset;

    public GeneratedChartPreview(int noteCount, int tapCount, int holdCount, int flickCount, float strongestOnset, float averageOnset)
    {
        NoteCount = noteCount;
        TapCount = tapCount;
        HoldCount = holdCount;
        FlickCount = flickCount;
        StrongestOnset = strongestOnset;
        AverageOnset = averageOnset;
    }
}

public static class AudioOnsetChartGenerator
{
    private const int FrameSize = 1024;
    private const int HopSize = 512;

    public static ChartData Generate(
        string songName,
        AudioClip clip,
        float bpm,
        float offset,
        float firstBeat,
        int laneCount,
        ChartDifficultyPreset difficulty,
        float density,
        float holdRatio,
        float flickRatio,
        out GeneratedChartPreview preview)
    {
        ChartData chart = new()
        {
            songName = songName,
            bpm = bpm,
            offset = offset,
            laneCount = Mathf.Clamp(laneCount, 1, 8)
        };

        preview = default;
        if (clip == null || clip.length <= 0f || bpm <= 0f)
            return chart;

        float[] onset = AnalyzeOnset(clip, out float onsetFps);
        if (onset.Length == 0 || onsetFps <= 0f)
            return chart;

        float beatDuration = 60f / bpm;
        int subdivision = GetSubdivision(difficulty);
        float stepDuration = beatDuration / subdivision;
        float startTime = Mathf.Max(0f, firstBeat + offset);
        float endTime = Mathf.Max(startTime, clip.length - 0.05f);
        float threshold = Mathf.Lerp(GetBaseThreshold(difficulty), 0.04f, Mathf.Clamp01(density));
        float spawnChance = Mathf.Lerp(GetBaseChance(difficulty), 0.98f, Mathf.Clamp01(density));
        float doubleChance = GetDoubleChance(difficulty) * Mathf.Lerp(0.45f, 1.25f, Mathf.Clamp01(density));
        float searchWindow = Mathf.Min(stepDuration * 0.48f, 0.12f);
        float minLaneGap = Mathf.Max(0.035f, stepDuration * 0.62f);
        int maxNotesPerBeat = GetMaxNotesPerBeat(difficulty, density);

        System.Random random = new(BuildSeed(songName, difficulty, bpm, laneCount));
        int previousLane = -1;
        float previousLaneTime = -999f;
        float minSameLaneInterval = beatDuration * GetSameLaneBeatInterval(difficulty);
        float[] laneBusyUntil = new float[chart.laneCount];
        int currentBeatIndex = -1;
        int notesThisBeat = 0;

        int tapCount = 0;
        int holdCount = 0;
        int flickCount = 0;
        float strongest = 0f;
        float sumStrength = 0f;
        int strengthSamples = 0;

        for (float gridTime = startTime; gridTime < endTime; gridTime += stepDuration)
        {
            float strength = GetNearestOnsetStrength(onset, onsetFps, gridTime, searchWindow);
            int stepIndex = Mathf.RoundToInt((gridTime - startTime) / stepDuration);
            int beatIndex = stepIndex / subdivision;
            int subStep = stepIndex % subdivision;

            if (beatIndex != currentBeatIndex)
            {
                currentBeatIndex = beatIndex;
                notesThisBeat = 0;
            }

            bool isStrongBeat = subStep == 0;
            bool isRhythmStep = IsAllowedRhythmStep(difficulty, subStep, beatIndex, strength, density);
            float localChance = spawnChance * GetPulseWeight(difficulty, subStep) * Mathf.Lerp(0.55f, 1.08f, strength);

            if (!isRhythmStep || notesThisBeat >= maxNotesPerBeat)
                continue;

            if (strength < threshold && !isStrongBeat && random.NextDouble() > density * 0.35f)
                continue;

            if (random.NextDouble() > localChance)
                continue;

            int lane = PickLane(random, chart.laneCount, previousLane, gridTime, previousLaneTime, minSameLaneInterval, laneBusyUntil, minLaneGap);
            if (lane < 0)
                continue;

            NoteType noteType = PickNoteType(random, holdRatio, flickRatio, strength, difficulty);
            NoteData note = AddNote(chart, gridTime, lane, noteType, beatDuration, random, difficulty);
            MarkLaneBusy(laneBusyUntil, lane, note, minLaneGap);
            CountNote(noteType, ref tapCount, ref holdCount, ref flickCount);

            previousLane = lane;
            previousLaneTime = gridTime;
            notesThisBeat++;

            if (notesThisBeat < maxNotesPerBeat && chart.laneCount > 1 && strength > 0.74f && random.NextDouble() < doubleChance)
            {
                int lane2 = PickLane(random, chart.laneCount, lane, gridTime, previousLaneTime, 0f, laneBusyUntil, minLaneGap);
                if (lane2 >= 0)
                {
                    NoteData doubleNote = AddNote(chart, gridTime, lane2, NoteType.Tap, beatDuration, random, difficulty);
                    MarkLaneBusy(laneBusyUntil, lane2, doubleNote, minLaneGap);
                    tapCount++;
                    notesThisBeat++;
                }
            }

            strongest = Mathf.Max(strongest, strength);
            sumStrength += strength;
            strengthSamples++;
        }

        chart.notes.Sort((a, b) => a.time.CompareTo(b.time));
        preview = new GeneratedChartPreview(
            chart.notes.Count,
            tapCount,
            holdCount,
            flickCount,
            strongest,
            strengthSamples > 0 ? sumStrength / strengthSamples : 0f);
        return chart;
    }

    private static float[] AnalyzeOnset(AudioClip clip, out float onsetFps)
    {
        onsetFps = clip.frequency / (float)HopSize;
        int sampleCount = clip.samples;
        if (sampleCount <= FrameSize)
            return Array.Empty<float>();

        float[] mono = GetMonoSamples(clip, sampleCount);
        int frameCount = Mathf.Max(0, (mono.Length - FrameSize) / HopSize);
        if (frameCount <= 1)
            return Array.Empty<float>();

        float[] energy = new float[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            int start = i * HopSize;
            float sum = 0f;
            for (int j = 0; j < FrameSize; j++)
            {
                float sample = mono[start + j];
                sum += sample * sample;
            }

            energy[i] = Mathf.Sqrt(sum / FrameSize);
        }

        float[] onset = new float[frameCount];
        float maxValue = 0f;
        for (int i = 1; i < frameCount; i++)
        {
            float flux = Mathf.Max(0f, energy[i] - energy[i - 1]);
            onset[i] = flux;
            maxValue = Mathf.Max(maxValue, flux);
        }

        if (maxValue > 0f)
        {
            for (int i = 0; i < onset.Length; i++)
                onset[i] /= maxValue;
        }

        Smooth(onset);
        return onset;
    }

    private static float[] GetMonoSamples(AudioClip clip, int sampleCount)
    {
        int channels = Mathf.Max(1, clip.channels);
        float[] raw = new float[sampleCount * channels];
        clip.GetData(raw, 0);

        if (channels == 1)
            return raw;

        float[] mono = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float sum = 0f;
            for (int c = 0; c < channels; c++)
                sum += raw[i * channels + c];
            mono[i] = sum / channels;
        }

        return mono;
    }

    private static void Smooth(float[] values)
    {
        if (values.Length < 3)
            return;

        float previous = values[0];
        for (int i = 1; i < values.Length - 1; i++)
        {
            float current = values[i];
            values[i] = previous * 0.2f + current * 0.6f + values[i + 1] * 0.2f;
            previous = current;
        }
    }

    private static float GetNearestOnsetStrength(float[] onset, float onsetFps, float time, float searchWindow)
    {
        int center = Mathf.RoundToInt(time * onsetFps);
        int radius = Mathf.Max(1, Mathf.RoundToInt(searchWindow * onsetFps));
        int min = Mathf.Max(0, center - radius);
        int max = Mathf.Min(onset.Length - 1, center + radius);

        float strongest = 0f;
        for (int i = min; i <= max; i++)
            strongest = Mathf.Max(strongest, onset[i]);

        return strongest;
    }

    private static NoteData AddNote(ChartData chart, float time, int lane, NoteType type, float beatDuration, System.Random random, ChartDifficultyPreset difficulty)
    {
        float maxHoldBeats = difficulty == ChartDifficultyPreset.Hard ? 1.5f : difficulty == ChartDifficultyPreset.Normal ? 1.25f : 1f;
        float duration = type == NoteType.Hold ? beatDuration * Mathf.Lerp(0.5f, maxHoldBeats, (float)random.NextDouble()) : 0f;
        NoteData note = new()
        {
            time = time,
            lane = lane,
            type = type,
            duration = duration,
            flickDirection = random.Next(0, 2) == 0 ? FlickDirection.Left : FlickDirection.Right,
            slidePath = null
        };

        chart.notes.Add(note);
        return note;
    }

    private static NoteType PickNoteType(System.Random random, float holdRatio, float flickRatio, float strength, ChartDifficultyPreset difficulty)
    {
        float hold = Mathf.Clamp01(holdRatio) * Mathf.Lerp(0.7f, 1.25f, strength);
        float flick = difficulty == ChartDifficultyPreset.Easy ? 0f : Mathf.Clamp01(flickRatio) * Mathf.Lerp(0.65f, 1.2f, strength);
        double roll = random.NextDouble();

        if (roll < hold)
            return NoteType.Hold;
        if (roll < hold + flick)
            return NoteType.Flick;
        return NoteType.Tap;
    }

    private static int PickLane(
        System.Random random,
        int laneCount,
        int previousLane,
        float time,
        float previousLaneTime,
        float minSameLaneInterval,
        float[] laneBusyUntil,
        float minLaneGap)
    {
        if (laneCount <= 1)
            return IsLaneAvailable(0, time, laneBusyUntil, minLaneGap) ? 0 : -1;

        int startLane = random.Next(0, laneCount);
        for (int attempt = 0; attempt < laneCount; attempt++)
        {
            int lane = (startLane + attempt) % laneCount;
            bool tooSoonSameLane = lane == previousLane && time - previousLaneTime < minSameLaneInterval;
            if (!tooSoonSameLane && IsLaneAvailable(lane, time, laneBusyUntil, minLaneGap))
                return lane;
        }

        return -1;
    }

    private static bool IsLaneAvailable(int lane, float time, float[] laneBusyUntil, float minLaneGap)
    {
        return lane >= 0 && lane < laneBusyUntil.Length && time >= laneBusyUntil[lane] + minLaneGap;
    }

    private static void MarkLaneBusy(float[] laneBusyUntil, int lane, NoteData note, float minLaneGap)
    {
        if (lane < 0 || lane >= laneBusyUntil.Length || note == null)
            return;

        float occupiedUntil = note.time + Mathf.Max(0f, note.duration);
        laneBusyUntil[lane] = Mathf.Max(laneBusyUntil[lane], occupiedUntil);
    }

    private static void CountNote(NoteType type, ref int tapCount, ref int holdCount, ref int flickCount)
    {
        if (type == NoteType.Hold) holdCount++;
        else if (type == NoteType.Flick) flickCount++;
        else tapCount++;
    }

    private static int BuildSeed(string songName, ChartDifficultyPreset difficulty, float bpm, int laneCount)
    {
        unchecked
        {
            int hash = 17;
            string source = songName ?? string.Empty;
            for (int i = 0; i < source.Length; i++)
                hash = hash * 31 + source[i];
            hash = hash * 31 + (int)difficulty;
            hash = hash * 31 + Mathf.RoundToInt(bpm * 10f);
            hash = hash * 31 + laneCount;
            return hash;
        }
    }

    private static int GetSubdivision(ChartDifficultyPreset difficulty)
    {
        return difficulty switch
        {
            ChartDifficultyPreset.Easy => 1,
            ChartDifficultyPreset.Hard => 4,
            _ => 2
        };
    }

    private static bool IsAllowedRhythmStep(ChartDifficultyPreset difficulty, int subStep, int beatIndex, float strength, float density)
    {
        float clampedDensity = Mathf.Clamp01(density);
        return difficulty switch
        {
            ChartDifficultyPreset.Easy => subStep == 0 && (beatIndex % 2 == 0 || strength > Mathf.Lerp(0.82f, 0.62f, clampedDensity)),
            ChartDifficultyPreset.Normal => subStep == 0 || (subStep == 1 && strength > Mathf.Lerp(0.72f, 0.46f, clampedDensity)),
            ChartDifficultyPreset.Hard => subStep == 0
                || subStep == 2
                || ((subStep == 1 || subStep == 3) && strength > Mathf.Lerp(0.78f, 0.50f, clampedDensity)),
            _ => subStep == 0
        };
    }

    private static float GetPulseWeight(ChartDifficultyPreset difficulty, int subStep)
    {
        return difficulty switch
        {
            ChartDifficultyPreset.Easy => 1f,
            ChartDifficultyPreset.Normal => subStep == 0 ? 1f : 0.58f,
            ChartDifficultyPreset.Hard => subStep == 0 ? 1f : subStep == 2 ? 0.72f : 0.42f,
            _ => 1f
        };
    }

    private static int GetMaxNotesPerBeat(ChartDifficultyPreset difficulty, float density)
    {
        float clampedDensity = Mathf.Clamp01(density);
        return difficulty switch
        {
            ChartDifficultyPreset.Easy => 1,
            ChartDifficultyPreset.Normal => clampedDensity > 0.72f ? 2 : 1,
            ChartDifficultyPreset.Hard => clampedDensity > 0.78f ? 3 : 2,
            _ => 1
        };
    }

    private static float GetBaseThreshold(ChartDifficultyPreset difficulty)
    {
        return difficulty switch
        {
            ChartDifficultyPreset.Easy => 0.42f,
            ChartDifficultyPreset.Hard => 0.25f,
            _ => 0.34f
        };
    }

    private static float GetBaseChance(ChartDifficultyPreset difficulty)
    {
        return difficulty switch
        {
            ChartDifficultyPreset.Easy => 0.34f,
            ChartDifficultyPreset.Hard => 0.62f,
            _ => 0.48f
        };
    }

    private static float GetDoubleChance(ChartDifficultyPreset difficulty)
    {
        return difficulty switch
        {
            ChartDifficultyPreset.Easy => 0.02f,
            ChartDifficultyPreset.Hard => 0.15f,
            _ => 0.07f
        };
    }

    private static float GetSameLaneBeatInterval(ChartDifficultyPreset difficulty)
    {
        return difficulty switch
        {
            ChartDifficultyPreset.Easy => 1.25f,
            ChartDifficultyPreset.Hard => 0.35f,
            _ => 0.75f
        };
    }
}
