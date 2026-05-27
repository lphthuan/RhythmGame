using UnityEngine;

/// <summary>
/// Hệ thống xử lý logic mở khóa bài nhạc (SRP).
/// Tách khỏi SaveManager để dễ maintain và test độc lập.
/// </summary>
public class UnlockSystem : IUnlockSystem
{
    private readonly ISaveProvider _provider;
    private PlayerSaveData _dataCache; // Tham chiếu đến _data của SaveManager

    public UnlockSystem(ISaveProvider provider, PlayerSaveData dataCache)
    {
        _provider = provider;
        _dataCache = dataCache;
    }

    /// <inheritdoc/>
    public bool IsUnlocked(string songGroupId, Difficulty difficulty)
    {
        string key = SongKey.Build(songGroupId, difficulty);
        var song = _dataCache.songs.Find(s => s.songKey == key);
        return song != null && song.isUnlocked;
    }

    /// <inheritdoc/>
    public void Unlock(string songGroupId, Difficulty difficulty)
    {
        string key = SongKey.Build(songGroupId, difficulty);
        var song = _dataCache.songs.Find(s => s.songKey == key);

        if (song != null)
        {
            song.isUnlocked = true;
        }
        else
        {
            _dataCache.songs.Add(new SongSaveData
            {
                songKey = key,
                songGroupId = songGroupId,
                difficulty = difficulty.ToString(),
                isUnlocked = true,
                bestRank = "",
                playCount = 0
            });
        }
    }

    /// <inheritdoc/>
    public void PurchaseSong(string songGroupId)
    {
        MarkAsPurchased(songGroupId, Difficulty.Easy);
        MarkAsPurchased(songGroupId, Difficulty.Medium);

        // Lưu lại bằng provider
        _provider.Save(SaveKeys.PLAYER_DATA, _dataCache);
        Debug.Log($"[UnlockSystem] 🛒 Purchased '{songGroupId}' — Easy & Medium unlocked.");
    }

    /// <inheritdoc/>
    public void InitializeFreeUnlocks(string songGroupId)
    {
        EnsureEntryExists(songGroupId, Difficulty.Easy, isUnlocked: true);
        EnsureEntryExists(songGroupId, Difficulty.Medium, isUnlocked: true);
        EnsureEntryExists(songGroupId, Difficulty.Hard, isUnlocked: false); // Skill-locked
    }

    // ── Internal Helpers ─────────────────────────────────────────

    private void MarkAsPurchased(string songGroupId, Difficulty difficulty)
    {
        string key = SongKey.Build(songGroupId, difficulty);
        var song = _dataCache.songs.Find(s => s.songKey == key);

        if (song != null)
        {
            song.isUnlocked = true;
            song.isPurchased = true;
        }
        else
        {
            _dataCache.songs.Add(new SongSaveData
            {
                songKey = key,
                songGroupId = songGroupId,
                difficulty = difficulty.ToString(),
                isUnlocked = true,
                isPurchased = true,
                bestRank = "",
                playCount = 0
            });
        }
    }

    private void EnsureEntryExists(string songGroupId, Difficulty difficulty, bool isUnlocked)
    {
        string key = SongKey.Build(songGroupId, difficulty);
        var song = _dataCache.songs.Find(s => s.songKey == key);

        if (song == null)
        {
            _dataCache.songs.Add(new SongSaveData
            {
                songKey = key,
                songGroupId = songGroupId,
                difficulty = difficulty.ToString(),
                isUnlocked = isUnlocked,
                bestRank = "",
                playCount = 0
            });
        }
    }
}
