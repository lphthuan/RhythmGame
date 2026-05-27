using System.Collections.Generic;

/// <summary>
/// Abstraction cho bảng xếp hạng cục bộ (ISP — Interface Segregation).
///
/// Tách riêng khỏi ISaveProvider để:
///   1. Consumer chỉ cần inject ILeaderboardProvider, không cần biết cách lưu
///   2. Có thể swap implementation (LocalLeaderboard / RemoteLeaderboard)
///   3. LeaderboardUI chỉ phụ thuộc vào interface, không phụ thuộc concrete class
///
/// LocalLeaderboardManager implement interface này.
/// </summary>
public interface ILeaderboardProvider
{
    /// <summary>
    /// Thêm một kết quả chơi vào bảng xếp hạng.
    /// Tự động duy trì top 10 per song-difficulty.
    /// </summary>
    void AddEntry(string songGroupId, Difficulty difficulty,
                  string playerName, int score, float accuracy,
                  bool isAllPerfect = false);

    /// <summary>
    /// Lấy top N entries của bài-độ khó, sắp xếp điểm cao nhất trước.
    /// </summary>
    List<LeaderboardEntry> GetTopEntries(string songGroupId, Difficulty difficulty, int count = 10);

    /// <summary>
    /// Vị trí xếp hạng của một điểm số (1-indexed).
    /// Trả về 0 nếu không lọt top.
    /// </summary>
    int GetRank(string songGroupId, Difficulty difficulty, int score);

    /// <summary>Kiểm tra điểm có lọt top 10 không.</summary>
    bool IsTopEntry(string songGroupId, Difficulty difficulty, int score);
}
