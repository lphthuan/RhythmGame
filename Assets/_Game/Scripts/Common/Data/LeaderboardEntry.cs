using System;

/// <summary>
/// Một entry trong bảng xếp hạng cục bộ.
/// Mỗi lần chơi xong, một entry được thêm vào leaderboard.json.
/// Top 10 per song-difficulty được giữ lại, phần còn lại bị loại.
/// </summary>
[Serializable]
public class LeaderboardEntry
{
    /// <summary>Key bài nhạc: "{songGroupId}_{difficulty}"</summary>
    public string songKey;

    /// <summary>Tên người chơi hiển thị trên bảng xếp hạng.</summary>
    public string playerName;

    /// <summary>Điểm đạt được trong lần chơi này.</summary>
    public int score;

    /// <summary>Độ chính xác (0.0 → 1.0).</summary>
    public float accuracy;

    /// <summary>Rank: "S", "A", "B", "C", "D", "F".</summary>
    public string rank;

    /// <summary>Unix timestamp (ms) lúc hoàn thành bài.</summary>
    public long timestamp;
}
