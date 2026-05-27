using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// ISaveProvider implementation dùng JsonUtility + System.IO.
///
/// Đặc điểm:
///   - Mỗi key ánh xạ đến một file .json riêng (xem KEY_TO_FILE).
///   - Không mã hóa — phù hợp để debug, không dùng production.
///   - Nếu key chưa có trong map → tự tạo file "{key}.json".
///
/// Thư mục lưu: Application.persistentDataPath/RhythmGame/
/// </summary>
public class JsonSaveProvider : ISaveProvider
{
    // ── Config ──────────────────────────────────────────────────
    private const string SUBFOLDER = "RhythmGame";

    /// <summary>Ánh xạ SaveKey → tên file. Chỉ JsonSaveProvider cần biết điều này.</summary>
    private static readonly Dictionary<string, string> KEY_TO_FILE = new()
    {
        [SaveKeys.PLAYER_DATA] = "savegame.json",
        [SaveKeys.LEADERBOARD] = "leaderboard.json",
        [SaveKeys.SYNC_QUEUE]  = "pending_sync.json"
    };

    // ── Properties ───────────────────────────────────────────────

    /// <summary>Đường dẫn thư mục lưu (dùng cho debug context menu).</summary>
    public string SaveDirectory { get; }

    // ── Constructor ──────────────────────────────────────────────

    public JsonSaveProvider()
    {
        SaveDirectory = Path.Combine(Application.persistentDataPath, SUBFOLDER);
        Directory.CreateDirectory(SaveDirectory);
    }

    // ── ISaveProvider ────────────────────────────────────────────

    /// <inheritdoc/>
    public T Load<T>(string key) where T : class, new()
    {
        string path = GetPath(key);

        if (!File.Exists(path))
        {
            Debug.Log($"[JsonSave] '{key}' not found → default.");
            return new T();
        }

        try
        {
            string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            T result = JsonUtility.FromJson<T>(json);
            return result ?? new T();
        }
        catch (Exception e)
        {
            Debug.LogError($"[JsonSave] Load '{key}': {e.Message}");
            return new T();
        }
    }

    /// <inheritdoc/>
    public bool Save<T>(string key, T data)
    {
        string path = GetPath(key);
        try
        {
            File.WriteAllText(path, JsonUtility.ToJson(data, prettyPrint: true),
                              System.Text.Encoding.UTF8);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[JsonSave] Save '{key}': {e.Message}");
            return false;
        }
    }

    /// <inheritdoc/>
    public bool Exists(string key) => File.Exists(GetPath(key));

    /// <inheritdoc/>
    public void Delete(string key)
    {
        string path = GetPath(key);
        if (File.Exists(path)) File.Delete(path);
    }

    // ── Private ──────────────────────────────────────────────────

    private string GetPath(string key)
    {
        string fileName = KEY_TO_FILE.TryGetValue(key, out string f) ? f : $"{key}.json";
        return Path.Combine(SaveDirectory, fileName);
    }
}
