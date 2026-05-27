using UnityEngine;

/// <summary>
/// ISaveProvider implementation dùng Easy Save 3 (ES3) với mã hóa AES.
///
/// Đặc điểm:
///   - Toàn bộ dữ liệu lưu vào một file .es3 duy nhất (tất cả keys).
///   - AES encryption tự động — người dùng không thể đọc/sửa bằng text editor.
///   - ES3 serialize Dictionary, abstract class, interface — JsonUtility không làm được.
///   - ES3 handles concurrent reads/writes an toàn (single-threaded Unity).
///
/// File: Application.persistentDataPath/rhythmgame.es3
///
/// ⚠️  ENCRYPTION_KEY nên được thay bằng giá trị bí mật thực sự trước khi release.
///     Có thể dùng Unity Cloud Build secrets hoặc CryptoConfig.
/// </summary>
public class ES3SaveProvider : ISaveProvider
{
    // ── Config ──────────────────────────────────────────────────
    private const string ES3_FILE_NAME    = "rhythmgame.es3";

    // TODO: Thay bằng key bí mật thực — không commit key vào source control!
    private const string ENCRYPTION_KEY   = "RhythmGame@SecretKey2024";

    private readonly ES3Settings _settings;

    // ── Constructor ──────────────────────────────────────────────

    public ES3SaveProvider()
    {
        _settings = new ES3Settings(
            ES3_FILE_NAME,
            ES3.EncryptionType.AES,
            ENCRYPTION_KEY
        );
    }

    // ── ISaveProvider ────────────────────────────────────────────

    /// <inheritdoc/>
    public T Load<T>(string key) where T : class, new()
    {
        try
        {
            // ES3.Load với defaultValue — trả về new T() nếu key không tồn tại
            return ES3.Load<T>(key, new T(), _settings);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ES3Save] Load '{key}': {e.Message}");
            return new T();
        }
    }

    /// <inheritdoc/>
    public bool Save<T>(string key, T data)
    {
        try
        {
            ES3.Save<T>(key, data, _settings);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ES3Save] Save '{key}': {e.Message}");
            return false;
        }
    }

    /// <inheritdoc/>
    public bool Exists(string key)
    {
        try   { return ES3.KeyExists(key, _settings); }
        catch { return false; }
    }

    /// <inheritdoc/>
    public void Delete(string key)
    {
        try
        {
            if (ES3.KeyExists(key, _settings))
                ES3.DeleteKey(key, _settings);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ES3Save] Delete '{key}': {e.Message}");
        }
    }
}
