/// <summary>
/// Dữ liệu event khi SaveManager.SaveResult() được gọi thành công.
///
/// Đặt trong Common/Data/ để các module khác (UI, Cloud, Leaderboard) subscribe
/// mà không tạo dependency vào Save/ module.
///
/// Cách dùng:
///   SaveManager.Instance.OnScoreSaved += HandleScore;
///   void HandleScore(ScoreSavedArgs args) { ... }
/// </summary>
[System.Serializable]
public class ScoreSavedArgs
{
    public string    SongGroupId;
    public Difficulty Difficulty;
    public int       Score;
    public float     Accuracy;     // 0.0 → 1.0
    public string    Rank;         // "S", "A", "B", "C", "D", "F"
    public bool      IsAllPerfect;
    public bool      IsNewHighScore;
}
