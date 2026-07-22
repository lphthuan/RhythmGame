using System.Collections.Generic;
using Dypsloom.RhythmTimeline.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM
using Keyboard = UnityEngine.InputSystem.Keyboard;
#endif

public class ChartNoteSpawner : MonoBehaviour
{
    [Header("Chart")]
    [Tooltip("Tên file JSON chart. Nếu SelectedSongManager có bài đang chọn thì sẽ đọc từ đó thay thế.")]
    [SerializeField] private string chartFileName = "test_chart";

    [Header("Clock")]
    [SerializeField] private ChartPlaybackClock playbackClock;

    [Header("Spawn")]
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private Transform noteParent;
    [SerializeField] private NoteVisualConfig visualConfig;
    [SerializeField] private bool useGeneratedTypedNotes = true;

    [Header("Gameplay — Required for movement")]
    [Tooltip("NoteManager is required. Notes will not move without it.")]
    [SerializeField] private NoteManager noteManager;

    [Header("Editor Integration")]
    [Tooltip("Preview Note Parent (world space). Sẽ tự động tắt khi runtime spawn bắt đầu để tránh chồng lên runtime notes.")]
    [SerializeField] private GameObject previewNoteParentToDisable;

    [Header("Pool")]
    [SerializeField] private int initialPoolSize = 50;

    [Header("Layout")]
    [SerializeField] private float laneSpacing = 160f;

    [Header("Movement")]
    [Tooltip("Y position of the hit line in anchoredPosition space (UI).")]
    [SerializeField] private float hitlineY = -330f;

    [Tooltip("Scroll speed in pixels per second (UI anchoredPosition).")]
    [SerializeField] private float scrollSpeed = 600f;

    [Tooltip("Touch radius in pixels used for input detection.")]
    [SerializeField] private float touchRadius = 120f;

    [Header("PC Test Controls")]
    [SerializeField] private bool enableScrollSpeedHotkeys = true;
    [SerializeField] private KeyCode increaseSpeedKey = KeyCode.F3;
    [SerializeField] private KeyCode decreaseSpeedKey = KeyCode.F4;
    [SerializeField] private float scrollSpeedStep = 100f;
    [SerializeField] private float minScrollSpeed = RuntimeGameplaySettings.MinScrollSpeed;
    [SerializeField] private float maxScrollSpeed = RuntimeGameplaySettings.MaxScrollSpeed;
    [SerializeField] private bool persistScrollSpeed = true;
    [SerializeField] private bool logScrollSpeedChanges = true;

    [Header("Timing")]
    [Tooltip("Offset bổ sung theo chart. Mặc định 0; dùng Offset trong Settings để căn máy người chơi.")]
    [SerializeField] private float gameplayTimingOffsetSeconds = 0f;
    [Tooltip("Thời gian xuất hiện sớm tối thiểu. Tốc độ thấp có thể cần lâu hơn để note đi hết màn hình.")]
    [SerializeField, Min(0f)] private float preSpawnTime = 2f;
    [Tooltip("Khoảng cách tối thiểu từ hitline khi note xuất hiện. Giữ note bắt đầu từ phía trên ngay cả ở tốc độ thấp.")]
    [SerializeField, Min(0f)] private float minimumSpawnDistance = 900f;

    [Header("Debug")]

    [SerializeField] private bool logSpawnedNotes = false;

    [Header("Runtime UI")]
    [SerializeField] private Vector2 comboDisplayPosition = new Vector2(0f, -180f);

    [Header("Edit Mode")]
    [Tooltip("Bật khi muốn chỉnh chart bằng tay (Load Preview → Play → kéo note → Save).\n" +
             "Khi ON: không spawn runtime notes, không tắt Note Parent preview.\n" +
             "Khi OFF: chế độ bình thường, Note Parent tự tắt, runtime notes spawn.")]
    [SerializeField] private bool editMode = false;

