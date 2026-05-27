/// <summary>
/// Loại điều kiện mở khóa của một bài nhạc.
/// Áp dụng cho cả nhóm bài (song group) — tất cả difficulty trong nhóm dùng cùng UnlockType.
/// Hard difficulty luôn có thêm điều kiện kỹ năng (Rank A+ trên Medium) bất kể UnlockType.
/// </summary>
public enum SongUnlockType
{
    /// <summary>Mở khóa Easy + Medium mặc định khi cài game.</summary>
    Free,

    /// <summary>Cần mua trong Shop bằng RC trước khi chơi.</summary>
    Purchase
}
