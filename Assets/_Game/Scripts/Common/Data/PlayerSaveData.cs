using System;
using System.Collections.Generic;

/// <summary>
/// Root object của toàn bộ dữ liệu người chơi — serialize thành savegame.json.
/// Chứa settings, kết quả từng bài, và metadata đồng bộ cloud.
/// </summary>
[Serializable]
public class PlayerSaveData
{
    // ── Versioning (cho migration sau này) ─────────────────────
    /// <summary>Phiên bản cấu trúc save file. Tăng lên khi thay đổi schema.</summary>
    public int saveVersion = 1;

    // ── Định danh người chơi ───────────────────────────────────
    /// <summary>
    /// ID người chơi từ hệ thống Auth (username hoặc UUID).
    /// Trống nếu chưa đăng nhập (chế độ Guest).
    /// </summary>
    public string playerId = "";

    // ── Dữ liệu gameplay ───────────────────────────────────────
    /// <summary>Cài đặt người chơi.</summary>
    public PlayerSaveSettings settings;

    /// <summary>
    /// Danh sách kết quả của từng bài nhạc theo từng độ khó.
    /// Serialize dưới dạng List vì JsonUtility không hỗ trợ Dictionary.
    /// SaveManager tự build Dictionary cache khi load.
    /// </summary>
    public List<SongSaveData> songs;

    // ── Cloud Sync metadata ────────────────────────────────────
    /// <summary>Unix timestamp (ms) lần cuối sync thành công lên cloud.</summary>
    public long lastSyncedAt;

    /// <summary>Khởi tạo với giá trị mặc định.</summary>
    public PlayerSaveData()
    {
        settings = new PlayerSaveSettings();
        songs    = new List<SongSaveData>();
    }
}
