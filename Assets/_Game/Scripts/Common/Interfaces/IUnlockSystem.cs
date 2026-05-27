/// <summary>
/// Abstraction cho hệ thống mở khóa bài nhạc (SRP — tách khỏi SaveManager).
///
/// UnlockSystem là implementation duy nhất hiện tại.
/// Tách ra interface để:
///   1. Dễ test (có thể mock IUnlockSystem)
///   2. SaveManager không bị phình to (SRP)
///   3. Có thể swap logic mở khóa mà không sửa SaveManager (OCP)
/// </summary>
public interface IUnlockSystem
{
    /// <summary>Kiểm tra bài-độ khó đã mở khóa chưa.</summary>
    bool IsUnlocked(string songGroupId, Difficulty difficulty);

    /// <summary>
    /// Mở khóa một bài-độ khó trực tiếp.
    /// Không tự flush — SaveManager quyết định khi nào ghi disk.
    /// </summary>
    void Unlock(string songGroupId, Difficulty difficulty);

    /// <summary>
    /// Mua bài trong Shop → mở khóa Easy + Medium.
    /// Hard vẫn cần điều kiện kỹ năng (Rank A+ trên Medium).
    /// </summary>
    void PurchaseSong(string songGroupId);

    /// <summary>
    /// Khởi tạo unlock mặc định cho free song.
    /// Chỉ tạo entry nếu chưa tồn tại (không override dữ liệu đã lưu).
    /// Gọi từ SongListManager khi render danh sách bài.
    /// </summary>
    void InitializeFreeUnlocks(string songGroupId);
}
