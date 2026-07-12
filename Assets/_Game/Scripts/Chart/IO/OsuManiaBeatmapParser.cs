using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class OsuManiaBeatmapParser
{
    public readonly struct ImportResult
    {
        public readonly ChartData Chart;
        public readonly string AudioFileName;
        public readonly string AudioFilePath;
        public readonly string BackgroundFileName;
        public readonly string BackgroundFilePath;
        public readonly string Title;
        public readonly string Artist;
        public readonly string Version;
        public readonly int LaneCount;

        public ImportResult(
            ChartData chart,
            string audioFileName,
            string audioFilePath,
            string backgroundFileName,
            string backgroundFilePath,
            string title,
            string artist,
            string version,
            int laneCount)
        {
            Chart = chart;
            AudioFileName = audioFileName;
            AudioFilePath = audioFilePath;
            BackgroundFileName = backgroundFileName;
            BackgroundFilePath = backgroundFilePath;
            Title = title;
            Artist = artist;
            Version = version;
            LaneCount = laneCount;
        }
    }

    private enum Section
    {
        None,
        General,
        Metadata,
        Difficulty,
        TimingPoints,
        HitObjects,
        Events
    }

    public static bool TryParse(string osuFilePath, out ImportResult result, out string error)
    {
        result = default;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(osuFilePath) || !File.Exists(osuFilePath))
        {
            error = $"osu! beatmap file not found: {osuFilePath}";
            return false;
        }

        string directory = Path.GetDirectoryName(osuFilePath);
        string audioFileName = string.Empty;
        string backgroundFileName = string.Empty;
        string title = Path.GetFileNameWithoutExtension(osuFilePath);
        string artist = string.Empty;
        string version = string.Empty;
        int mode = -1;
        int laneCount = 4;
        float bpm = 120f;
        bool hasBpm = false;
        List<NoteData> notes = new List<NoteData>();

        Section section = Section.None;
        string[] lines = File.ReadAllLines(osuFilePath);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = StripComment(lines[i]).Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("[", StringComparison.Ordinal) &&
                line.EndsWith("]", StringComparison.Ordinal))
            {
                section = ParseSection(line);
                continue;
            }

            switch (section)
            {
                case Section.General:
                    ReadGeneral(line, ref audioFileName, ref mode);
                    break;

                case Section.Metadata:
                    ReadMetadata(line, ref title, ref artist, ref version);
                    break;

                case Section.Difficulty:
                    ReadDifficulty(line, ref laneCount);
                    break;

                case Section.TimingPoints:
                    if (!hasBpm && TryReadBpm(line, out float timingBpm))
                    {
                        bpm = timingBpm;
                        hasBpm = true;
                    }
                    break;

                case Section.Events:
                    ReadEvents(line, ref backgroundFileName);
                    break;

                case Section.HitObjects:
                    if (TryReadNote(line, laneCount, out NoteData note))
                        notes.Add(note);
                    break;
            }
        }

        if (mode != 3)
        {
            error = $"Beatmap is not osu!mania. Expected Mode: 3, got Mode: {mode}.";
            return false;
        }

        if (notes.Count == 0)
        {
            error = "No osu!mania notes found in [HitObjects].";
            return false;
        }

        notes.Sort((a, b) => a.time.CompareTo(b.time));

        string displayTitle = string.IsNullOrWhiteSpace(artist)
            ? title
            : $"{artist} - {title}";

        if (!string.IsNullOrWhiteSpace(version))
            displayTitle = $"{displayTitle} [{version}]";

        ChartData chart = new ChartData
        {
            songName = displayTitle,
            bpm = bpm,
            offset = 0f,
            laneCount = laneCount,
            notes = notes
        };

        string audioPath = string.IsNullOrWhiteSpace(audioFileName) || string.IsNullOrWhiteSpace(directory)
            ? string.Empty
            : Path.Combine(directory, audioFileName);
        string backgroundPath = string.IsNullOrWhiteSpace(backgroundFileName) || string.IsNullOrWhiteSpace(directory)
            ? string.Empty
            : Path.Combine(directory, backgroundFileName);

        result = new ImportResult(
            chart,
            audioFileName,
            audioPath,
            backgroundFileName,
            backgroundPath,
            title,
            artist,
            version,
            laneCount);

        return true;
    }

    private static void ReadGeneral(string line, ref string audioFileName, ref int mode)
    {
        if (!TrySplitKeyValue(line, out string key, out string value))
            return;

        if (key.Equals("AudioFilename", StringComparison.OrdinalIgnoreCase))
            audioFileName = value;
        else if (key.Equals("Mode", StringComparison.OrdinalIgnoreCase))
            mode = ParseInt(value, mode);
    }

    private static void ReadEvents(string line, ref string backgroundFileName)
    {
        if (!string.IsNullOrWhiteSpace(backgroundFileName))
            return;

        string[] fields = line.Split(',');
        if (fields.Length < 3)
            return;

        if (fields[0].Trim() != "0")
            return;

        backgroundFileName = fields[2].Trim().Trim('"');
    }

    private static void ReadMetadata(
        string line,
        ref string title,
        ref string artist,
        ref string version)
    {
        if (!TrySplitKeyValue(line, out string key, out string value))
            return;

        if (key.Equals("TitleUnicode", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(value))
            title = value;
        else if (key.Equals("Title", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(title))
            title = value;
        else if (key.Equals("ArtistUnicode", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(value))
            artist = value;
        else if (key.Equals("Artist", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(artist))
            artist = value;
        else if (key.Equals("Version", StringComparison.OrdinalIgnoreCase))
            version = value;
    }

    private static void ReadDifficulty(string line, ref int laneCount)
    {
        if (!TrySplitKeyValue(line, out string key, out string value))
            return;

        if (!key.Equals("CircleSize", StringComparison.OrdinalIgnoreCase))
            return;

        laneCount = Mathf.Clamp(ParseInt(value, laneCount), 1, 18);
    }

    private static bool TryReadBpm(string line, out float bpm)
    {
        bpm = 120f;
        string[] fields = line.Split(',');
        if (fields.Length < 2)
            return false;

        float beatLength = ParseFloat(fields[1], 0f);
        if (beatLength <= 0f)
            return false;

        bpm = 60000f / beatLength;
        return bpm > 0f;
    }

    private static bool TryReadNote(string line, int laneCount, out NoteData note)
    {
        note = null;

        string[] fields = line.Split(',');
        if (fields.Length < 5)
            return false;

        int x = ParseInt(fields[0], 0);
        int timeMs = ParseInt(fields[2], 0);
        int type = ParseInt(fields[3], 0);

        bool isHold = (type & 128) != 0;
        bool isTap = (type & 1) != 0 || !isHold;
        if (!isTap && !isHold)
            return false;

        int lane = Mathf.Clamp(Mathf.FloorToInt(x * laneCount / 512f), 0, laneCount - 1);
        float startTime = timeMs / 1000f;

        note = new NoteData
        {
            time = startTime,
            lane = lane,
            type = isHold ? NoteType.Hold : NoteType.Tap,
            duration = 0f,
            flickDirection = FlickDirection.Any,
            slidePath = Array.Empty<int>()
        };

        if (!isHold || fields.Length < 6)
            return true;

        string[] holdParts = fields[5].Split(':');
        int endTimeMs = holdParts.Length > 0 ? ParseInt(holdParts[0], timeMs) : timeMs;
        note.duration = Mathf.Max(0.05f, (endTimeMs - timeMs) / 1000f);
        return true;
    }

    private static Section ParseSection(string line)
    {
        string value = line.Trim('[', ']');
        return value switch
        {
            "General" => Section.General,
            "Metadata" => Section.Metadata,
            "Difficulty" => Section.Difficulty,
            "TimingPoints" => Section.TimingPoints,
            "HitObjects" => Section.HitObjects,
            "Events" => Section.Events,
            _ => Section.None
        };
    }

    private static bool TrySplitKeyValue(string line, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        int index = line.IndexOf(':');
        if (index < 0)
            return false;

        key = line[..index].Trim();
        value = line[(index + 1)..].Trim();
        return true;
    }

    private static string StripComment(string line)
    {
        int index = line.IndexOf("//", StringComparison.Ordinal);
        return index >= 0 ? line[..index] : line;
    }

    private static int ParseInt(string value, int fallback)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
            ? result
            : fallback;
    }

    private static float ParseFloat(string value, float fallback)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)
            ? result
            : fallback;
    }
}
