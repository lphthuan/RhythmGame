/// <summary>
/// Dữ liệu combo truyền qua event.
/// Struct nhẹ, không gây GC allocation.
/// </summary>
public readonly struct ComboData
{
    /// <summary>Combo hiện tại.</summary>
    public readonly int CurrentCombo;

    /// <summary>Combo cao nhất trong lượt chơi.</summary>
    public readonly int MaxCombo;

    /// <summary>True nếu combo vừa đạt mốc milestone (50, 100, ...).</summary>
    public readonly bool IsMilestone;

    /// <summary>True nếu combo vừa bị reset về 0.</summary>
    public readonly bool IsBreak;

    public ComboData(int currentCombo, int maxCombo, bool isMilestone, bool isBreak)
    {
        CurrentCombo = currentCombo;
        MaxCombo = maxCombo;
        IsMilestone = isMilestone;
        IsBreak = isBreak;
    }
}
