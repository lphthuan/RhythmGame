using System;

/// <summary>
/// Cài đặt người chơi — lưu cùng PlayerSaveData.
/// Tất cả giá trị đều có default hợp lý để dùng ngay khi lần đầu cài game.
/// </summary>
[Serializable]
public class PlayerSaveSettings
{
    // ── Âm thanh ───────────────────────────────────────────────
    /// <summary>Âm lượng nhạc nền (0.0 → 1.0).</summary>
    public float musicVolume = 1f;

    /// <summary>Âm lượng hiệu ứng âm thanh (0.0 → 1.0).</summary>
    public float sfxVolume = 1f;

    // ── Gameplay ───────────────────────────────────────────────
    /// <summary>Tốc độ note rơi xuống (đơn vị game units/s). Điển hình 5–15.</summary>
    public float noteSpeed = 7f;

    /// <summary>
    /// Độ trễ input (milliseconds). Giá trị dương = nhấn sớm hơn thực tế.
    /// Điều chỉnh để bù lag âm thanh của thiết bị.
    /// </summary>
    public int inputOffsetMs = 0;

    // ── Giao diện ──────────────────────────────────────────────
    /// <summary>Hiển thị FPS trên màn hình gameplay.</summary>
    public bool showFPS = false;

    /// <summary>Bật rung khi đánh note (chỉ có tác dụng trên thiết bị hỗ trợ).</summary>
    public bool vibration = true;
}
