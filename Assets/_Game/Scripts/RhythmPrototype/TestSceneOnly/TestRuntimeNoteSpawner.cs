using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 1.2f;
    [SerializeField] private float travelTime = 2.5f;
    [SerializeField] private float hitlineY = -520f;
    [SerializeField] private float scrollSpeed = 500f;

    [Header("Layout")]
    [SerializeField] private float laneWidth = 180f;
    [SerializeField] private float noteSize = 120f;

    [Header("Touch")]
    [SerializeField] private float defaultTouchRadius = 130f;

    private RectTransform canvasRect;
    private RectTransform root;
    private RectTransform hitline;

    private float timer;
    private int noteIdCounter;
    private int nextTypeIndex;

    private readonly NoteType[] spawnOrder =
    {
        NoteType.Tap,
        NoteType.Hold,
        NoteType.Flick,
        NoteType.Slide
    };

    private void Start()
    {
        if (targetCanvas == null)
            targetCanvas = FindFirstObjectByType<Canvas>();

        if (noteManager == null)
            noteManager = FindFirstObjectByType<NoteManager>();

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

        canvasRect = targetCanvas.GetComponent<RectTransform>();

        CreateRoot();
        CreateHitline();
        CreateLaneGuides();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnNextTestNote();
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
        GameObject hitlineObject = new GameObject("TEST_HITLINE", typeof(RectTransform), typeof(Image));

        hitlineObject.transform.SetParent(root, false);

        hitline = hitlineObject.GetComponent<RectTransform>();
        hitline.anchorMin = new Vector2(0.5f, 0.5f);
        hitline.anchorMax = new Vector2(0.5f, 0.5f);
        hitline.pivot = new Vector2(0.5f, 0.5f);
        hitline.sizeDelta = new Vector2(900f, 8f);
        hitline.anchoredPosition = new Vector2(0f, hitlineY);

        Image image = hitlineObject.GetComponent<Image>();
        image.color = Color.red;
    }

    private void CreateLaneGuides()
    {
        for (int i = 0; i < 4; i++)
        {
            float x = LaneToX(i);

            GameObject laneObject = new GameObject($"TEST_LANE_{i}", typeof(RectTransform), typeof(Image));

            laneObject.transform.SetParent(root, false);

            RectTransform rect = laneObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(laneWidth - 10f, 1400f);
            rect.anchoredPosition = new Vector2(x, 0f);

            Image image = laneObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.08f);
        }
    }

    private void SpawnNextTestNote()
    {
        NoteType type = spawnOrder[nextTypeIndex];

        nextTypeIndex++;
        if (nextTypeIndex >= spawnOrder.Length)
            nextTypeIndex = 0;

        int laneIndex = Random.Range(0, 4);

        switch (type)
        {
            case NoteType.Tap:
                SpawnTap(laneIndex);
                break;

            case NoteType.Hold:
                SpawnHold(laneIndex);
                break;

            case NoteType.Flick:
                SpawnFlick(laneIndex);
                break;

            case NoteType.Slide:
                SpawnSlide(laneIndex);
                break;
        }
    }

    private void SpawnTap(int laneIndex)
    {
        GameObject obj = CreateBaseNoteObject("TEST_TAP_NOTE", laneIndex, new Vector2(noteSize, noteSize));

        obj.AddComponent<NoteMovement>();
        TapNote note = obj.AddComponent<TapNote>();

        Image image = obj.GetComponent<Image>();
        image.color = Color.white;

        InitializeAndRegister(note, laneIndex, 0.1f, FlickDirection.Any);
    }

    private void SpawnHold(int laneIndex)
    {
        GameObject obj = CreateBaseNoteObject("TEST_HOLD_NOTE", laneIndex, new Vector2(noteSize, noteSize * 1.8f));

        obj.AddComponent<NoteMovement>();
        HoldNote note = obj.AddComponent<HoldNote>();

        Image image = obj.GetComponent<Image>();
        image.color = Color.white;

        InitializeAndRegister(note, laneIndex, 1.25f, FlickDirection.Any);
    }

    private void SpawnFlick(int laneIndex)
    {
        GameObject obj = CreateBaseNoteObject("TEST_FLICK_NOTE", laneIndex, new Vector2(noteSize, noteSize));

        obj.AddComponent<NoteMovement>();
        FlickNote note = obj.AddComponent<FlickNote>();

        Image image = obj.GetComponent<Image>();
        image.color = Color.white;

        FlickDirection dir = RandomFlickDirection();

        CreateTextChild(obj.transform, DirectionText(dir), new Vector2(0f, 0f), 44);

        InitializeAndRegister(note, laneIndex, 0.1f, dir);
    }

    private void SpawnSlide(int laneIndex)
    {
        GameObject obj = CreateBaseNoteObject("TEST_SLIDE_NOTE", laneIndex, new Vector2(80f, 80f));

        obj.AddComponent<NoteMovement>();
        SlideCheckpointSystem checkpointSystem = obj.AddComponent<SlideCheckpointSystem>();
        SlideNote note = obj.AddComponent<SlideNote>();

        Image image = obj.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.25f);

        List<RectTransform> checkpoints = CreateSlideCheckpoints(obj.transform);

        checkpointSystem.Initialize(checkpoints, 95f);

        InitializeAndRegister(note, laneIndex, 2.0f, FlickDirection.Any);
    }

    private GameObject CreateBaseNoteObject(string objectName, int laneIndex, Vector2 size)
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
        rect.pivot = new Vector2(0.5f, 0.5f);
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

    private List<RectTransform> CreateSlideCheckpoints(Transform parent)
    {
        List<RectTransform> result = new List<RectTransform>();

        Vector2[] localPositions =
        {
            new Vector2(0f, 0f),
            new Vector2(100f, 90f),
            new Vector2(-80f, 180f),
            new Vector2(80f, 280f)
        };

        for (int i = 0; i < localPositions.Length; i++)
        {
            GameObject checkpoint = new GameObject(
                $"Slide_Checkpoint_{i}",
                typeof(RectTransform),
                typeof(Image)
            );

            checkpoint.transform.SetParent(parent, false);

            RectTransform rect = checkpoint.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(65f, 65f);
            rect.anchoredPosition = localPositions[i];

            Image image = checkpoint.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;

            result.Add(rect);
        }

        return result;
    }

    private void CreateTextChild(Transform parent, string text, Vector2 anchoredPosition, int fontSize)
    {
        GameObject textObject = new GameObject("TEST_TEXT", typeof(RectTransform), typeof(Text));

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