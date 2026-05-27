/// <summary>
/// Abstraction cho backend lưu trữ dữ liệu (DIP — Dependency Inversion Principle).
///
/// Triết lý thiết kế:
///   - Generic key/value API để không ràng buộc với cấu trúc file cụ thể.
///   - Implementation cụ thể (JsonSaveProvider, ES3SaveProvider) hoàn toàn thay thế được.
///   - SaveManager chỉ biết về interface này, không biết về file system hay ES3.
///
/// Implementations có trong dự án:
///   - JsonSaveProvider  : Lưu JSON text file (không mã hóa, dễ debug)
///   - ES3SaveProvider   : Dùng Easy Save 3 với AES encryption (production)
/// </summary>
public interface ISaveProvider
{
    /// <summary>
    /// Đọc và deserialize dữ liệu theo key.
    /// Trả về instance mới của T nếu key không tồn tại hoặc lỗi đọc.
    /// </summary>
    T Load<T>(string key) where T : class, new();

    /// <summary>
    /// Serialize và ghi dữ liệu theo key.
    /// </summary>
    /// <returns>true nếu ghi thành công.</returns>
    bool Save<T>(string key, T data);

    /// <summary>Kiểm tra key có tồn tại trong storage không.</summary>
    bool Exists(string key);

    /// <summary>Xóa dữ liệu theo key. Không throw nếu key không tồn tại.</summary>
    void Delete(string key);
}
