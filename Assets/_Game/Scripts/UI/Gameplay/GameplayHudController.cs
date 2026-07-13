using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameplayHudController : MonoBehaviour
{
    public static GameplayHudController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private NoteManager noteManager;
    [SerializeField] private ChartNoteSpawner chartSpawner;
    [SerializeField] private GameplayPauseController pauseController;

    [Header("Health")]
    [SerializeField, Range(0f, 100f)] private float startHealth = 50f;
    [SerializeField, Range(0f, 100f)] private float passHealth = 70f;
    [SerializeField] private float perfectHealthGain = 1.1f;
    [SerializeField] private float greatHealthGain = 0.7f;
    [SerializeField] private float goodHealthGain = 0.25f;
    [SerializeField] private float missHealthLoss = 4.5f;

    [Header("Layout")]
    [SerializeField] private Vector2 healthBarPosition = new Vector2(116f, 0f);
    [SerializeField] private Vector2 scorePanelPosition = new Vector2(-178f, -52f);
    [SerializeField] private Vector2 songInfoPosition = new Vector2(-172f, -135f);

    private const string RootName = "RG Gameplay Live HUD";
    private const string UiAssetFolder = "Assets/_Game/Sprites/UI/UIGamePlay/";

    private RectTransform _root;
    private RectTransform _healthFill;
    private TextMeshProUGUI _healthText;
    private TextMeshProUGUI _scoreText;
    private TextMeshProUGUI _difficultyText;
    private TextMeshProUGUI _songTitleText;
    private TextMeshProUGUI _artistText;
    private Image _coverImage;

    private int _perfect;
    private int _great;
    private int _good;
    private int _miss;
    private int _liveScore;
    private float _health;

    public int LiveScore => _liveScore;
    public float Health => _health;
    public bool IsPassed => _health >= passHealth;
    public float PassHealth => passHealth;

    private void Awake()
    {
        Instance = this;
        ResolveReferences();
        EnsureEventSystem();
    }

    private void Start()
    {
        BuildHud();
        RefreshSongInfo();
        ResetRunState();
    }

    private void OnEnable()
    {
        if (noteManager == null)
            noteManager = FindFirstObjectByType<NoteManager>();
        if (noteManager != null)
            noteManager.OnNoteFinishedEvent += HandleNoteFinished;
    }

    private void OnDisable()
    {
        if (noteManager != null)
            noteManager.OnNoteFinishedEvent -= HandleNoteFinished;
    }

    private void Update()
    {
    }

    private void ResolveReferences()
    {
        if (targetCanvas == null)
            targetCanvas = FindFirstObjectByType<Canvas>();
        if (noteManager == null)
            noteManager = FindFirstObjectByType<NoteManager>();
        if (chartSpawner == null)
            chartSpawner = FindFirstObjectByType<ChartNoteSpawner>();
        if (pauseController == null)
            pauseController = FindFirstObjectByType<GameplayPauseController>();
    }

    private void ResetRunState()
    {
        _perfect = 0;
        _great = 0;
        _good = 0;
        _miss = 0;
        _liveScore = 0;
        _health = Mathf.Clamp(startHealth, 0f, 100f);
        RefreshHudValues();
    }

    private void HandleNoteFinished(NoteBase note, NoteResult result)
    {
        HitJudgment judgment = result == NoteResult.Completed && note != null
            ? note.LastJudgment
            : HitJudgment.Miss;

        switch (judgment)
        {
            case HitJudgment.Perfect:
                _perfect++;
                _health += perfectHealthGain;
                break;
            case HitJudgment.Great:
                _great++;
                _health += greatHealthGain;
                break;
            case HitJudgment.Good:
                _good++;
                _health += goodHealthGain;
                break;
            default:
                _miss++;
                _health -= missHealthLoss;
                break;
        }

        _health = Mathf.Clamp(_health, 0f, 100f);
        _liveScore = CalculateLiveScore();
        RefreshHudValues();
    }

    private int CalculateLiveScore()
    {
        int totalNotes = Mathf.Max(1, chartSpawner != null ? chartSpawner.TotalNoteCount : _perfect + _great + _good + _miss);
        float scoreUnits = 1000000f / totalNotes;
        float weightedHits = _perfect + _great * 0.75f + _good * 0.5f;
        return Mathf.Clamp(Mathf.RoundToInt(weightedHits * scoreUnits), 0, 1000000);
    }

    private void RefreshHudValues()
    {
        if (_scoreText != null)
            _scoreText.text = _liveScore.ToString("D7");

        if (_healthText != null)
            _healthText.text = Mathf.RoundToInt(_health).ToString();

        if (_healthFill != null)
        {
            Vector2 size = _healthFill.sizeDelta;
            size.y = Mathf.Lerp(0f, 338f, _health / 100f);
            _healthFill.sizeDelta = size;
        }
    }

    private void RefreshSongInfo()
    {
        SongData song = SelectedSongManager.Instance != null ? SelectedSongManager.Instance.SelectedSong : null;
        Difficulty difficulty = SelectedSongManager.Instance != null ? SelectedSongManager.Instance.SelectedDifficulty : Difficulty.Medium;

        if (_songTitleText != null)
            _songTitleText.text = song != null && !string.IsNullOrWhiteSpace(song.SongTitle) ? song.SongTitle : "Unknown Song";
        if (_artistText != null)
            _artistText.text = song != null && !string.IsNullOrWhiteSpace(song.songGroupId) ? song.songGroupId : GetDifficultyLabel(difficulty);
        if (_difficultyText != null)
            _difficultyText.text = GetDifficultyLabel(difficulty).ToUpperInvariant();
        if (_coverImage != null)
        {
            _coverImage.sprite = song != null ? song.PreviewImage : null;
            _coverImage.color = _coverImage.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.18f);
        }
    }

    private void BuildHud()
    {
        if (targetCanvas == null)
            return;

        Transform old = targetCanvas.transform.Find(RootName);
        if (old != null)
            Destroy(old.gameObject);

        _root = CreateRect(RootName, targetCanvas.transform);
        Stretch(_root);
        CanvasGroup group = _root.gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        _root.SetAsLastSibling();

        BuildHealthBar(_root);
        BuildScorePanel(_root);
    }

    private void BuildHealthBar(RectTransform root)
    {
        RectTransform holder = CreatePanel("Health Bar", root, new Color(0.06f, 0.04f, 0.10f, 0.72f));
        Anchor(holder, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), healthBarPosition, new Vector2(52f, 382f));

        Sprite healthSprite = LoadEditorSprite("Healthbar-colour-0.png");
        if (healthSprite != null)
        {
            RectTransform frameRect = CreatePanel("Health Frame Sprite", holder, Color.white);
            Anchor(frameRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(382f, 52f));
            frameRect.localEulerAngles = new Vector3(0f, 0f, 90f);
            Image frame = frameRect.GetComponent<Image>();
            frame.sprite = healthSprite;
            frame.preserveAspect = false;
            frame.raycastTarget = false;
        }

        RectTransform fillMask = CreatePanel("Health Fill Mask", holder, new Color(1f, 1f, 1f, 0.08f));
        Anchor(fillMask, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(26f, 338f));
        Mask mask = fillMask.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        _healthFill = CreatePanel("Health Fill", fillMask, new Color(0.86f, 0.25f, 0.92f, 0.95f));
        Anchor(_healthFill, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(26f, 338f));

        _healthText = CreateText("50", holder, 18f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, new Vector2(0f, -166f), new Vector2(44f, 26f));
    }

    private void BuildScorePanel(RectTransform root)
    {
        RectTransform scorePanel = CreatePanel("Score Panel", root, new Color(0.18f, 0.12f, 0.25f, 0.76f));
        Anchor(scorePanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), scorePanelPosition, new Vector2(310f, 82f));
        Image scoreBg = scorePanel.GetComponent<Image>();
        Sprite scoreSprite = LoadEditorSprite("scorebar-bg@2x.png");
        if (scoreSprite != null)
        {
            scoreBg.sprite = scoreSprite;
            scoreBg.preserveAspect = false;
        }

        CreateText("SCORE", scorePanel, 13f, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 1f, 1f, 0.84f), new Vector2(-116f, 22f), new Vector2(88f, 22f));
        _scoreText = CreateText("0000000", scorePanel, 38f, FontStyles.Normal, TextAlignmentOptions.Right, Color.white, new Vector2(45f, -4f), new Vector2(232f, 48f));

        RectTransform songPanel = CreatePanel("Song Info Panel", root, new Color(0.18f, 0.10f, 0.20f, 0.82f));
        Anchor(songPanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), songInfoPosition, new Vector2(346f, 78f));
        Sprite rankingSprite = LoadEditorSprite("ranking-panel.png");
        if (rankingSprite != null)
        {
            Image image = songPanel.GetComponent<Image>();
            image.sprite = rankingSprite;
            image.preserveAspect = false;
        }

        RectTransform coverFrame = CreatePanel("Song Cover", songPanel, new Color(1f, 1f, 1f, 0.15f));
        Anchor(coverFrame, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(44f, 0f), new Vector2(64f, 64f));
        _coverImage = coverFrame.GetComponent<Image>();
        _coverImage.preserveAspect = true;

        _difficultyText = CreateText("NORMAL", songPanel, 13f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, new Vector2(103f, 22f), new Vector2(88f, 20f));
        _songTitleText = CreateText("Unknown Song", songPanel, 18f, FontStyles.Bold, TextAlignmentOptions.Left, Color.white, new Vector2(202f, 6f), new Vector2(194f, 28f));
        _artistText = CreateText("", songPanel, 12f, FontStyles.Normal, TextAlignmentOptions.Left, new Color(1f, 1f, 1f, 0.78f), new Vector2(202f, -19f), new Vector2(194f, 22f));
    }

    private static string GetDifficultyLabel(Difficulty difficulty)
    {
        return difficulty == Difficulty.Medium ? "Normal" : difficulty.ToString();
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject obj = new(objectName, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static RectTransform CreatePanel(string objectName, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(objectName, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private static TextMeshProUGUI CreateText(string text, Transform parent, float size, FontStyles style, TextAlignmentOptions alignment, Color color, Vector2 position, Vector2 dimensions)
    {
        RectTransform rect = CreateRect("Text - " + text, parent);
        Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, dimensions);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = alignment;
        label.color = color;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        TmpRuntimeFontFallback.Apply(label);
        return label;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static Sprite LoadEditorSprite(string fileName)
    {
#if UNITY_EDITOR
        string path = UiAssetFolder + fileName;
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (Object asset in assets)
        {
            if (asset is Sprite nestedSprite)
                return nestedSprite;
        }
#else
        return null;
#endif
        return null;
    }
}
