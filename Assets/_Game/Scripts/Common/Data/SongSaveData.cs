using System;

/// <summary>
/// Dữ liệu lưu của một bài nhạc theo từng độ khó.
/// Key duy nhất = songKey = "{songGroupId}_{difficulty}" (e.g. "axium_divergence_Hard").
/// </summary>
[Serializable]
public class SongSaveData
{
    // ── Định danh ──────────────────────────────────────────────
    /// <summary>Key duy nhất: "{songGroupId}_{difficulty}"</summary>
    public string songKey;

    /// <summary>ID nhóm bài (giống nhau cho tất cả difficulty của cùng 1 bài nhạc).</summary>
    public string songGroupId;

    /// <summary>Độ khó dạng string: "Easy", "Medium", "Hard".</summary>
    public string difficulty;

    // ── Kết quả tốt nhất ───────────────────────────────────────
    /// <summary>Điểm cao nhất đạt được.</summary>
    public int highScore;

    /// <summary>Độ chính xác tốt nhất (0.0 → 1.0).</summary>
    public float bestAccuracy;

    /// <summary>Rank tốt nhất: "S", "A", "B", "C", "D", "F". Rỗng nếu chưa chơi.</summary>
    public string bestRank;

    // ── Trạng thái mở khóa ─────────────────────────────────────
    /// <summary>true nếu người chơi có thể chơi bài này.</summary>
    public bool isUnlocked;

    /// <summary>true nếu đã mua trong Shop (không bị mất khi reset progress).</summary>
    public bool isPurchased;

    // ── Thống kê ───────────────────────────────────────────────
    /// <summary>Unix timestamp (ms) lần chơi gần nhất.</summary>
    public long lastPlayedAt;

    /// <summary>Số lần đã chơi bài này.</summary>
    public int playCount;
}