    private List<ChartNoteSpawnData> _spawnDataList;
    private readonly Dictionary<NoteType, Queue<NoteBase>> _typedPools = new();
    private readonly HashSet<NoteBase> _pooledGeneratedNotes = new();
    private readonly HashSet<NoteBase> _finishedRuntimeNotes = new();
    private int _nextSpawnIndex;
    private int _finishedNoteCount;
    private bool _isReady;
    private bool _chartFinished;
    private int _laneCount = 4;
    private bool _usingTypedPools;
    private bool _listeningToNoteManager;

    public event System.Action OnChartFinished;
    public int TotalNoteCount => _spawnDataList != null ? _spawnDataList.Count : 0;
    public bool IsReady => _isReady;

    /// <summary>
    /// Đồng bộ chartFileName với SongData được chọn từ Editor dropdown.
    /// Gọi bởi ChartGeneratorToolEditor khi user đổi bài nhạc.
    /// </summary>
    public void SetChartFileName(string fileName)
    {
        chartFileName = fileName;
    }

    public void ApplyGameplayLayout(float newLaneSpacing, float newHitlineY, float newTouchRadius = -1f)
    {
        laneSpacing = Mathf.Max(1f, newLaneSpacing);
        hitlineY = newHitlineY;

        if (newTouchRadius > 0f)
            touchRadius = newTouchRadius;
    }

    private void Start()
    {
        LoadPersistedScrollSpeed();
        GameplaySongBackdrop.Apply();
        EnsureGameplayUi();

        // ─── EDIT MODE: chỉ xem preview, không spawn runtime notes ───────────
        if (editMode)
        {
            Debug.Log("ChartNoteSpawner: Edit Mode ON — runtime spawn disabled. " +
                      "Note Parent giữ nguyên để editor kéo note.");
            return;
        }
        // ────────────────────────────────────────────────────────────────

        if (noteManager == null)
        {
            Debug.LogError("ChartNoteSpawner: NoteManager is not assigned. " +
                           "Notes will not move. Assign NoteManager in the Inspector.");
            return;
        }

        SubscribeToNoteManager();

        // Tắt preview Note Parent để runtime notes không bị chồng lên preview notes.
        if (previewNoteParentToDisable != null)
        {
            previewNoteParentToDisable.SetActive(false);
            Debug.Log("ChartNoteSpawner: Preview Note Parent đã tắt để nhường chỗ cho runtime notes.");
        }

        if (useGeneratedTypedNotes)
        {
            InitializeTypedPools();
        }
        else
        {
            // --- KHỞI TẠO OBJECT POOL ---
            NotePool pool = NotePool.Instance;
            if (pool == null)
            {
                GameObject poolObj = new GameObject("NotePool");
                pool = poolObj.AddComponent<NotePool>();
            }

            NoteBase noteBasePrefab = notePrefab != null ? notePrefab.GetComponent<NoteBase>() : null;
            if (noteBasePrefab != null)
            {
                pool.InitializePool(noteBasePrefab, noteParent, initialPoolSize, noteManager);
            }
            else
            {
                Debug.LogError("ChartNoteSpawner: notePrefab must have a NoteBase component for Object Pooling.");
            }
            // ------------------------------
        }

        // Ưu tiên lấy từ SelectedSongManager (bài đang chọn trong menu).
        // Nếu không có (test trực tiếp trong scene) → dùng Inspector value.
        string fileToLoad = chartFileName;

        SongData selectedSong = SelectedSongManager.Instance != null
            ? SelectedSongManager.Instance.SelectedSong
            : null;
        Difficulty selectedDifficulty = SelectedSongManager.Instance != null
            ? SelectedSongManager.Instance.SelectedDifficulty
            : Difficulty.Medium;

        if (selectedSong == null &&
            SelectedSongManager.TryRestoreLastSelection(out SongData restoredSong, out Difficulty restoredDifficulty))
        {
            selectedSong = restoredSong;
            selectedDifficulty = restoredDifficulty;
            SelectedSongManager.EnsureInstance().SetSelectedSong(selectedSong, selectedDifficulty);
            Debug.LogWarning($"ChartNoteSpawner: Restored selected song '{selectedSong.SongTitle}' ({selectedDifficulty}).");
        }

        if (selectedSong != null)
        {
            string computedName = selectedSong.GetChartFileName(selectedDifficulty);
            if (!string.IsNullOrEmpty(computedName))
            {
                fileToLoad = computedName;
                Debug.Log($"ChartNoteSpawner: Chart từ SelectedSongManager ({selectedDifficulty}): '{fileToLoad}'");
            }

            // Đổi AudioClip sang nhạc của bài được chọn
            if (playbackClock != null && selectedSong.audioClip != null)
            {
                playbackClock.SetClip(selectedSong.audioClip);
                Debug.Log($"ChartNoteSpawner: Audio → '{selectedSong.audioClip.name}'");
            }
        }

        RhythmTimelineAsset timelineToLoad = selectedSong != null
            ? selectedSong.GetTimelineAsset(selectedDifficulty)
            : null;

        if (!TryLoadSelectedChart(
                fileToLoad,
                timelineToLoad,
                out ChartData loadedChart,
                out _spawnDataList))
        {
            Debug.LogError("ChartNoteSpawner: Failed to get chart and spawn data.");
            return;
        }

        _laneCount = loadedChart.laneCount > 0 ? loadedChart.laneCount : 4;

        if (playbackClock != null)
        {
            float totalOffset = loadedChart.offset + RuntimeGameplaySettings.EffectiveAudioOffsetSeconds;
            playbackClock.SetOffset(totalOffset);
            // Master volume is applied at AudioListener level so previews, menus and chart music stay in sync.
            playbackClock.SetVolume(1f);
        }

        Debug.Log($"ChartNoteSpawner: Applied chart offset: {loadedChart.offset} | Player offset: {RuntimeGameplaySettings.AudioOffsetSeconds} | Platform offset: {RuntimeGameplaySettings.PlatformAudioOffsetSeconds} | Gameplay offset: {gameplayTimingOffsetSeconds}");

        ClearSpawnedNotes();

        _nextSpawnIndex = 0;
        _finishedNoteCount = 0;
        _finishedRuntimeNotes.Clear();
        _chartFinished = false;
        _isReady = true;

        Debug.Log($"ChartNoteSpawner ready. Notes to spawn: {_spawnDataList.Count}");
    }

