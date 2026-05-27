/// <summary>
/// Factory Pattern — tạo ISaveProvider từ SaveBackend enum.
///
/// Lợi ích:
///   - SaveManager chỉ gọi SaveProviderFactory.Create() một lần trong Awake()
///   - Thêm backend mới: chỉ cần thêm case ở đây, không sửa SaveManager (OCP)
///   - Dễ test: inject ISaveProvider mock thay vì dùng factory
///
/// Cách dùng:
///   ISaveProvider provider = SaveProviderFactory.Create(SaveBackend.ES3);
/// </summary>
public static class SaveProviderFactory
{
    /// <summary>
    /// Tạo ISaveProvider phù hợp với backend yêu cầu.
    /// Fallback về ES3SaveProvider nếu backend không được nhận dạng.
    /// </summary>
    public static ISaveProvider Create(SaveBackend backend) => backend switch
    {
        SaveBackend.ES3  => new ES3SaveProvider(),
        SaveBackend.Json => new JsonSaveProvider(),
        _                => new ES3SaveProvider()   // safe default
    };
}
