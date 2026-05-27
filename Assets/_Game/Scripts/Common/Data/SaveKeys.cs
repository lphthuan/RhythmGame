/// <summary>
/// Hằng số string dùng làm key trong ISaveProvider.
///
/// Mỗi key ánh xạ đến một "slot" dữ liệu:
///   JsonSaveProvider  → ánh xạ đến tên file (.json)
///   ES3SaveProvider   → ánh xạ đến key trong file .es3
///
/// Quy tắc đặt tên: "rhythm_{data_type}" để tránh xung đột với game khác trên cùng thiết bị.
/// </summary>
public static class SaveKeys
{
    /// <summary>Dữ liệu chính của người chơi: settings, điểm số, unlock status.</summary>
    public const string PLAYER_DATA = "rhythm_player_data";

    /// <summary>Bảng xếp hạng cục bộ (top 10 mỗi bài).</summary>
    public const string LEADERBOARD = "rhythm_leaderboard";

    /// <summary>Hàng đợi sync cloud khi offline.</summary>
    public const string SYNC_QUEUE  = "rhythm_sync_queue";
}