    private static bool TryLoadSelectedChart(
        string chartFileNameToLoad,
        RhythmTimelineAsset timelineToLoad,
        out ChartData loadedChart,
        out List<ChartNoteSpawnData> spawnDataList)
    {
        if (timelineToLoad != null &&
            ChartSpawnDataProvider.TryGetChartAndSpawnData(
                timelineToLoad,
                out loadedChart,
                out spawnDataList))
        {
            Debug.Log($"ChartNoteSpawner: Loaded timeline asset '{timelineToLoad.name}'.");
            return true;
        }

        if (!string.IsNullOrWhiteSpace(chartFileNameToLoad) &&
            ChartSpawnDataProvider.TryGetChartAndSpawnData(
                chartFileNameToLoad,
                out loadedChart,
                out spawnDataList))
        {
            Debug.LogWarning(
                $"ChartNoteSpawner: Timeline asset was unavailable. Loaded JSON fallback '{chartFileNameToLoad}'.");
            return true;
        }

        loadedChart = null;
        spawnDataList = new List<ChartNoteSpawnData>();
        return false;
    }

    private void Update()
    {
        HandleScrollSpeedHotkeys();

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

        float songTime = playbackClock.SongTime + gameplayTimingOffsetSeconds;

        // Sync NoteManager time với audio clock để NoteMovement tính đúng vị trí.
        // SetExternalTime() tắt internal clock của NoteManager, tránh drift.
        noteManager.SetExternalTime(songTime);

        SpawnDueNotes(songTime);
    }

