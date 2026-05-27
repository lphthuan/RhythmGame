/// <summary>
/// Tiện ích tạo key duy nhất cho một bài nhạc theo độ khó.
///
/// Đặt trong Common/ để tất cả module đều dùng được mà không tạo circular dependency.
/// SaveManager.BuildKey() delegate về đây để backward-compatible.
///
/// Ví dụ: SongKey.Build("axium_divergence", Difficulty.Hard) → "axium_divergence_Hard"
/// </summary>
public static class SongKey
{
    /// <summary>Tạo key duy nhất: "{songGroupId}_{difficulty}"</summary>
    public static string Build(string songGroupId, Difficulty difficulty)
        => $"{songGroupId}_{difficulty}";

    /// <summary>Phân tích key thành (songGroupId, difficulty). Trả về false nếu không hợp lệ.</summary>
    public static bool TryParse(string key, out string songGroupId, out Difficulty difficulty)
    {
        songGroupId = string.Empty;
        difficulty  = Difficulty.Easy;

        if (string.IsNullOrEmpty(key)) return false;

        // Key format: "{songGroupId}_{Difficulty}"
        // Tìm đoạn suffix là tên Difficulty enum
        foreach (Difficulty d in System.Enum.GetValues(typeof(Difficulty)))
        {
            string suffix = "_" + d.ToString();
            if (key.EndsWith(suffix))
            {
                songGroupId = key.Substring(0, key.Length - suffix.Length);
                difficulty  = d;
                return !string.IsNullOrEmpty(songGroupId);
            }
        }

        return false;
    }
}
