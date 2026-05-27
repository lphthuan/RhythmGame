using UnityEngine;

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
    [Tooltip("AudioClip của bài nhạc này.")]
    public AudioClip audioClip;

    /// <summary>
    /// Tên file JSON chart, tự sinh từ audioClip.name.
    /// Ví dụ: "Axium Divergence" → "chart_axium_divergence"
    /// Không cần nhập tay — chỉ cần gán đúng AudioClip.
    /// </summary>
    public string ComputedChartFileName
    {
        get
        {
            if (audioClip == null) return string.Empty;
            return "chart_" + SanitizeForFileName(audioClip.name);
        }
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