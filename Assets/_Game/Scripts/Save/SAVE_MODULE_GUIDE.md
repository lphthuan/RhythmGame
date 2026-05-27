# Hướng dẫn hệ thống Save (Đã Refactor SOLID + ES3)

Hệ thống Save mới được thiết kế hoàn toàn dựa trên các nguyên tắc SOLID, đặc biệt là **Dependency Inversion** thông qua interface `ISaveProvider`.

## Cấu trúc kiến trúc

*   **Layer 0: Primitives (Common/Data & Common/Enums)**
    *   Chứa Model thuần tuý (`SongSaveData`, `PlayerSaveSettings`, `LeaderboardEntry`).
    *   Chứa Constants (`SaveKeys`, `Difficulty`, `SongUnlockType`).
    *   Chứa Utilities tĩnh (`SongKey` tạo key tránh circular dependency).
*   **Layer 1: Interfaces (Common/Interfaces)**
    *   `ISaveProvider`: Abstraction cho I/O (Lưu/Đọc).
    *   `IUnlockSystem`: Abstraction cho logic Unlock.
    *   `ILeaderboardProvider`: Abstraction cho Leaderboard.
    *   `ICloudSync`: Abstraction cho Cloud Sync.
*   **Layer 2: Providers (Save/)**
    *   `JsonSaveProvider`: Implementation bằng `JsonUtility` + File I/O (dễ debug).
    *   `ES3SaveProvider`: Implementation bằng **Easy Save 3** + Mã hóa AES (chuẩn production).
    *   `SaveProviderFactory`: Quyết định trả về Provider nào dựa trên Enum `SaveBackend`.
*   **Layer 3: Services (Save/)**
    *   `SaveManager`: Singleton điều phối luồng chính, dùng `ISaveProvider` và `IUnlockSystem`.
    *   `LocalLeaderboardManager`: Dùng `ISaveProvider` độc lập.
    *   `CloudSyncManager`: Implement `ICloudSync`, dùng `SyncQueue` (và `ISaveProvider`) cho hàng đợi offline.

## Cách thay đổi Backend Lưu Trữ

Bạn **KHÔNG CẦN** sửa code để đổi từ file JSON sang mã hóa AES (Easy Save 3).

1. Mở Scene chính (hoặc Scene khởi tạo).
2. Chọn GameObject chứa `SaveManager`, `LocalLeaderboardManager`, `CloudSyncManager`.
3. Trong Inspector, đổi trường `_backend` từ `Json` sang `ES3`.

> **Bảo mật:** Nhớ đổi chuỗi `ENCRYPTION_KEY` bên trong `ES3SaveProvider.cs` trước khi Release production.

## Coding Convention & Workflow

*   **Không dùng `SaveFileHandler` nữa.** Lớp này đã bị đánh dấu `[Obsolete]`.
*   Muốn lưu một file/key mới?
    1. Thêm key const vào `Common/Data/SaveKeys.cs`.
    2. Trong service của bạn, yêu cầu Factory cấp Provider:
       `ISaveProvider provider = SaveProviderFactory.Create(SaveBackend.ES3);`
    3. Gọi `provider.Save(SaveKeys.MY_NEW_KEY, data);`
*   Lắng nghe sự kiện người chơi hoàn thành bài hát thay vì gọi thẳng CloudSync/Leaderboard:
    `SaveManager.Instance.OnScoreSaved += MyHandler;`
