using System;

/// <summary>
/// Abstraction cho hệ thống đồng bộ cloud (DIP + ISP).
///
/// CloudSyncManager implement interface này.
/// SaveManager KHÔNG biết về ICloudSync — thay vào đó SaveManager fires events
/// và CloudSyncManager tự subscribe. Không cần interface này trong SaveManager.
///
/// Interface được dùng bởi:
///   - Code muốn gọi cloud sync trực tiếp (ví dụ: nút "Sync ngay" trong Settings UI)
///   - Dependency injection / testing
///
/// Đặt trong Common/ vì UI module có thể cần biết về trạng thái sync.
/// </summary>
public interface ICloudSync
{
    /// <summary>Số lượng operations đang chờ gửi lên cloud.</summary>
    int PendingCount { get; }

    /// <summary>Gửi điểm số lên cloud. Nếu offline → xếp vào hàng đợi.</summary>
    void SyncScore(string songGroupId, Difficulty difficulty, int score, float accuracy, string rank);

    /// <summary>Gửi settings lên cloud. Nếu offline → xếp vào hàng đợi.</summary>
    void SyncSettings(PlayerSaveSettings settings);

    /// <summary>
    /// Tải dữ liệu từ server về, merge với local (lấy điểm cao hơn).
    /// Dùng khi đăng nhập trên thiết bị mới.
    /// </summary>
    void DownloadPlayerData(Action<bool> onComplete = null);

    /// <summary>
    /// Gửi tất cả operations đang chờ trong hàng đợi.
    /// Gọi tự động bởi NetworkMonitor khi mạng trở lại.
    /// </summary>
    void ProcessPendingQueue();
}