    public void AdjustScrollSpeed(float delta)
    {
        SetScrollSpeed(scrollSpeed + delta);
    }

    public void SetScrollSpeed(float newScrollSpeed)
    {
        float previousSpeed = scrollSpeed;
        scrollSpeed = Mathf.Clamp(newScrollSpeed, minScrollSpeed, maxScrollSpeed);

        if (Mathf.Approximately(previousSpeed, scrollSpeed))
            return;

        ApplyScrollSpeedToActiveNotes();
        SavePersistedScrollSpeed();

        if (logScrollSpeedChanges)
            Debug.Log($"ChartNoteSpawner: Scroll speed = {scrollSpeed:F0}");
    }

    private void SpawnDueNotes(float songTime)
    {
        while (_nextSpawnIndex < _spawnDataList.Count)
        {
            ChartNoteSpawnData data = _spawnDataList[_nextSpawnIndex];

            float spawnTime = data.hitTime - GetPreSpawnTime();

            if (songTime < spawnTime)
            {
                break;
            }

            SpawnNote(data);
            _nextSpawnIndex++;
        }
    }

    private float GetPreSpawnTime()
    {
        float safeScrollSpeed = Mathf.Max(1f, scrollSpeed);
        return Mathf.Max(preSpawnTime, minimumSpawnDistance / safeScrollSpeed);
    }

    private void SpawnNote(ChartNoteSpawnData data)
    {
        NoteBase noteBase = GetNote(data.noteType);

        if (noteBase == null)
        {
            Debug.LogError($"ChartNoteSpawner: Could not get note for type {data.noteType}.");
            return;
        }

        noteBase.name = $"Note_{data.noteId}_{data.noteType}_Lane{data.laneIndex}_Time{data.hitTime:F2}";

        NoteRuntimeData runtimeData = new NoteRuntimeData
        {
            noteId         = data.noteId,
            laneIndex      = data.laneIndex,
            noteType       = data.noteType,
            visualConfig   = visualConfig,
            hitTime        = data.hitTime,
            duration       = data.duration,
            anchoredX      = GetCenteredLaneX(data.laneIndex),
            hitlineY       = this.hitlineY,
            scrollSpeed    = this.scrollSpeed,
            touchRadius    = this.touchRadius,
            flickDirection = data.flickDirection,
            slidePath      = data.slidePath,
            laneSpacing    = this.laneSpacing
        };

        // Initialize sẽ set anchoredPosition.x và chuẩn bị NoteMovement.
        // NoteMovement.Tick() sẽ tính Y mỗi frame từ NoteManager.
        noteBase.Initialize(runtimeData);

        noteManager.RegisterNote(noteBase);

        if (logSpawnedNotes)
        {
            Debug.Log($"ChartNoteSpawner: Spawned + initialized note ID {data.noteId} " +
                      $"| Type {data.noteType} | Lane {data.laneIndex} | HitTime {data.hitTime:F2}");
        }
    }

    private void InitializeTypedPools()
    {
        _usingTypedPools = true;
        _typedPools.Clear();
        _pooledGeneratedNotes.Clear();
        _finishedRuntimeNotes.Clear();

        int typeCount = System.Enum.GetValues(typeof(NoteType)).Length;
        int perTypePoolSize = Mathf.Max(4, Mathf.CeilToInt(initialPoolSize / (float)typeCount));

        foreach (NoteType noteType in System.Enum.GetValues(typeof(NoteType)))
        {
            Queue<NoteBase> pool = new Queue<NoteBase>();
            _typedPools[noteType] = pool;

            for (int i = 0; i < perTypePoolSize; i++)
            {
                NoteBase note = CreateGeneratedNote(noteType);
                note.gameObject.SetActive(false);
                pool.Enqueue(note);
                _pooledGeneratedNotes.Add(note);
            }
        }

        Debug.Log($"ChartNoteSpawner: Initialized generated typed note pools. Per type: {perTypePoolSize}");
    }

