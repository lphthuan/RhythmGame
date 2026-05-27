using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Quản lý bảng xếp hạng cục bộ.
/// Implement ILeaderboardProvider và dùng ISaveProvider để lưu.
/// </summary>
public class LocalLeaderboardManager : MonoBehaviour, ILeaderboardProvider
{
    public static LocalLeaderboardManager Instance { get; private set; }

    [Header("Dependencies")]
    [Tooltip("Chọn backend để lưu leaderboard")]
    [SerializeField] private SaveBackend _backend = SaveBackend.ES3;

    private const int MAX_PER_SONG = 10;
    private ISaveProvider _provider;
    private LeaderboardData _data;

    // ─────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _provider = SaveProviderFactory.Create(_backend);
        LoadData();
    }

    private void Start()
    {
        // Subscribe vào event SaveManager
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnScoreSaved += HandleScoreSaved;
        }
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnScoreSaved -= HandleScoreSaved;
        }
    }

    private void OnApplicationPause(bool isPaused) { if (isPaused) FlushToDisk(); }
    private void OnApplicationQuit() => FlushToDisk();

    // ─────────────────────────────────────────────────────────────────────
    // ILeaderboardProvider
    // ─────────────────────────────────────────────────────────────────────

    public void AddEntry(string songGroupId, Difficulty difficulty,
                         string playerName, int score, float accuracy,
                         bool isAllPerfect = false)
    {
        string key  = SongKey.Build(songGroupId, difficulty);
        string rank = RankCalculator.Calculate(accuracy, isAllPerfect);

        var entry = new LeaderboardEntry
        {
            songKey    = key,
            playerName = playerName,
            score      = score,
            accuracy   = accuracy,
            rank       = rank,
            timestamp  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        _data.entries.Add(entry);
        TrimToTopN(key);
        FlushToDisk();
    }

    public List<LeaderboardEntry> GetTopEntries(string songGroupId, Difficulty difficulty, int count = MAX_PER_SONG)
    {
        string key = SongKey.Build(songGroupId, difficulty);
        return _data.entries
            .Where(e => e.songKey == key)
            .OrderByDescending(e => e.score)
            .ThenByDescending(e => e.accuracy)
            .ThenBy(e => e.timestamp)
            .Take(count)
            .ToList();
    }

    public int GetRank(string songGroupId, Difficulty difficulty, int score)
    {
        var top = GetTopEntries(songGroupId, difficulty);
        for (int i = 0; i < top.Count; i++)
        {
            if (score >= top[i].score) return i + 1;
        }
        return top.Count < MAX_PER_SONG ? top.Count + 1 : 0;
    }

    public bool IsTopEntry(string songGroupId, Difficulty difficulty, int score)
    {
        return GetRank(songGroupId, difficulty, score) > 0;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Internal
    // ─────────────────────────────────────────────────────────────────────

    private void HandleScoreSaved(ScoreSavedArgs args)
    {
        // Tự động add entry khi có kết quả mới
        string playerName = SaveManager.Instance.GetPlayerId();
        if (string.IsNullOrEmpty(playerName)) playerName = "Player";

        AddEntry(args.SongGroupId, args.Difficulty, playerName, args.Score, args.Accuracy, args.IsAllPerfect);
    }

    private void LoadData()
    {
        _data = _provider.Load<LeaderboardData>(SaveKeys.LEADERBOARD);
        _data.entries ??= new List<LeaderboardEntry>();
        Debug.Log($"[LocalLeaderboard] ✅ Loaded {_data.entries.Count} entries via {_provider.GetType().Name}.");
    }

    private void FlushToDisk()
    {
        if (_data != null && _provider != null)
            _provider.Save(SaveKeys.LEADERBOARD, _data);
    }

    private void TrimToTopN(string songKey)
    {
        var songEntries = _data.entries
            .Where(e => e.songKey == songKey)
            .OrderByDescending(e => e.score)
            .ThenByDescending(e => e.accuracy)
            .ToList();

        if (songEntries.Count <= MAX_PER_SONG) return;

        var toRemove = songEntries.Skip(MAX_PER_SONG);
        foreach (var entry in toRemove)
            _data.entries.Remove(entry);
    }
}
