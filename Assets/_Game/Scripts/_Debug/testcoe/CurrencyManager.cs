using System;

/// <summary>
/// Quản lý số dư Coin / Diamond của người chơi.
/// BẢN TEST: chỉ lưu trong bộ nhớ phiên chơi - mỗi lần chạy lại game
/// số dư tự reset về 2500/2500 để test mua thoải mái.
/// TODO API: khi bạn commit API nạp tiền / ví, thay 2 biến local dưới đây
/// bằng số dư server trả về (lúc đó mới lưu vĩnh viễn).
/// </summary>
public static class CurrencyManager
{
    // Số dư ban đầu mỗi phiên test (khớp với UI đang hiển thị 2500)
    public const int StartingCoins = 2500;
    public const int StartingDiamonds = 2500;

    // Chỉ nằm trong RAM - dừng Play là reset
    private static int coins = StartingCoins;
    private static int diamonds = StartingDiamonds;

    /// <summary>Bắn ra mỗi khi số dư thay đổi (UI đăng ký để tự cập nhật).</summary>
    public static event Action OnChanged;

    public static int Coins
    {
        get { return coins; }
    }

    public static int Diamonds
    {
        get { return diamonds; }
    }

    public static bool TrySpendCoins(int amount)
    {
        if (amount < 0 || coins < amount) return false;
        coins -= amount;
        if (OnChanged != null) OnChanged();
        return true;
    }

    public static bool TrySpendDiamonds(int amount)
    {
        if (amount < 0 || diamonds < amount) return false;
        diamonds -= amount;
        if (OnChanged != null) OnChanged();
        return true;
    }

    /// <summary>Cộng tiền - dùng khi nạp tiền (VNPay) hoặc nhận thưởng sau này.</summary>
    public static void AddCoins(int amount)
    {
        if (amount <= 0) return;
        coins += amount;
        if (OnChanged != null) OnChanged();
    }

    public static void AddDiamonds(int amount)
    {
        if (amount <= 0) return;
        diamonds += amount;
        if (OnChanged != null) OnChanged();
    }

    /// <summary>Reset số dư về ban đầu ngay lập tức (không cần chạy lại game).</summary>
    public static void ResetForTesting()
    {
        coins = StartingCoins;
        diamonds = StartingDiamonds;
        if (OnChanged != null) OnChanged();
    }
}
