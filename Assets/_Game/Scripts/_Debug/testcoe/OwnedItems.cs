using System.Collections.Generic;

/// <summary>
/// Danh sách sản phẩm người chơi đã mua.
/// BẢN TEST: chỉ lưu trong bộ nhớ phiên chơi - mỗi lần chạy lại game
/// mọi sản phẩm trở về trạng thái chưa mua để test lại từ đầu.
/// TODO API: khi có API, thay HashSet dưới đây bằng danh sách item
/// server trả về sau khi đăng nhập (lúc đó mới lưu vĩnh viễn).
/// </summary>
public static class OwnedItems
{
    // Chỉ nằm trong RAM - dừng Play là reset
    private static readonly HashSet<string> owned = new HashSet<string>();

    public static bool IsOwned(string itemId)
    {
        return owned.Contains(itemId);
    }

    public static void SetOwned(string itemId)
    {
        owned.Add(itemId);
    }

    /// <summary>Bỏ sở hữu 1 item để test lại việc mua ngay trong phiên.</summary>
    public static void RemoveForTesting(string itemId)
    {
        owned.Remove(itemId);
    }

    /// <summary>Reset toàn bộ về chưa mua ngay lập tức.</summary>
    public static void ResetAllForTesting()
    {
        owned.Clear();
    }
}
