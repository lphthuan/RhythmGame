using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton — Điểm truy cập chính cho Local Save.
/// Đã được refactor theo chuẩn SOLID (Dependency Inversion & Single Responsibility).
///
/// Chức năng:
///   • Lưu/đọc kết quả chơi (high score, accuracy, rank).
///   • Lưu/đọc PlayerSaveSettings.
///   • Quản lý save qua ISaveProvider (ES3 hoặc Json).
///   • Ủy quyền unlock cho IUnlockSystem.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Dependencies")]
    [Tooltip("Chọn backend để lưu dữ liệu (ES3 = mã hóa, Json = debug)")]
    [SerializeField] private SaveBackend _backend = SaveBackend.ES3;

    // ── Interfaces & Services ────────────────────────────────────
    private ISaveProvider _provider;
    private IUnlockSystem _unlockSystem;

    // ── Internal state ───────────────────────────────────────────
    private PlayerSaveData _data;
    private readonly Dictionary<string, SongSaveData> _songCache = new();

    // ── Events ───────────────────────────────────────────────────
    public event Action<ScoreSavedArgs> OnScoreSaved;

    // ─────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Factory khởi tạo provider
        _provider = SaveProviderFactory.Create(_backend);
        LoadData();

        // Khởi tạo Unlock System và truyền tham chiếu data để nó có thể sửa
        _unlockSystem = new UnlockSystem(_provider, _data);
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused) FlushToDisk();
    }

    private void OnApplicationQuit()
    {
        FlushToDisk();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Public API — Lưu kết quả gameplay
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lưu kết quả sau khi hoàn thành bài.
    /// Kích hoạt event OnScoreSaved để CloudSyncManager hoặc LocalLeaderboardManager xử lý.
    /// </summary>
    public bool SaveResult(string songGroupId, Difficulty difficulty,
                           int score, float accuracy, bool isAllPerfect = false)
    {
        string key  = SongKey.Build(songGroupId, difficulty);
        string rank = RankCalculator.Calculate(accuracy, isAllPerfect);
        bool   isNewHighScore = false;

        if (_songCache.TryGetValue(key, out SongSaveData existing))
        {
            if (score > existing.highScore)
            {
                existing.highScore    = score;
                existing.bestAccuracy = accuracy;
                existing.bestRank     = rank;
                isNewHighScore        = true;
            }
            existing.lastPlayedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            existing.playCount++;
        }
        else
        {
            var entry = new SongSaveData
            {
                songKey      = key,
                songGroupId  = songGroupId,
                difficulty   = difficulty.ToString(),
                isUnlocked   = true,
                highScore    = score,
                bestAccuracy = accuracy,
                bestRank     = rank,
                lastPlayedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                playCount    = 1
            };

            _songCache[key] = entry;
            _data.songs.Add(entry);
            isNewHighScore = true;
        }

        // Auto-unlock Hard nếu đạt A+ trên Medium
        if (difficulty == Difficulty.Medium && RankCalculator.MeetsHardUnlockRequirement(rank))
        {
            bool wasAlreadyUnlocked = _unlockSystem.IsUnlocked(songGroupId, Difficulty.Hard);
            _unlockSystem.Unlock(songGroupId, Difficulty.Hard);

            if (!wasAlreadyUnlocked)
                Debug.Log($"[SaveManager] 🔓 Hard unlocked: '{songGroupId}' (Medium rank {rank})");
        }

        FlushToDisk();

        // Kích hoạt event thay vì gọi trực tiếp CloudSyncManager
        OnScoreSaved?.Invoke(new ScoreSavedArgs
        {
            SongGroupId    = songGroupId,
            Difficulty     = difficulty,
            Score          = score,
            Accuracy       = accuracy,
            Rank           = rank,
            IsAllPerfect   = isAllPerfect,
            IsNewHighScore = isNewHighScore
        });

        return isNewHighScore;
    }

    public SongSaveData GetSongData(string songGroupId, Difficulty difficulty)
    {
        _songCache.TryGetValue(SongKey.Build(songGroupId, difficulty), out var data);
        return data;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Public API — Delegated to UnlockSystem
    // ─────────────────────────────────────────────────────────────────────

    public bool IsUnlocked(string songGroupId, Difficulty difficulty)
        => _unlockSystem.IsUnlocked(songGroupId, difficulty);

    public void UnlockSong(string songGroupId, Difficulty difficulty)
    {
        _unlockSystem.Unlock(songGroupId, difficulty);
        FlushToDisk(); // Ghi ra đĩa
    }

    public void PurchaseSong(string songGroupId)
    {
        _unlockSystem.PurchaseSong(songGroupId);
        // PurchaseSong trong UnlockSystem đã tự Save
    }

    public void InitializeFreeUnlocks(string songGroupId)
        => _unlockSystem.InitializeFreeUnlocks(songGroupId);

    // ─────────────────────────────────────────────────────────────────────
    // Public API — Settings
    // ─────────────────────────────────────────────────────────────────────

    public PlayerSaveSettings GetSettings()
    {
        return _data.settings ??= new PlayerSaveSettings();
    }

    public void SaveSettings(PlayerSaveSettings settings)
    {
        _data.settings = settings;
        FlushToDisk();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Public API — Player Identity
    // ─────────────────────────────────────────────────────────────────────

    public void SetPlayerId(string playerId)
    {
        _data.playerId = playerId;
        FlushToDisk();
    }

    public string GetPlayerId() => _data.playerId ?? "";

    // ─────────────────────────────────────────────────────────────────────
    // Internal — Load / Save
    // ─────────────────────────────────────────────────────────────────────

    private void LoadData()
    {
        _data = _provider.Load<PlayerSaveData>(SaveKeys.PLAYER_DATA);

        // Đảm bảo không null
        _data.settings ??= new PlayerSaveSettings();
        _data.songs    ??= new List<SongSaveData>();

        RebuildCache();
        Debug.Log($"[SaveManager] ✅ Loaded via {_provider.GetType().Name}. Player: '{_data.playerId}', Songs: {_data.songs.Count}");
    }

    private void RebuildCache()
    {
        _songCache.Clear();
        foreach (var s in _data.songs)
        {
            if (!string.IsNullOrEmpty(s.songKey))
                _songCache[s.songKey] = s;
        }
    }

    public void FlushToDisk()
    {
        if (_data == null || _provider == null) return;
        _provider.Save(SaveKeys.PLAYER_DATA, _data);
    }
}