    private NoteBase GetNote(NoteType noteType)
    {
        if (!_usingTypedPools)
            return NotePool.Instance != null ? NotePool.Instance.GetNote(Vector3.zero, Quaternion.identity) : null;

        if (!_typedPools.TryGetValue(noteType, out Queue<NoteBase> pool))
            pool = _typedPools[noteType] = new Queue<NoteBase>();

        if (pool.Count == 0)
        {
            NoteBase createdNote = CreateGeneratedNote(noteType);
            createdNote.gameObject.SetActive(false);
            pool.Enqueue(createdNote);
            _pooledGeneratedNotes.Add(createdNote);
        }

        NoteBase note = pool.Dequeue();
        _pooledGeneratedNotes.Remove(note);
        _finishedRuntimeNotes.Remove(note);
        note.transform.SetParent(noteParent, false);
        note.transform.localRotation = Quaternion.identity;
        note.gameObject.SetActive(true);

        return note;
    }

    private NoteBase CreateGeneratedNote(NoteType noteType)
    {
        GameObject noteObject = new GameObject(
            $"{noteType}_Note",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(NoteMovement)
        );

        noteObject.layer = noteParent != null ? noteParent.gameObject.layer : gameObject.layer;
        noteObject.transform.SetParent(noteParent, false);

        RectTransform rectTransform = noteObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(64f, 64f);

        Image image = noteObject.GetComponent<Image>();
        image.raycastTarget = true;

        if (noteType == NoteType.Slide)
            noteObject.AddComponent<SlideCheckpointSystem>();

        return noteType switch
        {
            NoteType.Hold => noteObject.AddComponent<HoldNote>(),
            NoteType.Flick => noteObject.AddComponent<FlickNote>(),
            NoteType.Slide => noteObject.AddComponent<SlideNote>(),
            _ => noteObject.AddComponent<TapNote>()
        };
    }

    private void HandleGeneratedNoteFinished(NoteBase note, NoteResult result)
    {
        if (note == null)
            return;

        if (!_finishedRuntimeNotes.Add(note))
            return;

        _finishedNoteCount++;
        ReturnGeneratedNote(note);

        if (!_chartFinished &&
            _spawnDataList != null &&
            _nextSpawnIndex >= _spawnDataList.Count &&
            _finishedNoteCount >= _spawnDataList.Count)
        {
            _chartFinished = true;
            OnChartFinished?.Invoke();
        }
    }

    private void ReturnGeneratedNote(NoteBase note)
    {
        if (!_usingTypedPools || note == null)
            return;

        if (_pooledGeneratedNotes.Contains(note))
            return;

        note.gameObject.SetActive(false);

        if (!_typedPools.TryGetValue(note.NoteType, out Queue<NoteBase> pool))
            pool = _typedPools[note.NoteType] = new Queue<NoteBase>();

        pool.Enqueue(note);
        _pooledGeneratedNotes.Add(note);
    }

    private void SubscribeToNoteManager()
    {
        if (noteManager == null || _listeningToNoteManager)
            return;

        noteManager.OnNoteFinishedEvent += HandleGeneratedNoteFinished;
        _listeningToNoteManager = true;
    }

