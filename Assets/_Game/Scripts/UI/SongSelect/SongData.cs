using UnityEngine;
using Dypsloom.RhythmTimeline.Core;

[CreateAssetMenu(fileName = "NewSong", menuName = "Data/SongData")]
public class SongData : ScriptableObject
{
    public string _songTitle;
    public string _sceneName;
    public Sprite _previewImage;
    public float  _bpm;

    // ── Legacy (giữ để không break SO cũ) ──────────────────────
    [Tooltip("[Legacy] Chuỗi độ khó tự do. Dùng difficultyLevel bên dưới cho hệ thống mới.")]
    public string _difficulty = "Normal";

    // ── Save & Unlock System ────────────────────────────────────
    [Header("Save & Unlock")]

    [Tooltip(
        "ID nhóm bài — phải GIỐNG NHAU cho tất cả difficulty của cùng 1 bài nhạc.\n" +
        "Dùng lowercase, underscore. Ví dụ: 'axium_divergence'\n" +
        "Đây là chuỗi được dùng làm khóa lưu điểm.")]
    public string songGroupId;

    [Tooltip("Độ khó của chart này (Easy / Medium / Hard).")]
    public Difficulty difficultyLevel = Difficulty.Easy;

    [Tooltip(
        "Free: Easy + Medium mở mặc định khi cài game.\n" +
        "Purchase: Cần mua trong Shop → mở Easy + Medium.")]
    public SongUnlockType unlockType = SongUnlockType.Free;

    [Header("Chart Integration")]
    [Tooltip("Tên file JSON chart không kèm .json. Nếu trống sẽ tự tính từ AudioClip để giữ tương thích SongData cũ.")]
    public string chartFileName;

    [Tooltip("AudioClip của bài nhạc này.")]
    public AudioClip audioClip;

    [Tooltip("Timeline dùng để chỉnh chart của bài này. Nút Open Timeline trong Inspector sẽ mở trực tiếp asset này.")]
    public RhythmTimelineAsset timelineAsset;

    [Header("Difficulty Charts")]
    [Tooltip("Tên file JSON chart Easy. Để trống sẽ dùng Chart File Name legacy.")]
    public string easyChartFileName;
    [Tooltip("Tên file JSON chart Normal. Để trống sẽ dùng Chart File Name legacy.")]
    public string normalChartFileName;
    [Tooltip("Tên file JSON chart Hard. Để trống sẽ dùng Chart File Name legacy.")]
    public string hardChartFileName;

    [Header("Difficulty Timelines")]
    public RhythmTimelineAsset easyTimelineAsset;
    public RhythmTimelineAsset normalTimelineAsset;
    public RhythmTimelineAsset hardTimelineAsset;

    /// <summary>
    /// Tên file JSON chart, tự sinh từ audioClip.name.
    /// Ví dụ: "Axium Divergence" → "chart_axium_divergence"
    /// Không cần nhập tay — chỉ cần gán đúng AudioClip.
    /// </summary>
    public string ComputedChartFileName
    {
        get
        {
            return GetChartFileName(difficultyLevel);
        }
    }

    public string GetChartFileName(Difficulty difficulty)
    {
        string difficultyChart = difficulty switch
        {
            Difficulty.Easy => easyChartFileName,
            Difficulty.Medium => normalChartFileName,
            Difficulty.Hard => hardChartFileName,
            _ => string.Empty
        };

        if (!string.IsNullOrWhiteSpace(difficultyChart)) return difficultyChart;
        if (!string.IsNullOrWhiteSpace(chartFileName)) return chartFileName;
        if (audioClip == null) return string.Empty;
        return "chart_" + SanitizeForFileName(audioClip.name);
    }

    public RhythmTimelineAsset GetTimelineAsset(Difficulty difficulty)
    {
        RhythmTimelineAsset difficultyTimeline = difficulty switch
        {
            Difficulty.Easy => easyTimelineAsset,
            Difficulty.Medium => normalTimelineAsset,
            Difficulty.Hard => hardTimelineAsset,
            _ => null
        };

        return difficultyTimeline != null ? difficultyTimeline : timelineAsset;
    }

    public string SongTitle    => _songTitle;
    public string SceneName    => _sceneName;
    public Sprite PreviewImage => _previewImage;

    // ─── Helper ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Chuyển tên file nhạc thành tên file JSON hợp lệ.
    /// Lowercase, space/dash → underscore, ký tự đặc biệt bị bỏ.
    /// Recommend tên file nhạc tối đa 30 ký tự.
    /// </summary>
    public static string SanitizeForFileName(string input)
    {
        if (string.IsNullOrEmpty(input)) return "unknown";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (char c in input.ToLower())
        {
            if (char.IsLetterOrDigit(c))   sb.Append(c);
            else if (c == ' ' || c == '-') sb.Append('_');
            // Bỏ qua các ký tự khác: (, ), +, ., !, ...
        }

        string result = sb.ToString();
        while (result.Contains("__"))
            result = result.Replace("__", "_");
        return result.Trim('_');
    }
}
