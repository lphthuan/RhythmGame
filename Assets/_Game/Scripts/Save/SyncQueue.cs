using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hàng đợi lưu trữ các cloud sync operations khi offline.
/// Dữ liệu persist qua ISaveProvider.
/// </summary>
public class SyncQueue
{
    private const int MAX_RETRY = 5;

    private readonly ISaveProvider _provider;
    private SyncQueueData _data;

    public SyncQueue(ISaveProvider provider)
    {
        _provider = provider;
        _data = _provider.Load<SyncQueueData>(SaveKeys.SYNC_QUEUE);
        _data.operations ??= new List<SyncOperation>();
        Debug.Log($"[SyncQueue] Loaded {_data.operations.Count} pending ops via {_provider.GetType().Name}.");
    }

    // ── Public API ───────────────────────────────────────────────

    public void Enqueue(string operationType, string payload)
    {
        _data.operations.Add(new SyncOperation
        {
            operationType = operationType,
            payload       = payload,
            createdAt     = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            retryCount    = 0
        });
        Flush();
        Debug.Log($"[SyncQueue] Queued '{operationType}'. Total: {_data.operations.Count}");
    }

    public List<SyncOperation> GetAll() => new(_data.operations);

    public void Remove(SyncOperation op)
    {
        _data.operations.Remove(op);
        Flush();
    }

    public void IncrementRetry(SyncOperation op)
    {
        op.retryCount++;
        if (op.retryCount >= MAX_RETRY)
        {
            Debug.LogWarning($"[SyncQueue] Op '{op.operationType}' đã thử {MAX_RETRY} lần — bỏ qua.");
            _data.operations.Remove(op);
        }
        Flush();
    }

    public bool HasPending()  => _data.operations.Count > 0;
    public int  PendingCount  => _data.operations.Count;

    public void Clear()
    {
        _data.operations.Clear();
        Flush();
    }

    // ── Internal ─────────────────────────────────────────────────

    private void Flush()
    {
        _provider.Save(SaveKeys.SYNC_QUEUE, _data);
    }
}