    private void UnsubscribeFromNoteManager()
    {
        if (noteManager == null || !_listeningToNoteManager)
            return;

        noteManager.OnNoteFinishedEvent -= HandleGeneratedNoteFinished;
        _listeningToNoteManager = false;
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
        if (noteParent == null) return;

        // Trả tất cả nốt đang active về Pool thay vì Destroy
        for (int i = noteParent.childCount - 1; i >= 0; i--)
        {
            Transform child = noteParent.GetChild(i);
            if (child.gameObject.activeSelf)
            {
                NoteBase note = child.GetComponent<NoteBase>();
                if (_usingTypedPools && note != null)
                {
                    ReturnGeneratedNote(note);
                }
                else if (note != null && NotePool.Instance != null)
                {
                    NotePool.Instance.ReturnNote(note);
                }
                else
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    private void HandleScrollSpeedHotkeys()
    {
        if (!enableScrollSpeedHotkeys)
            return;

        if (WasKeyPressedThisFrame(increaseSpeedKey))
            AdjustScrollSpeed(scrollSpeedStep);

        if (WasKeyPressedThisFrame(decreaseSpeedKey))
            AdjustScrollSpeed(-scrollSpeedStep);
    }

    private void LoadPersistedScrollSpeed()
    {
        if (!persistScrollSpeed)
            return;

        scrollSpeed = Mathf.Clamp(
            RuntimeGameplaySettings.ScrollSpeed,
            minScrollSpeed,
            maxScrollSpeed);

        if (logScrollSpeedChanges)
            Debug.Log($"ChartNoteSpawner: Loaded scroll speed = {scrollSpeed:F0}");
    }

    private void EnsureGameplayUi()
    {
        if (GameObject.Find("RG Runtime Hit Effect Receiver") == null)
            new GameObject("RG Runtime Hit Effect Receiver").AddComponent<HitEffectSpriteReceiver>();

        if (GameObject.Find("RG Runtime Tap Sound Receiver") == null)
            new GameObject("RG Runtime Tap Sound Receiver").AddComponent<TapSoundEffectReceiver>();

        ComboManager runtimeComboManager = EnsureRuntimeComboManager();

        EnsureRuntimeComboDisplay(runtimeComboManager);

        if (FindFirstObjectByType<GameplayHudController>() == null)
            new GameObject("RG Gameplay HUD Controller").AddComponent<GameplayHudController>();

        if (FindFirstObjectByType<GameplayPauseController>() == null)
            new GameObject("RG Gameplay Pause Controller").AddComponent<GameplayPauseController>();
    }

    private ComboManager EnsureRuntimeComboManager()
    {
        GameObject managerObject = GameObject.Find("RG Runtime Combo Manager");
        ComboManager comboManager = managerObject != null
            ? managerObject.GetComponent<ComboManager>()
            : null;

        if (comboManager == null)
        {
            managerObject = new GameObject("RG Runtime Combo Manager");
            comboManager = managerObject.AddComponent<ComboManager>();
        }

        comboManager.BindNoteManager(noteManager != null ? noteManager : FindFirstObjectByType<NoteManager>());
        comboManager.BindConfig(LoadRuntimeComboConfig());
        return comboManager;
    }

    private void EnsureRuntimeComboDisplay(ComboManager comboManager)
    {
        Canvas canvas = RuntimeCanvasUtility.FindSceneCanvas();
        if (canvas == null || comboManager == null)
            return;

        ComboDisplay sceneDisplay = FindExistingComboDisplay();
        if (sceneDisplay != null)
        {
            BindExistingComboDisplay(sceneDisplay, comboManager, comboDisplayPosition);
            return;
        }

        if (TryCreateComboDisplayFromPrefab(canvas, comboManager, comboDisplayPosition))
            return;

        CreateSimpleRuntimeComboDisplay(canvas, comboManager, comboDisplayPosition);
    }

    private static ComboDisplay FindExistingComboDisplay()
    {
        ComboDisplay[] displays = FindObjectsByType<ComboDisplay>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (ComboDisplay display in displays)
        {
            if (display == null)
                continue;

            if (display.name == "RG Combo Runtime Display")
                continue;

            return display;
        }

        return null;
    }

    private static void BindExistingComboDisplay(ComboDisplay display, ComboManager comboManager, Vector2 anchoredPosition)
    {
        if (display == null || comboManager == null)
            return;

        CanvasGroup canvasGroup = display.GetComponent<CanvasGroup>();
        TextMeshProUGUI labelText = FindChildText(display.transform, "Combo_Label");
        TextMeshProUGUI numberText = FindChildText(display.transform, "Combo_Number");
        display.BindRuntimeReferences(comboManager, labelText, numberText, canvasGroup);

        RectTransform rect = display.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = anchoredPosition;
            rect.SetAsLastSibling();
        }

        display.gameObject.SetActive(true);
    }

    private static ComboConfig LoadRuntimeComboConfig()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<ComboConfig>("Assets/_Game/Data/Combo/ComboConfig.asset");
#else
        return null;
#endif
    }

    private static bool TryCreateComboDisplayFromPrefab(Canvas canvas, ComboManager comboManager, Vector2 anchoredPosition)
    {
#if UNITY_EDITOR
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Core/UI/ComboDisplay_Root.prefab");
        if (prefab == null)
            return false;

        GameObject rootObject = Instantiate(prefab, canvas.transform, false);
        rootObject.name = "RG Combo Runtime Display";
        rootObject.transform.SetAsLastSibling();

        RectTransform rect = rootObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
        }

        ComboDisplay display = rootObject.GetComponent<ComboDisplay>();
        CanvasGroup canvasGroup = rootObject.GetComponent<CanvasGroup>();
        TextMeshProUGUI labelText = FindChildText(rootObject.transform, "Combo_Label");
        TextMeshProUGUI numberText = FindChildText(rootObject.transform, "Combo_Number");

        if (display != null)
            display.BindRuntimeReferences(comboManager, labelText, numberText, canvasGroup);

        return display != null;
#else
        return false;
#endif
    }

