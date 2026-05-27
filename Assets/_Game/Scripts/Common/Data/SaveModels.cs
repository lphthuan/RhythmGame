using System;
using System.Collections.Generic;

// =========================================================================
// Các class Data Model phụ trợ cho hệ thống Save (Wrapper / DTO)
// Chứa các class chỉ dùng cho mục đích Serialize/Deserialize hoặc Network Payload
// =========================================================================

// ── Wrapper cho JsonUtility ──────────────────────────────────────────────

/// <summary>
/// Wrapper để serialize danh sách entry vì JsonUtility cần class root.
/// </summary>
[Serializable]
public class LeaderboardData
{
    public List<LeaderboardEntry> entries = new();
}

/// <summary>
/// Wrapper root object cho SyncQueue.
/// </summary>
[Serializable]
public class SyncQueueData
{
    public List<SyncOperation> operations = new();
}

// ── Cloud Sync DTOs ──────────────────────────────────────────────────────

/// <summary>
/// Một operation cần đồng bộ lên cloud.
/// Được serialize vào queue khi offline.
/// </summary>
[Serializable]
public class SyncOperation
{
    public string operationType;
    public string payload;
    public long createdAt;
    public int retryCount;
}

[Serializable]
public class ScoreSyncPayload
{
    public string playerId;
    public string songKey;
    public string difficulty;
    public int    score;
    public float  accuracy;
    public string rank;
    public long   timestamp;
}

[Serializable]
public class PlayerDataResponse
{
    public string          playerId;
    public PlayerSaveSettings  settings;
    public SongSaveData[]  songs;
    public long            lastSyncedAt;
}
