/// <summary>
/// Tính Rank từ accuracy và trạng thái All Perfect.
///
/// Thang xếp hạng:
///   S  — All Perfect hoặc accuracy ≥ 95%
///   A  — accuracy ≥ 90%
///   B  — accuracy ≥ 80%
///   C  — accuracy ≥ 70%
///   D  — accuracy ≥ 60%
///   F  — accuracy &lt; 60%
///
/// Dùng bởi: SaveManager, LocalLeaderboardManager, ResultUI.
/// </summary>
public static class RankCalculator
{
    // ── Ngưỡng rank (có thể cấu hình qua Config/ sau này) ──────
    private const float RANK_S_THRESHOLD = 0.95f;
    private const float RANK_A_THRESHOLD = 0.90f;
    private const float RANK_B_THRESHOLD = 0.80f;
    private const float RANK_C_THRESHOLD = 0.70f;
    private const float RANK_D_THRESHOLD = 0.60f;

    // ── Ngưỡng mở khóa Hard (phải đạt A trở lên trên Medium) ───
    private const float HARD_UNLOCK_MIN_ACCURACY = RANK_A_THRESHOLD;

    /// <summary>
    /// Tính rank từ accuracy (0.0→1.0) và cờ All Perfect.
    /// </summary>
    /// <param name="accuracy">Độ chính xác, range [0.0, 1.0].</param>
    /// <param name="isAllPerfect">True nếu toàn bộ note đều Perfect.</param>
    /// <returns>"S", "A", "B", "C", "D", hoặc "F"</returns>
    public static string Calculate(float accuracy, bool isAllPerfect = false)
    {
        if (isAllPerfect || accuracy >= RANK_S_THRESHOLD) return "S";
        if (accuracy >= RANK_A_THRESHOLD)                 return "A";
        if (accuracy >= RANK_B_THRESHOLD)                 return "B";
        if (accuracy >= RANK_C_THRESHOLD)                 return "C";
        if (accuracy >= RANK_D_THRESHOLD)                 return "D";
        return "F";
    }

    /// <summary>
    /// Kiểm tra rank có đủ điều kiện mở khóa Hard không (A hoặc S).
    /// </summary>
    public static bool MeetsHardUnlockRequirement(string rank)
        => rank == "S" || rank == "A";

    /// <summary>
    /// Kiểm tra accuracy có đủ điều kiện mở khóa Hard không.
    /// </summary>
    public static bool MeetsHardUnlockRequirement(float accuracy, bool isAllPerfect = false)
        => isAllPerfect || accuracy >= HARD_UNLOCK_MIN_ACCURACY;

    /// <summary>
    /// So sánh hai rank — rank cao hơn trả về giá trị dương.
    /// Thứ tự: S > A > B > C > D > F
    /// </summary>
    public static int CompareRanks(string rankA, string rankB)
    {
        return RankToValue(rankA) - RankToValue(rankB);
    }

    private static int RankToValue(string rank) => rank switch
    {
        "S" => 5,
        "A" => 4,
        "B" => 3,
        "C" => 2,
        "D" => 1,
        _   => 0   // "F" hoặc ""
    };
}