    private static TextMeshProUGUI FindChildText(Transform root, string childName)
    {
        TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            if (text != null && text.name == childName)
                return text;
        }

        return null;
    }

    private void CreateSimpleRuntimeComboDisplay(Canvas canvas, ComboManager comboManager, Vector2 anchoredPosition)
    {
        GameObject rootObject = new GameObject(
            "RG Combo Runtime Display",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(VerticalLayoutGroup)
        );
        rootObject.SetActive(false);
        rootObject.transform.SetParent(canvas.transform, false);
        rootObject.transform.SetAsLastSibling();

        RectTransform rect = rootObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(280f, 150f);
        rect.anchoredPosition = anchoredPosition;

        CanvasGroup canvasGroup = rootObject.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        VerticalLayoutGroup layout = rootObject.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = -6f;

        TextMeshProUGUI labelText = CreateRuntimeComboText(rootObject.transform, "Combo Label", "COMBO", 24, 44f);
        labelText.alpha = 0.75f;
        labelText.characterSpacing = 10f;

        TextMeshProUGUI numberText = CreateRuntimeComboText(rootObject.transform, "Combo Number", "0", 72, 96f);

        ComboDisplay display = rootObject.AddComponent<ComboDisplay>();
        display.BindRuntimeReferences(comboManager, labelText, numberText, canvasGroup);

        rootObject.SetActive(true);
    }

    private static TextMeshProUGUI CreateRuntimeComboText(
        Transform parent,
        string objectName,
        string text,
        int fontSize,
        float preferredHeight)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(TextMeshProUGUI),
            typeof(LayoutElement)
        );
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280f, preferredHeight);

        LayoutElement layoutElement = textObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = preferredHeight;

        TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    private void SavePersistedScrollSpeed()
    {
        if (!persistScrollSpeed)
            return;

        RuntimeGameplaySettings.ScrollSpeed = scrollSpeed;
        PlayerPrefs.Save();
    }

    private static bool WasKeyPressedThisFrame(KeyCode key)
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(key))
            return true;
#endif

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        return key switch
        {
            KeyCode.F3 => keyboard.f3Key.wasPressedThisFrame,
            KeyCode.F4 => keyboard.f4Key.wasPressedThisFrame,
            _ => false
        };
#else
        return false;
#endif
    }

    private void ApplyScrollSpeedToActiveNotes()
    {
        if (noteParent == null)
            return;

        NoteBase[] notes = noteParent.GetComponentsInChildren<NoteBase>(false);
        foreach (NoteBase note in notes)
        {
            if (note != null)
                note.ApplyScrollSpeed(scrollSpeed);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromNoteManager();
    }
}
