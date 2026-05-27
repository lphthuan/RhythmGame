using System;
using UnityEngine;

/// <summary>
/// Singleton — Theo dõi trạng thái kết nối mạng.
///
/// Kiểm tra Application.internetReachability định kỳ (mặc định 3 giây).
/// Phát event OnConnectivityChanged khi mạng thay đổi.
/// Khi phát hiện mạng trở lại → tự động kích hoạt CloudSyncManager.ProcessPendingQueue().
///
/// Không dùng Ping vì tốn tài nguyên — dùng Unity API đơn giản, đủ cho mobile game.
/// </summary>
public class NetworkMonitor : MonoBehaviour
{
    public static NetworkMonitor Instance { get; private set; }

    [Header("Config")]
    [Tooltip("Khoảng thời gian (giây) kiểm tra kết nối một lần.")]
    [SerializeField] private float checkIntervalSeconds = 3f;

    /// <summary>
    /// Phát ra khi trạng thái kết nối thay đổi.
    /// true = vừa có mạng trở lại, false = vừa mất mạng.
    /// </summary>
    public event Action<bool> OnConnectivityChanged;

    /// <summary>Trạng thái kết nối hiện tại.</summary>
    public bool IsConnected { get; private set; }

    private float _timer;

    // ─────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Trạng thái ban đầu
        IsConnected = CheckConnectivity();
        Debug.Log($"[NetworkMonitor] 🌐 Khởi tạo: {(IsConnected ? "Online" : "Offline")}");
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= checkIntervalSeconds)
        {
            _timer = 0f;
            PollConnectivity();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Internal
    // ─────────────────────────────────────────────────────────────────────

    private void PollConnectivity()
    {
        bool current = CheckConnectivity();

        if (current == IsConnected) return; // Không thay đổi

        IsConnected = current;
        Debug.Log($"[NetworkMonitor] 🌐 Connectivity: {(IsConnected ? "ONLINE ✅" : "OFFLINE ❌")}");

        OnConnectivityChanged?.Invoke(IsConnected);

        // Khi có mạng trở lại → flush pending queue
        if (IsConnected)
        {
            CloudSyncManager.Instance?.ProcessPendingQueue();
        }
    }

    private static bool CheckConnectivity()
        => Application.internetReachability != NetworkReachability.NotReachable;
}
