using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Quản lý đồng bộ cloud. Implement ICloudSync.
/// Dùng Event-driven architecture (subscribe vào SaveManager.OnScoreSaved).
/// Sửa lỗi OCP: dùng Dictionary map operationType → endpoint.
/// </summary>
public class CloudSyncManager : MonoBehaviour, ICloudSync
{
    public static CloudSyncManager Instance { get; private set; }

    [Header("Dependencies")]
    [Tooltip("Backend để lưu SyncQueue khi offline")]
    [SerializeField] private SaveBackend _backend = SaveBackend.ES3;

    [Header("Server Config")]
    [SerializeField] private string _baseUrl = "http://localhost:5000/api";
    [SerializeField] private int _timeoutSeconds = 10;

    private SyncQueue _queue;

    // ── OCP Fix: Đăng ký Endpoint động ───────────────────────────
    private readonly Dictionary<string, string> _endpoints = new()
    {
        { "upload_score",    "/scores" },
        { "upload_settings", "/player/settings" },
        { "upload_progress", "/player/progress" }
    };

    // ─────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ISaveProvider provider = SaveProviderFactory.Create(_backend);
        _queue = new SyncQueue(provider);
    }

    private void Start()
    {
        // Tự động subscribe vào SaveManager thay vì bắt SaveManager gọi cứng
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnScoreSaved += HandleScoreSaved;
        }

        if (IsOnline) ProcessPendingQueue();
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.OnScoreSaved -= HandleScoreSaved;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // ICloudSync
    // ─────────────────────────────────────────────────────────────────────

    public int PendingCount => _queue.PendingCount;
    public bool IsOnline => NetworkMonitor.Instance != null && NetworkMonitor.Instance.IsConnected;

    public void SyncScore(string songGroupId, Difficulty difficulty, int score, float accuracy, string rank)
    {
        var payload = new ScoreSyncPayload
        {
            playerId   = SaveManager.Instance?.GetPlayerId() ?? "guest",
            songKey    = SongKey.Build(songGroupId, difficulty),
            difficulty = difficulty.ToString(),
            score      = score,
            accuracy   = accuracy,
            rank       = rank,
            timestamp  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        SendOrQueue("upload_score", JsonUtility.ToJson(payload));
    }

    public void SyncSettings(PlayerSaveSettings settings)
    {
        SendOrQueue("upload_settings", JsonUtility.ToJson(settings));
    }

    public void SyncProgress(string songGroupId, Difficulty difficulty, bool isUnlocked)
    {
        var payload = new { songKey = SongKey.Build(songGroupId, difficulty), isUnlocked };
        SendOrQueue("upload_progress", JsonUtility.ToJson(payload));
    }

    public void DownloadPlayerData(Action<bool> onComplete = null)
    {
        if (!IsOnline)
        {
            Debug.LogWarning("[CloudSync] ❌ Không thể tải — đang offline.");
            onComplete?.Invoke(false);
            return;
        }

        StartCoroutine(GetRequest("/player/data", response =>
        {
            if (response == null) { onComplete?.Invoke(false); return; }

            try
            {
                var serverData = JsonUtility.FromJson<PlayerDataResponse>(response);
                MergeWithLocal(serverData);
                onComplete?.Invoke(true);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CloudSync] Parse error: {e.Message}");
                onComplete?.Invoke(false);
            }
        }));
    }

    public void ProcessPendingQueue()
    {
        if (!_queue.HasPending()) return;
        StartCoroutine(FlushQueueCoroutine());
    }

    // ─────────────────────────────────────────────────────────────────────
    // Internal
    // ─────────────────────────────────────────────────────────────────────

    private void HandleScoreSaved(ScoreSavedArgs args)
    {
        SyncScore(args.SongGroupId, args.Difficulty, args.Score, args.Accuracy, args.Rank);
    }

    private void SendOrQueue(string opType, string json)
    {
        if (!_endpoints.TryGetValue(opType, out string endpoint))
        {
            Debug.LogError($"[CloudSync] Chưa đăng ký endpoint cho operation: {opType}");
            return;
        }

        if (IsOnline)
        {
            StartCoroutine(PostRequest(endpoint, json, success =>
            {
                if (!success) _queue.Enqueue(opType, json);
            }));
        }
        else
        {
            _queue.Enqueue(opType, json);
        }
    }

    private IEnumerator FlushQueueCoroutine()
    {
        var ops = _queue.GetAll();

        foreach (var op in ops)
        {
            if (!IsOnline) break;

            if (!_endpoints.TryGetValue(op.operationType, out string endpoint))
            {
                _queue.Remove(op);
                continue;
            }

            bool success = false;
            yield return StartCoroutine(PostRequest(endpoint, op.payload, ok => success = ok));

            if (success) _queue.Remove(op);
            else _queue.IncrementRetry(op);
        }
    }

    private void MergeWithLocal(PlayerDataResponse serverData)
    {
        if (serverData?.songs == null || SaveManager.Instance == null) return;

        bool changed = false;

        foreach (var serverSong in serverData.songs)
        {
            if (string.IsNullOrEmpty(serverSong.songKey)) continue;

            if (!Enum.TryParse(serverSong.difficulty, out Difficulty diff)) continue;

            var local = SaveManager.Instance.GetSongData(serverSong.songGroupId, diff);

            if (local == null || serverSong.highScore > local.highScore)
            {
                SaveManager.Instance.SaveResult(
                    serverSong.songGroupId, diff,
                    serverSong.highScore, serverSong.bestAccuracy);
                changed = true;
            }
        }

        if (changed) SaveManager.Instance.FlushToDisk();
    }

    // ─────────────────────────────────────────────────────────────────────
    // HTTP
    // ─────────────────────────────────────────────────────────────────────

    private IEnumerator PostRequest(string endpoint, string json, Action<bool> onDone)
    {
        string url  = _baseUrl + endpoint;
        byte[] body = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(url, "POST")
        {
            uploadHandler   = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer(),
            timeout         = _timeoutSeconds
        };
        req.SetRequestHeader("Content-Type", "application/json");
        AppendAuthHeader(req);

        yield return req.SendWebRequest();
        onDone?.Invoke(req.result == UnityWebRequest.Result.Success);
    }

    private IEnumerator GetRequest(string endpoint, Action<string> onDone)
    {
        string url = _baseUrl + endpoint;
        using var req = UnityWebRequest.Get(url);
        req.timeout = _timeoutSeconds;
        AppendAuthHeader(req);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success) onDone?.Invoke(req.downloadHandler.text);
        else onDone?.Invoke(null);
    }

    private void AppendAuthHeader(UnityWebRequest req)
    {
        string playerId = SaveManager.Instance?.GetPlayerId();
        if (!string.IsNullOrEmpty(playerId))
            req.SetRequestHeader("X-Player-Id", playerId);
    }
}
