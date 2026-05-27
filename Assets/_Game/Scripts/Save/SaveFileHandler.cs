using System;
using System.IO;
using UnityEngine;

/// <summary>
/// ⚠️ [OBSOLETE] Tầng I/O thuần tuý cũ (đã được thay thế bằng ISaveProvider).
///
/// Hướng dẫn Migrate:
///   Thay vì dùng: SaveFileHandler.Save("path", data);
///   Hãy dùng   : ISaveProvider provider = SaveProviderFactory.Create(SaveBackend.ES3);
///                provider.Save("key", data);
/// </summary>
[Obsolete("Sử dụng ISaveProvider (JsonSaveProvider hoặc ES3SaveProvider) thông qua SaveProviderFactory thay thế.")]
public static class SaveFileHandler
{
    private const string SAVE_FOLDER      = "RhythmGame";
    private const string SAVE_FILE        = "savegame.json";
    private const string LEADERBOARD_FILE = "leaderboard.json";
    private const string PENDING_SYNC_FILE = "pending_sync.json";

    public static string SavePath         => GetPath(SAVE_FILE);
    public static string LeaderboardPath  => GetPath(LEADERBOARD_FILE);
    public static string PendingSyncPath  => GetPath(PENDING_SYNC_FILE);

    public static string SaveDirectory
    {
        get
        {
            string dir = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    [Obsolete("Dùng ISaveProvider.Load<T>(key)")]
    public static T Load<T>(string filePath) where T : class, new()
    {
        if (!File.Exists(filePath)) return new T();
        try
        {
            string json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            T data = JsonUtility.FromJson<T>(json);
            return data ?? new T();
        }
        catch (Exception) { return new T(); }
    }

    [Obsolete("Dùng ISaveProvider.Save<T>(key, data)")]
    public static bool Save<T>(string filePath, T data)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
            return true;
        }
        catch (Exception) { return false; }
    }

    [Obsolete("Dùng ISaveProvider.Exists(key)")]
    public static bool Exists(string filePath) => File.Exists(filePath);

    [Obsolete("Dùng ISaveProvider.Delete(key)")]
    public static void Delete(string filePath)
    {
        if (File.Exists(filePath)) File.Delete(filePath);
    }

    private static string GetPath(string fileName)
    {
        string dir = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, fileName);
    }
}
