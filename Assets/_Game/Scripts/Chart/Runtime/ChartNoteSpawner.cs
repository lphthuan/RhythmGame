using System.Collections.Generic;
using UnityEngine;

public class ChartNoteSpawner : MonoBehaviour
{
    [Header("Chart")]
    [SerializeField] private string chartFileName = "test_chart";

    [Header("Clock")]
    [SerializeField] private ChartPlaybackClock playbackClock;

    [Header("Spawn")]
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private Transform noteParent;

    [Header("Gameplay — Required for movement")]
    [Tooltip("NoteManager is required. Notes will not move without it.")]
    [SerializeField] private NoteManager noteManager;

    [Header("Layout")]
    [SerializeField] private float laneSpacing = 160f;

    [Header("Movement")]
    [Tooltip("Y position of the hit line in anchoredPosition space (UI).")]
    [SerializeField] private float hitlineY = -330f;

    [Tooltip("Scroll speed in pixels per second (UI anchoredPosition).")]
    [SerializeField] private float scrollSpeed = 600f;

    [Tooltip("Touch radius in pixels used for input detection.")]
    [SerializeField] private float touchRadius = 120f;

    [Header("Timing")]
    [SerializeField] private float preSpawnTime = 2f;

    [Header("Fallback / Debug")]
    [Tooltip("Only used as fallback when spawned note does not have NoteBase/NoteMovement.")]
    [SerializeField] private float spawnY = 3f;

    [SerializeField] private bool logSpawnedNotes = false;

    private List<ChartNoteSpawnData> _spawnDataList;
    private int _nextSpawnIndex;
    private bool _isReady;
    private int _laneCount = 4;

    private void Start()
    {
        if (noteManager == null)
        {
            Debug.LogError("ChartNoteSpawner: NoteManager is not assigned. " +
                           "Notes will not move. Assign NoteManager in the Inspector.");
            return;
        }

        if (!ChartSpawnDataProvider.TryGetChartAndSpawnData(
                chartFileName,
                out ChartData loadedChart,
                out _spawnDataList))
        {
            Debug.LogError("ChartNoteSpawner: Failed to get chart and spawn data.");
            return;
        }

        _laneCount = loadedChart.laneCount > 0 ? loadedChart.laneCount : 4;

        if (playbackClock != null)
        {
            playbackClock.SetOffset(loadedChart.offset);
        }

        Debug.Log($"ChartNoteSpawner: Applied chart offset: {loadedChart.offset}");

        ClearSpawnedNotes();

        _nextSpawnIndex = 0;
        _isReady = true;

        Debug.Log($"ChartNoteSpawner ready. Notes to spawn: {_spawnDataList.Count}");
    }

    private void Update()
    {
        if (!_isReady)
        {
            return;
        }

        if (playbackClock == null)
        {
            Debug.LogError("ChartNoteSpawner: Playback clock is missing.");
            _isReady = false;
            return;
        }

        if (!playbackClock.IsPlaying)
        {
            return;
        }

        float songTime = playbackClock.SongTime;

        // Sync NoteManager time với audio clock để NoteMovement tính đúng vị trí.
        // SetExternalTime() tắt internal clock của NoteManager, tránh drift.
        noteManager.SetExternalTime(songTime);

        SpawnDueNotes(songTime);
    }

    private void SpawnDueNotes(float songTime)
    {
        while (_nextSpawnIndex < _spawnDataList.Count)
        {
            ChartNoteSpawnData data = _spawnDataList[_nextSpawnIndex];

            float spawnTime = data.hitTime - preSpawnTime;

            if (songTime < spawnTime)
            {
                break;
            }

            SpawnNote(data);
            _nextSpawnIndex++;
        }
    }

    private void SpawnNote(ChartNoteSpawnData data)
    {
        if (notePrefab == null)
        {
            Debug.LogError("ChartNoteSpawner: Note prefab is missing.");
            return;
        }

        // Instantiate dưới noteParent (UI Canvas). Không set world position vì
        // NoteMovement sẽ điều khiển anchoredPosition ngay sau Initialize().
        GameObject noteObject = Instantiate(notePrefab, noteParent);
        noteObject.name = $"Note_{data.noteId}_{data.noteType}_Lane{data.laneIndex}_Time{data.hitTime:F2}";

        NoteBase noteBase = noteObject.GetComponent<NoteBase>();

        if (noteBase != null)
        {
            NoteRuntimeData runtimeData = new NoteRuntimeData
            {
                noteId         = data.noteId,
                laneIndex      = data.laneIndex,
                hitTime        = data.hitTime,
                duration       = data.duration,
                anchoredX      = GetCenteredLaneX(data.laneIndex),
                hitlineY       = this.hitlineY,
                scrollSpeed    = this.scrollSpeed,
                touchRadius    = this.touchRadius,
                flickDirection = data.flickDirection,
            };

            // Initialize sẽ set anchoredPosition.x và chuẩn bị NoteMovement.
            // NoteMovement.Tick() sẽ tính Y mỗi frame từ NoteManager.
            noteBase.Initialize(runtimeData);

            noteManager.RegisterNote(noteBase);

            if (logSpawnedNotes)
            {
                Debug.Log($"ChartNoteSpawner: Spawned + initialized note ID {data.noteId} " +
                          $"| Lane {data.laneIndex} | HitTime {data.hitTime:F2}");
            }
        }
        else
        {
            // Fallback: prefab không có NoteBase — set anchoredPosition thủ công.
            // Note sẽ không move theo NoteMovement system.
            Debug.LogWarning($"ChartNoteSpawner: Note ID {data.noteId} prefab has no NoteBase. " +
                             "Movement will not work. Using spawnY fallback.");

            if (noteObject.TryGetComponent<RectTransform>(out RectTransform rt))
            {
                rt.anchoredPosition = new Vector2(GetCenteredLaneX(data.laneIndex), spawnY);
            }
            else
            {
                noteObject.transform.position = new Vector3(data.laneIndex * laneSpacing, spawnY, 0f);
            }
        }
    }

    /// <summary>
    /// Tính anchoredPosition X để 4 lane trải đều, căn giữa màn hình (X = 0).
    /// Ví dụ 4 lanes spacing 160: -240, -80, +80, +240
    /// </summary>
    private float GetCenteredLaneX(int laneIndex)
    {
        float totalWidth = (_laneCount - 1) * laneSpacing;
        float startX = -totalWidth / 2f;
        return startX + laneIndex * laneSpacing;
    }

    private void ClearSpawnedNotes()
    {
        if (noteParent == null)
        {
            return;
        }

        for (int i = noteParent.childCount - 1; i >= 0; i--)
        {
            Destroy(noteParent.GetChild(i).gameObject);
        }
    }
}