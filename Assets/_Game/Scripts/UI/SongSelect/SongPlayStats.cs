using System.Collections.Generic;
using UnityEngine;

/// <summary>Small local cache for the latest and best result shown on the song-selection screen.</summary>
public readonly struct SongPlayStats
{
    public readonly int LastScore;
    public readonly int BestScore;
    public readonly string BestRank;

    // The carousel reads this every frame for every visible card. Cache by the
    // actual ScriptableObject reference and difficulty so that scrolling does
    // not repeatedly build PlayerPrefs keys or allocate strings.
    private static readonly Dictionary<StatCacheKey, SongPlayStats> Cache = new();

    static SongPlayStats()
    {
        AccountSession.Changed += Cache.Clear;
    }

    private SongPlayStats(int lastScore, int bestScore, string bestRank)
    {
        LastScore = lastScore;
        BestScore = bestScore;
        BestRank = bestRank;
    }

    public static SongPlayStats Load(SongData song)
    {
        Difficulty difficulty = song != null ? song.difficultyLevel : Difficulty.Easy;
        return Load(song, difficulty);
    }

    public static SongPlayStats Load(SongData song, Difficulty difficulty)
    {
        if (song != null)
        {
            StatCacheKey cacheKey = new(song, difficulty);
            if (Cache.TryGetValue(cacheKey, out SongPlayStats cached))
                return cached;

            SongPlayStats loaded = LoadUncached(song, difficulty);
            Cache[cacheKey] = loaded;
            return loaded;
        }

        return LoadUncached(null, difficulty);
    }

    private static SongPlayStats LoadUncached(SongData song, Difficulty difficulty)
    {
        string key = GetKey(song, difficulty);
        return new SongPlayStats(
            PlayerPrefs.GetInt(key + ".last", 0),
            PlayerPrefs.GetInt(key + ".best", 0),
            PlayerPrefs.GetString(key + ".rank", string.Empty));
    }

    public static void Save(SongData song, GameplayResultData result)
    {
        if (song == null)
            return;

        int total = result.perfect + result.great + result.good + result.miss;
        float accuracy = total > 0 ? (result.perfect + result.great * 0.75f + result.good * 0.5f) / total : 0f;
        int score = Mathf.RoundToInt(accuracy * 1000000f);
        string rank = GetRank(accuracy);
        Difficulty difficulty = SelectedSongManager.Instance != null
            ? SelectedSongManager.Instance.SelectedDifficulty
            : song.difficultyLevel;
        string key = GetKey(song, difficulty);
        PlayerPrefs.SetInt(key + ".last", score);
        if (score >= PlayerPrefs.GetInt(key + ".best", 0))
        {
            PlayerPrefs.SetInt(key + ".best", score);
            PlayerPrefs.SetString(key + ".rank", rank);
        }
        PlayerPrefs.Save();

        Cache[new StatCacheKey(song, difficulty)] = new SongPlayStats(
            score,
            PlayerPrefs.GetInt(key + ".best", 0),
            PlayerPrefs.GetString(key + ".rank", string.Empty));
    }

    private readonly struct StatCacheKey : System.IEquatable<StatCacheKey>
    {
        private readonly SongData song;
        private readonly Difficulty difficulty;

        public StatCacheKey(SongData song, Difficulty difficulty)
        {
            this.song = song;
            this.difficulty = difficulty;
        }

        public bool Equals(StatCacheKey other) => song == other.song && difficulty == other.difficulty;
        public override bool Equals(object obj) => obj is StatCacheKey other && Equals(other);
        public override int GetHashCode() => unchecked(((song != null ? song.GetInstanceID() : 0) * 397) ^ (int)difficulty);
    }

    private static string GetKey(SongData song)
    {
        Difficulty difficulty = song != null ? song.difficultyLevel : Difficulty.Easy;
        return GetKey(song, difficulty);
    }

    private static string GetKey(SongData song, Difficulty difficulty)
    {
        string id = song != null && !string.IsNullOrWhiteSpace(song.songGroupId) ? song.songGroupId : song != null ? song.name : "unknown";
        return AccountSession.ScopedKey("SongStats." + SongData.SanitizeForFileName(id) + "." + difficulty);
    }

    private static string GetRank(float accuracy)
    {
        if (accuracy >= 0.9999f) return "SSS";
        if (accuracy >= 0.98f) return "SS";
        if (accuracy >= 0.95f) return "S";
        if (accuracy >= 0.90f) return "A";
        if (accuracy >= 0.80f) return "B";
        if (accuracy >= 0.70f) return "C";
        return "D";
    }
}
