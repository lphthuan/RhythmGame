using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum TestSpawnDifficulty
{
    Normal,
    Hard,
    Expert,
    Insane
}

// TEST SCENE ONLY
// Script này tự sinh note runtime để test Tap / Hold / Flick / Slide.
// Không đem qua scene chính.
// Không phải Note Spawner thật của game.
// Không dùng Object Pool.
// Không dùng Beatmap Parser.
public class TestRuntimeNoteSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private NoteManager noteManager;

    [Header("Difficulty TEST")]
    [SerializeField] private TestSpawnDifficulty difficulty = TestSpawnDifficulty.Normal;
    [SerializeField] private bool applyDifficultyPresetOnStart = true;

    [SerializeField] private float parallelPatternChance = 0.0f;
    [SerializeField] private int maxParallelNotes = 1;
    [SerializeField] private float slideHeavyPatternChance = 0.0f;

    [Header("Insane Density TEST")]
    [SerializeField] private float insaneBurstChance = 0.65f;
    [SerializeField] private int insaneMinBurstWaves = 3;
    [SerializeField] private int insaneMaxBurstWaves = 6;
    [SerializeField] private float insaneMinWaveDelay = 0.08f;
    [SerializeField] private float insaneMaxWaveDelay = 0.16f;
    [SerializeField] private float insaneTapFlickLaneLock = 0.03f;

    [Header("Spawn / Movement Settings")]
    [SerializeField] private float travelTime = 2.1f;
    [SerializeField] private float hitlineY = -330f;
    [SerializeField] private float scrollSpeed = 420f;

    [Header("Landscape Layout")]
    [SerializeField] private float laneWidth = 300f;
    [SerializeField] private float noteSize = 120f;

    [Header("Touch")]
    [SerializeField] private float defaultTouchRadius = 140f;

    [Header("Smart Random Weight TEST")]
    [SerializeField] private float tapWeight = 40f;
    [SerializeField] private float flickWeight = 25f;
    [SerializeField] private float holdWeight = 20f;
    [SerializeField] private float slideWeight = 15f;

    [Header("Spawn Delay TEST")]
    [SerializeField] private float minSmallNoteDelay = 0.55f;
    [SerializeField] private float maxSmallNoteDelay = 0.9f;

    [SerializeField] private float minLongNoteDelay = 1.2f;
    [SerializeField] private float maxLongNoteDelay = 2.0f;

    [SerializeField] private float laneReleaseBuffer = 0.35f;

    [Header("Hold Smart Size TEST")]
    [SerializeField] private float minHoldVisualHeight = 250f;
    [SerializeField] private float maxHoldVisualHeight = 700f;

    [Header("Slide Smart Path TEST")]
    [SerializeField] private int minSlideCheckpoints = 3;
    [SerializeField] private int maxSlideCheckpoints = 5;
    [SerializeField] private float slideVerticalSpacing = 130f;
    [SerializeField] private int maxLaneStepPerCheckpoint = 2;
    [SerializeField] private float slideCheckpointRadius = 125f;

    private RectTransform canvasRect;
    private RectTransform root;
    private RectTransform hitline;

    private float timer;
    private float nextSpawnDelay = 0.8f;

    private int noteIdCounter;

    private bool isBurstRunning;

    private readonly float[] laneBusyUntil = new float[4];

    private void Start()
    {
        ResolveReferences();

        if (targetCanvas == null)
        {
            Debug.LogError("TestRuntimeNoteSpawner needs a Canvas.");
            enabled = false;
            return;
        }

        if (noteManager == null)
        {
            Debug.LogError("TestRuntimeNoteSpawner needs a NoteManager.");
            enabled = false;
            return;
        }

        if (applyDifficultyPresetOnStart)
        {
            ApplyDifficultyPreset();
        }

        canvasRect = targetCanvas.GetComponent<RectTransform>();

        CreateRoot();
        CreateHitline();
        CreateLaneGuides();

        nextSpawnDelay = 0.8f;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= nextSpawnDelay)
        {
            timer = 0f;
            SpawnByDifficulty();
        }
    }

    private void ResolveReferences()
    {
        if (targetCanvas == null)
            targetCanvas = FindFirstObjectByType<Canvas>();

        if (noteManager == null)
            noteManager = FindFirstObjectByType<NoteManager>();
    }

    private void ApplyDifficultyPreset()
    {
        switch (difficulty)
        {
            case TestSpawnDifficulty.Normal:
                travelTime = 2.1f;
                scrollSpeed = 420f;

                tapWeight = 40f;
                flickWeight = 25f;
                holdWeight = 20f;
                slideWeight = 15f;

                minSmallNoteDelay = 0.55f;
                maxSmallNoteDelay = 0.9f;
                minLongNoteDelay = 1.2f;
                maxLongNoteDelay = 2.0f;

                laneReleaseBuffer = 0.35f;

                minHoldVisualHeight = 250f;
                maxHoldVisualHeight = 700f;

                minSlideCheckpoints = 3;
                maxSlideCheckpoints = 5;
                slideVerticalSpacing = 130f;
                maxLaneStepPerCheckpoint = 2;
                slideCheckpointRadius = 125f;

                parallelPatternChance = 0f;
                maxParallelNotes = 1;
                slideHeavyPatternChance = 0f;

                insaneBurstChance = 0f;
                insaneMinBurstWaves = 0;
                insaneMaxBurstWaves = 0;
                insaneMinWaveDelay = 0.1f;
                insaneMaxWaveDelay = 0.2f;
                insaneTapFlickLaneLock = 0.03f;
                break;

            case TestSpawnDifficulty.Hard:
                travelTime = 1.7f;
                scrollSpeed = 520f;

                tapWeight = 45f;
                flickWeight = 25f;
                holdWeight = 18f;
                slideWeight = 12f;

                minSmallNoteDelay = 0.35f;
                maxSmallNoteDelay = 0.6f;
                minLongNoteDelay = 0.9f;
                maxLongNoteDelay = 1.4f;

                laneReleaseBuffer = 0.2f;

                minHoldVisualHeight = 230f;
                maxHoldVisualHeight = 650f;

                minSlideCheckpoints = 3;
                maxSlideCheckpoints = 6;
                slideVerticalSpacing = 120f;
                maxLaneStepPerCheckpoint = 2;
                slideCheckpointRadius = 115f;

                parallelPatternChance = 0.25f;
                maxParallelNotes = 2;
                slideHeavyPatternChance = 0.15f;

                insaneBurstChance = 0f;
                insaneMinBurstWaves = 0;
                insaneMaxBurstWaves = 0;
                insaneMinWaveDelay = 0.1f;
                insaneMaxWaveDelay = 0.2f;
                insaneTapFlickLaneLock = 0.03f;
                break;

            case TestSpawnDifficulty.Expert:
                travelTime = 1.35f;
                scrollSpeed = 620f;

                tapWeight = 50f;
                flickWeight = 25f;
                holdWeight = 12f;
                slideWeight = 13f;

                minSmallNoteDelay = 0.22f;
                maxSmallNoteDelay = 0.42f;
                minLongNoteDelay = 0.65f;
                maxLongNoteDelay = 1.1f;

                laneReleaseBuffer = 0.1f;

                minHoldVisualHeight = 220f;
                maxHoldVisualHeight = 620f;

                minSlideCheckpoints = 4;
                maxSlideCheckpoints = 7;
                slideVerticalSpacing = 100f;
                maxLaneStepPerCheckpoint = 2;
                slideCheckpointRadius = 110f;

                parallelPatternChance = 0.45f;
                maxParallelNotes = 3;
                slideHeavyPatternChance = 0.25f;

                insaneBurstChance = 0f;
                insaneMinBurstWaves = 0;
                insaneMaxBurstWaves = 0;
                insaneMinWaveDelay = 0.1f;
                insaneMaxWaveDelay = 0.2f;
                insaneTapFlickLaneLock = 0.03f;
                break;

            case TestSpawnDifficulty.Insane:
                travelTime = 1.15f;
                scrollSpeed = 700f;

                tapWeight = 58f;
                flickWeight = 25f;
                holdWeight = 7f;
                slideWeight = 10f;

                minSmallNoteDelay = 0.18f;
                maxSmallNoteDelay = 0.34f;
                minLongNoteDelay = 0.45f;
                maxLongNoteDelay = 0.85f;

                laneReleaseBuffer = 0.08f;

                minHoldVisualHeight = 180f;
                maxHoldVisualHeight = 480f;

                minSlideCheckpoints = 4;
                maxSlideCheckpoints = 7;
                slideVerticalSpacing = 95f;
                maxLaneStepPerCheckpoint = 2;
                slideCheckpointRadius = 105f;

                parallelPatternChance = 0.58f;
                maxParallelNotes = 3;
                slideHeavyPatternChance = 0.25f;

                insaneBurstChance = 0.55f;
                insaneMinBurstWaves = 2;
                insaneMaxBurstWaves = 4;
                insaneMinWaveDelay = 0.20f;
                insaneMaxWaveDelay = 0.32f;
                insaneTapFlickLaneLock = 0.18f;
                break;
        }
    }

    private void CreateRoot()
    {
        GameObject rootObject = new GameObject("TEST_NOTE_ROOT", typeof(RectTransform));
        rootObject.transform.SetParent(targetCanvas.transform, false);

        root = rootObject.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = canvasRect.sizeDelta;
        root.anchoredPosition = Vector2.zero;
    }

    private void CreateHitline()
    {
        GameObject hitlineObject = new GameObject(
            "TEST_HITLINE",
            typeof(RectTransform),
            typeof(Image)
        );

        hitlineObject.transform.SetParent(root, false);

        hitline = hitlineObject.GetComponent<RectTransform>();
        hitline.anchorMin = new Vector2(0.5f, 0.5f);
        hitline.anchorMax = new Vector2(0.5f, 0.5f);
        hitline.pivot = new Vector2(0.5f, 0.5f);
        hitline.sizeDelta = new Vector2(1200f, 8f);
        hitline.anchoredPosition = new Vector2(0f, hitlineY);

        Image image = hitlineObject.GetComponent<Image>();
        image.color = Color.red;
        image.raycastTarget = false;
    }

    private void CreateLaneGuides()
    {
        for (int i = 0; i < 4; i++)
        {
            float x = LaneToX(i);

            GameObject laneObject = new GameObject(
                $"TEST_LANE_{i}",
                typeof(RectTransform),
                typeof(Image)
            );

            laneObject.transform.SetParent(root, false);

            RectTransform rect = laneObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(laneWidth - 10f, 1400f);
            rect.anchoredPosition = new Vector2(x, 0f);

            Image image = laneObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.08f);
            image.raycastTarget = false;
        }
    }

    private void SpawnByDifficulty()
    {
        if (difficulty == TestSpawnDifficulty.Insane)
        {
            SpawnInsaneMode();
            return;
        }

        float roll = Random.value;

        if (roll < parallelPatternChance)
        {
            SpawnParallelPattern();
            return;
        }

        roll -= parallelPatternChance;

        if (roll < slideHeavyPatternChance)
        {
            SpawnSlideHeavyPattern();
            return;
        }

        SpawnSmartRandomNote();
    }

    private void SpawnInsaneMode()
    {
        if (isBurstRunning)
        {
            nextSpawnDelay = 0.12f;
            return;
        }

        float roll = Random.value;

        if (roll < insaneBurstChance)
        {
            StartCoroutine(SpawnInsaneBurstRoutine());
            nextSpawnDelay = Random.Range(0.20f, 0.35f);
            return;
        }

        roll -= insaneBurstChance;

        if (roll < slideHeavyPatternChance)
        {
            SpawnSlideHeavyPattern();
            return;
        }

        SpawnParallelPattern();
    }

    private IEnumerator SpawnInsaneBurstRoutine()
    {
        isBurstRunning = true;

        int minWaves = Mathf.Max(1, insaneMinBurstWaves);
        int maxWaves = Mathf.Max(minWaves, insaneMaxBurstWaves);

        int waveCount = Random.Range(minWaves, maxWaves + 1);

        for (int wave = 0; wave < waveCount; wave++)
        {
            SpawnInsaneTapFlickWave();

            float delay = Random.Range(insaneMinWaveDelay, insaneMaxWaveDelay);
            yield return new WaitForSeconds(delay);
        }

        isBurstRunning = false;

        nextSpawnDelay = Random.Range(0.25f, 0.45f);
    }

    private void SpawnInsaneTapFlickWave()
    {
        int clampedMax = Mathf.Clamp(maxParallelNotes, 2, 4);
        int noteCount = Random.Range(2, clampedMax + 1);

        List<int> lanes = GetRandomAvailableLanes(noteCount);

        if (lanes.Count == 0)
            return;

        float laneLock = GetMinimumTapFlickLaneLock();

        for (int i = 0; i < lanes.Count; i++)
        {
            int lane = lanes[i];

            if (Random.value < 0.75f)
            {
                SpawnTap(lane);
            }
            else
            {
                SpawnFlick(lane);
            }

            ReserveLane(lane, laneLock);
        }
    }

    private void SpawnSmartRandomNote()
    {
        NoteType type = GetWeightedRandomNoteType();

        bool spawned = false;

        for (int attempt = 0; attempt < 12; attempt++)
        {
            int laneIndex = Random.Range(0, 4);

            if (!IsLaneAvailable(laneIndex))
                continue;

            switch (type)
            {
                case NoteType.Tap:
                    SpawnTap(laneIndex);
                    ReserveLane(laneIndex, travelTime + laneReleaseBuffer);
                    nextSpawnDelay = Random.Range(minSmallNoteDelay, maxSmallNoteDelay);
                    spawned = true;
                    break;

                case NoteType.Flick:
                    SpawnFlick(laneIndex);
                    ReserveLane(laneIndex, travelTime + laneReleaseBuffer);
                    nextSpawnDelay = Random.Range(minSmallNoteDelay, maxSmallNoteDelay);
                    spawned = true;
                    break;

                case NoteType.Hold:
                    {
                        float holdHeight = GetRandomHoldHeight();
                        float holdDuration = holdHeight / scrollSpeed;

                        SpawnHoldWithHeight(laneIndex, holdHeight, holdDuration);

                        ReserveLane(laneIndex, travelTime + holdDuration + laneReleaseBuffer);

                        nextSpawnDelay = Random.Range(minLongNoteDelay, maxLongNoteDelay);
                        spawned = true;
                        break;
                    }

                case NoteType.Slide:
                    {
                        int[] lanePath = CreateSmartSlideLanePath(laneIndex);

                        if (!AreSlideLanesAvailable(lanePath))
                            continue;

                        float slideDuration = CalculateSlideDuration(lanePath.Length);

                        SpawnSlideWithPath(lanePath, slideDuration);

                        ReserveSlideLanes(
                            lanePath,
                            travelTime + slideDuration + laneReleaseBuffer
                        );

                        nextSpawnDelay = Random.Range(minLongNoteDelay, maxLongNoteDelay);
                        spawned = true;
                        break;
                    }
            }

            if (spawned)
                break;
        }

        if (!spawned)
        {
            nextSpawnDelay = 0.2f;
        }
    }

    private void SpawnParallelPattern()
    {
        int clampedMax = Mathf.Clamp(maxParallelNotes, 2, 4);
        int noteCount = Random.Range(2, clampedMax + 1);

        List<int> lanes = GetRandomAvailableLanes(noteCount);

        if (lanes.Count == 0)
        {
            nextSpawnDelay = 0.16f;
            return;
        }

        float laneLock;

        if (difficulty == TestSpawnDifficulty.Insane)
        {
            laneLock = GetMinimumTapFlickLaneLock();
        }
        else
        {
            laneLock = travelTime + laneReleaseBuffer;
        }

        for (int i = 0; i < lanes.Count; i++)
        {
            int lane = lanes[i];

            if (Random.value < 0.7f)
            {
                SpawnTap(lane);
            }
            else
            {
                SpawnFlick(lane);
            }

            ReserveLane(lane, laneLock);
        }

        nextSpawnDelay = Random.Range(minSmallNoteDelay, maxSmallNoteDelay);
    }

    private void SpawnSlideHeavyPattern()
    {
        int startLane = Random.Range(0, 4);

        if (!IsLaneAvailable(startLane))
        {
            nextSpawnDelay = 0.2f;
            return;
        }

        int[] lanePath = CreateSmartSlideLanePath(startLane);

        if (!AreSlideLanesAvailable(lanePath))
        {
            nextSpawnDelay = 0.2f;
            return;
        }

        float slideDuration = CalculateSlideDuration(lanePath.Length);

        SpawnSlideWithPath(lanePath, slideDuration);

        ReserveSlideLanes(
            lanePath,
            travelTime + slideDuration + laneReleaseBuffer
        );

        int bonusLane = GetLaneNotInPath(lanePath);

        if (bonusLane >= 0 && IsLaneAvailable(bonusLane))
        {
            if (Random.value < 0.65f)
            {
                SpawnTap(bonusLane);
            }
            else
            {
                SpawnFlick(bonusLane);
            }

            ReserveLane(bonusLane, travelTime + laneReleaseBuffer);
        }

        nextSpawnDelay = Random.Range(minLongNoteDelay, maxLongNoteDelay);
    }

    private NoteType GetWeightedRandomNoteType()
    {
        float totalWeight = tapWeight + flickWeight + holdWeight + slideWeight;

        if (totalWeight <= 0f)
            return NoteType.Tap;

        float value = Random.Range(0f, totalWeight);

        if (value < tapWeight)
            return NoteType.Tap;

        value -= tapWeight;

        if (value < flickWeight)
            return NoteType.Flick;

        value -= flickWeight;

        if (value < holdWeight)
            return NoteType.Hold;

        return NoteType.Slide;
    }

    private void SpawnTap(int laneIndex)
    {
        GameObject obj = CreateBaseNoteObject(
            "TEST_TAP_NOTE",
            laneIndex,
            new Vector2(noteSize, noteSize)
        );

        obj.AddComponent<NoteMovement>();
        TapNote note = obj.AddComponent<TapNote>();

        Image image = obj.GetComponent<Image>();
        image.color = Color.white;

        InitializeAndRegister(note, laneIndex, 0.1f, FlickDirection.Any);
    }

    private void SpawnFlick(int laneIndex)
    {
        GameObject obj = CreateBaseNoteObject(
            "TEST_FLICK_NOTE",
            laneIndex,
            new Vector2(noteSize, noteSize)
        );

        obj.AddComponent<NoteMovement>();
        FlickNote note = obj.AddComponent<FlickNote>();

        Image image = obj.GetComponent<Image>();
        image.color = Color.white;

        FlickDirection direction = RandomFlickDirection();

        CreateTextChild(
            obj.transform,
            DirectionText(direction),
            Vector2.zero,
            44
        );

        InitializeAndRegister(note, laneIndex, 0.1f, direction);
    }

    private void SpawnHoldWithHeight(
        int laneIndex,
        float holdVisualHeight,
        float holdDuration
    )
    {
        GameObject obj = CreateBaseNoteObject(
            "TEST_HOLD_NOTE",
            laneIndex,
            new Vector2(noteSize, holdVisualHeight),
            new Vector2(0.5f, 0f)
        );

        obj.AddComponent<NoteMovement>();
        HoldNote note = obj.AddComponent<HoldNote>();

        Image image = obj.GetComponent<Image>();
        image.color = Color.white;

        InitializeAndRegister(note, laneIndex, holdDuration, FlickDirection.Any);
    }

    private void SpawnSlideWithPath(int[] lanePath, float slideDuration)
    {
        GameObject obj = CreateBaseNoteObject(
            "TEST_SLIDE_NOTE",
            lanePath[0],
            new Vector2(76f, 76f)
        );

        obj.AddComponent<NoteMovement>();
        SlideCheckpointSystem checkpointSystem = obj.AddComponent<SlideCheckpointSystem>();
        SlideNote note = obj.AddComponent<SlideNote>();

        Image image = obj.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.25f);

        List<RectTransform> checkpoints = CreateSlideCheckpointsAcrossLanes(
            obj.transform,
            lanePath
        );

        checkpointSystem.Initialize(checkpoints, slideCheckpointRadius);

        InitializeAndRegister(note, lanePath[0], slideDuration, FlickDirection.Any);
    }

    private GameObject CreateBaseNoteObject(
        string objectName,
        int laneIndex,
        Vector2 size,
        Vector2? customPivot = null
    )
    {
        GameObject obj = new GameObject(
            $"{objectName}_{noteIdCounter}",
            typeof(RectTransform),
            typeof(Image)
        );

        obj.transform.SetParent(root, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = customPivot ?? new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        float x = LaneToX(laneIndex);
        float spawnY = hitlineY + travelTime * scrollSpeed;

        rect.anchoredPosition = new Vector2(x, spawnY);

        Image image = obj.GetComponent<Image>();
        image.raycastTarget = false;

        return obj;
    }

    private void InitializeAndRegister(
        NoteBase note,
        int laneIndex,
        float duration,
        FlickDirection flickDirection
    )
    {
        float hitTime = noteManager.CurrentTime + travelTime;

        NoteRuntimeData data = new NoteRuntimeData
        {
            noteId = noteIdCounter,
            laneIndex = laneIndex,
            hitTime = hitTime,
            duration = duration,
            anchoredX = LaneToX(laneIndex),
            hitlineY = hitlineY,
            scrollSpeed = scrollSpeed,
            touchRadius = defaultTouchRadius,
            flickDirection = flickDirection
        };

        note.Initialize(data);
        noteManager.RegisterNote(note);

        noteIdCounter++;
    }

    private bool IsLaneAvailable(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= laneBusyUntil.Length)
            return false;

        return noteManager.CurrentTime >= laneBusyUntil[laneIndex];
    }

    private void ReserveLane(int laneIndex, float busyDuration)
    {
        if (laneIndex < 0 || laneIndex >= laneBusyUntil.Length)
            return;

        laneBusyUntil[laneIndex] = noteManager.CurrentTime + busyDuration;
    }

    private bool AreSlideLanesAvailable(int[] lanePath)
    {
        for (int i = 0; i < lanePath.Length; i++)
        {
            if (!IsLaneAvailable(lanePath[i]))
                return false;
        }

        return true;
    }

    private void ReserveSlideLanes(int[] lanePath, float busyDuration)
    {
        for (int i = 0; i < lanePath.Length; i++)
        {
            ReserveLane(lanePath[i], busyDuration);
        }
    }

    private List<int> GetRandomAvailableLanes(int wantedCount)
    {
        List<int> available = new List<int>();

        for (int lane = 0; lane < 4; lane++)
        {
            if (IsLaneAvailable(lane))
            {
                available.Add(lane);
            }
        }

        ShuffleList(available);

        if (available.Count > wantedCount)
        {
            available.RemoveRange(wantedCount, available.Count - wantedCount);
        }

        return available;
    }

    private List<int> GetRandomLanesAllowingDense(int wantedCount)
    {
        List<int> lanes = new List<int>();

        for (int lane = 0; lane < 4; lane++)
        {
            lanes.Add(lane);
        }

        ShuffleList(lanes);

        if (lanes.Count > wantedCount)
        {
            lanes.RemoveRange(wantedCount, lanes.Count - wantedCount);
        }

        return lanes;
    }

    private void ShuffleList(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);

            int temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private int GetLaneNotInPath(int[] lanePath)
    {
        List<int> candidates = new List<int>();

        for (int lane = 0; lane < 4; lane++)
        {
            bool used = false;

            for (int i = 0; i < lanePath.Length; i++)
            {
                if (lanePath[i] == lane)
                {
                    used = true;
                    break;
                }
            }

            if (!used && IsLaneAvailable(lane))
            {
                candidates.Add(lane);
            }
        }

        if (candidates.Count == 0)
            return -1;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private float GetRandomHoldHeight()
    {
        float canvasHeight = canvasRect.rect.height;

        float safeMaxHeight = canvasHeight * 0.65f;
        float maxHeight = Mathf.Min(maxHoldVisualHeight, safeMaxHeight);

        if (maxHeight < minHoldVisualHeight)
            maxHeight = minHoldVisualHeight;

        return Random.Range(minHoldVisualHeight, maxHeight);
    }

    private int[] CreateSmartSlideLanePath(int startLane)
    {
        int minCount = Mathf.Max(2, minSlideCheckpoints);
        int maxCount = Mathf.Max(minCount, maxSlideCheckpoints);

        int count = Random.Range(minCount, maxCount + 1);

        int[] path = new int[count];
        path[0] = Mathf.Clamp(startLane, 0, 3);

        int direction = Random.value < 0.5f ? -1 : 1;

        for (int i = 1; i < count; i++)
        {
            int previousLane = path[i - 1];

            if (Random.value < 0.35f)
                direction *= -1;

            int maxStep = Mathf.Clamp(maxLaneStepPerCheckpoint, 1, 3);
            int step = Random.Range(1, maxStep + 1);

            int nextLane = previousLane + direction * step;

            if (nextLane < 0 || nextLane > 3)
            {
                direction *= -1;
                nextLane = previousLane + direction * step;
            }

            nextLane = Mathf.Clamp(nextLane, 0, 3);

            if (nextLane == previousLane)
            {
                if (previousLane <= 1)
                    nextLane = previousLane + 1;
                else
                    nextLane = previousLane - 1;
            }

            path[i] = nextLane;
        }

        return path;
    }

    private float CalculateSlideDuration(int checkpointCount)
    {
        return Mathf.Max(1.1f, checkpointCount * 0.35f);
    }

    private List<RectTransform> CreateSlideCheckpointsAcrossLanes(
        Transform parent,
        int[] lanePath
    )
    {
        List<RectTransform> result = new List<RectTransform>();

        RectTransform parentRect = parent.GetComponent<RectTransform>();
        float baseX = parentRect.anchoredPosition.x;

        for (int i = 0; i < lanePath.Length; i++)
        {
            int lane = lanePath[i];

            float targetLaneX = LaneToX(lane);
            float localX = targetLaneX - baseX;
            float localY = i * slideVerticalSpacing;

            GameObject checkpoint = new GameObject(
                $"Slide_Checkpoint_Lane_{lane}_{i}",
                typeof(RectTransform),
                typeof(Image)
            );

            checkpoint.transform.SetParent(parent, false);

            RectTransform rect = checkpoint.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(72f, 72f);
            rect.anchoredPosition = new Vector2(localX, localY);

            Image image = checkpoint.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;

            CreateTextChild(
                checkpoint.transform,
                lane.ToString(),
                Vector2.zero,
                28
            );

            result.Add(rect);
        }

        return result;
    }

    private void CreateTextChild(
        Transform parent,
        string text,
        Vector2 anchoredPosition,
        int fontSize
    )
    {
        GameObject textObject = new GameObject(
            "TEST_TEXT",
            typeof(RectTransform),
            typeof(Text)
        );

        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(120f, 120f);
        rect.anchoredPosition = anchoredPosition;

        Text uiText = textObject.GetComponent<Text>();
        uiText.text = text;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.fontSize = fontSize;
        uiText.color = Color.black;
        uiText.raycastTarget = false;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font != null)
        {
            uiText.font = font;
        }
        else
        {
            Debug.LogWarning("Built-in font LegacyRuntime.ttf not found. Text may not render correctly.");
        }
    }

    private float GetMinimumTapFlickLaneLock()
    {
        float minVisualSpacing = noteSize * 1.35f;
        float timeSpacing = minVisualSpacing / scrollSpeed;

        return Mathf.Max(insaneTapFlickLaneLock, timeSpacing);
    }

    private float LaneToX(int laneIndex)
    {
        float startX = -laneWidth * 1.5f;
        return startX + laneIndex * laneWidth;
    }

    private FlickDirection RandomFlickDirection()
    {
        int value = Random.Range(0, 4);

        switch (value)
        {
            case 0:
                return FlickDirection.Up;

            case 1:
                return FlickDirection.Down;

            case 2:
                return FlickDirection.Left;

            default:
                return FlickDirection.Right;
        }
    }

    private string DirectionText(FlickDirection direction)
    {
        switch (direction)
        {
            case FlickDirection.Up:
                return "↑";

            case FlickDirection.Down:
                return "↓";

            case FlickDirection.Left:
                return "←";

            case FlickDirection.Right:
                return "→";

            default:
                return "*";
        }
    }
}