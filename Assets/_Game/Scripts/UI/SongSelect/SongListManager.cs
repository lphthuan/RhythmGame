using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using Keyboard = UnityEngine.InputSystem.Keyboard;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Builds the landscape song-selection screen from the SongData assets in the project.</summary>
public class SongListManager : MonoBehaviour
{
    public static SongListManager Instance;

    public List<SongData> _songList = new();
    public SongItemUI _itemPrefab;
    public Transform _contentArea;
    public Image _centerPreviewImage;
    public TextMeshProUGUI _rightBpmText;

    [Header("Navigation")]
    [SerializeField] private string gameplaySceneName = "KhoaCuBu";
    [SerializeField] private string mainMenuSceneName = "StartMenu";

    [Header("Generated Layout")]
    [SerializeField] private bool buildGeneratedLayout = true;
    [SerializeField] private bool hideLegacyLayout = true;
    [SerializeField] private bool includeProjectSongData = true;
    [SerializeField] private string projectSongDataFolder = "Assets/_Game/Data/Songs";
    [Tooltip("When an RG Generated Song Select object exists in the scene, Play Mode reuses it as an editable template instead of rebuilding every visual object.")]
    [SerializeField] private bool reuseEditableScenePreviewInPlay = true;
    [SerializeField] private RectTransform songCardTemplate;
    [SerializeField] private bool playPreviewOnSelect = true;
    [SerializeField] private float previewStartSeconds = 0f;
    [SerializeField] private Sprite selectScreenBackground;

    private const string GeneratedRootName = "RG Generated Song Select";
    private readonly Dictionary<SongData, CarouselCard> _cards = new();
    private SongData _selectedSong;
    private AudioSource _previewAudioSource;
    private Image _previewArt;
    private AspectRatioFitter _previewArtFitter;
    private TextMeshProUGUI _noArtText;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _artistText;
    private TextMeshProUGUI _bpmText;
    private TextMeshProUGUI _difficultyText;
    private TextMeshProUGUI _lastScoreText;
    private TextMeshProUGUI _bestScoreText;
    private TextMeshProUGUI _rankText;
    private RectTransform _leaderboardContent;
    private TextMeshProUGUI _leaderboardSubtitle;
    private TextMeshProUGUI _leaderboardEmptyText;
    private TextMeshProUGUI _moneyText;
    private TextMeshProUGUI _diamondText;
    private TextMeshProUGUI _songActionText;
    private Button _songActionButton;
    private ScrollRect _carouselScrollRect;
    private RectTransform _carouselViewport;
    private RectTransform _carouselContent;
    private RectTransform _runtimeCardTemplate;
    private Difficulty _selectedDifficulty = Difficulty.Medium;
    private readonly Dictionary<Difficulty, Image> _difficultyButtonImages = new();
    private readonly Dictionary<Difficulty, TextMeshProUGUI> _difficultyButtonLabels = new();
    private readonly Dictionary<Difficulty, TextMeshProUGUI> _difficultyButtonScores = new();

    private sealed class CarouselCard
    {
        public RectTransform Rect;
        public Image Background;
        public Image Art;
        public TextMeshProUGUI Difficulty;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Bpm;
        public TextMeshProUGUI Rank;
        public TextMeshProUGUI BestScore;
        public SongTitleMarquee Marquee;
        public GameObject LockOverlay;
    }

    private void Awake() => Instance = this;

    private void OnEnable()
    {
        PlayerWallet.Changed += RefreshStoreUi;
        PlayerInventory.Changed += RefreshStoreUi;
    }

    private void OnDisable()
    {
        PlayerWallet.Changed -= RefreshStoreUi;
        PlayerInventory.Changed -= RefreshStoreUi;
    }

    private void Start()
    {
        EnsureLocalLeaderboard();
        OnlineLeaderboardService.EnsureInstance();
        if (buildGeneratedLayout)
        {
            BuildGeneratedLayout();
            _selectedDifficulty = LoadLastSelectedDifficulty();
            SelectSong(GetInitialSong(), true);
        }
        else
        {
            PopulateList();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Song Select/Build Edit Mode Preview")]
    public void BuildEditModePreview()
    {
        BuildGeneratedLayout();
        _selectedDifficulty = GetPreferredDifficulty(GetInitialSong());
        SelectSong(GetInitialSong(), false);
        UnityEditor.EditorUtility.SetDirty(gameObject);
    }

    [ContextMenu("Song Select/Clear Edit Mode Preview")]
    public void ClearEditModePreview()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = RuntimeCanvasUtility.FindSceneCanvas();
        if (canvas == null)
            return;

        RemoveOldGeneratedRoot(canvas.transform);
        RestoreLegacyLayout(canvas);
        _cards.Clear();
        UnityEditor.EditorUtility.SetDirty(gameObject);
    }
#endif

    private void Update()
    {
        if (_cards.Count > 0)
            UpdateCarousel();

        if (WasConfirmPressedThisFrame())
            PlaySelectedSong();

        if (WasPreviousPressedThisFrame())
            SelectRelativeSong(-1);
        else if (WasNextPressedThisFrame())
            SelectRelativeSong(1);

        HandleStoreDebugInput();
    }

    public void PopulateList()
    {
        MergeProjectSongData();
        ConsolidateSongListByGroupId();

        if (_itemPrefab == null || _contentArea == null)
            return;

        for (int i = _contentArea.childCount - 1; i >= 0; i--)
            Destroy(_contentArea.GetChild(i).gameObject);

        foreach (SongData song in _songList)
        {
            if (song != null)
                Instantiate(_itemPrefab, _contentArea).Setup(song);
        }
    }

    public void SelectOrPlaySong(SongData song)
    {
        if (song == null)
            return;

        if (_selectedSong == song)
            PlaySelectedSong();
        else
            SelectSong(song, true);
    }

    public void PlaySelectedSong()
    {
        SongData song = _selectedSong != null
            ? _selectedSong
            : SelectedSongManager.Instance != null
                ? SelectedSongManager.Instance.SelectedSong
                : null;
        if (song == null)
            return;

        if (!SongUnlockService.IsUnlocked(song))
        {
            LockedSongShopPrompt.Show();
            return;
        }

        // A few older SongSelect scenes do not contain this persistent manager.
        // Create it here so the gameplay scene always receives the selected song.
        SelectedSongManager.EnsureInstance().SetSelectedSong(song, _selectedDifficulty);

        if (_previewAudioSource != null)
            _previewAudioSource.Stop();

        SceneLoadUtility.LoadSceneByName(gameplaySceneName);
    }

    public void BackToMainMenu()
    {
        if (_previewAudioSource != null)
            _previewAudioSource.Stop();

        SceneLoadUtility.LoadSceneByName(mainMenuSceneName);
    }

    public void OpenSettings()
    {
        UIManager settingsManager = FindSettingsManager();
        if (settingsManager == null)
        {
            Debug.LogWarning("SongListManager: CanvasThai/UIManager is missing from SongSelect.");
            return;
        }

        settingsManager.OpenSettings();
    }

    private void BuildGeneratedLayout()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = RuntimeCanvasUtility.FindSceneCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("SongListManager: no Canvas found.");
            return;
        }

        MergeProjectSongData();
        ConsolidateSongListByGroupId();
        EnsureCanvasScaler(canvas);
        EnsureEventSystem();
        EnsurePreviewAudioSource();
        _cards.Clear();
        _difficultyButtonImages.Clear();
        _difficultyButtonLabels.Clear();
        _difficultyButtonScores.Clear();

        if (TryUseEditableScenePreview(canvas))
            return;

        RemoveOldGeneratedRoot(canvas.transform);

        RectTransform root = CreateRect(GeneratedRootName, canvas.transform);
        Stretch(root);
        root.SetAsLastSibling();
        HideLegacyLayout(canvas, root);

        Image background = root.gameObject.AddComponent<Image>();
        background.sprite = selectScreenBackground;
        background.type = Image.Type.Simple;
        background.preserveAspect = false;
        background.color = selectScreenBackground != null ? Color.white : new Color(0.10f, 0.08f, 0.16f, 1f);

        RectTransform shade = CreatePanel("Background Shade", root, new Color(0.045f, 0.035f, 0.09f, 0.58f));
        Stretch(shade);
        shade.GetComponent<Image>().raycastTarget = false;

        BuildTopBar(root);
        BuildSongDetail(root);
        BuildCarousel(root);
        CreateText("Tap a card to select. Tap the selected card again to play.", root, 14, FontStyles.Normal,
            TextAlignmentOptions.Right, new Color(1f, 1f, 1f, 0.80f), new Vector2(-28f, 18f), new Vector2(560f, 28f), Vector2.one, Vector2.one);
    }

    private bool TryUseEditableScenePreview(Canvas canvas)
    {
        if (!Application.isPlaying || !reuseEditableScenePreviewInPlay)
            return false;

        RectTransform root = canvas.transform.Find(GeneratedRootName) as RectTransform;
        if (root == null)
            return false;

        root.gameObject.SetActive(true);
        root.SetAsLastSibling();
        HideLegacyLayout(canvas, root);
        BindEditableTopBar(root);
        BindEditableSongDetail(root);
        BindEditableCarousel(root);
        return _titleText != null && _previewArt != null && _carouselContent != null;
    }

    private void BindEditableTopBar(RectTransform root)
    {
        Button back = root.Find("Top Bar/Back")?.GetComponent<Button>();
        if (back != null)
        {
            back.onClick.RemoveAllListeners();
            back.onClick.AddListener(BackToMainMenu);
        }

        Button settings = root.Find("Top Bar/Settings")?.GetComponent<Button>();
        if (settings != null)
        {
            settings.onClick.RemoveAllListeners();
            settings.onClick.AddListener(OpenSettings);
        }

        _moneyText = FirstText(root.Find("Top Bar/Money Badge"));
        _diamondText = FirstText(root.Find("Top Bar/Diamond Badge"));
        RefreshStoreUi();
    }

    private void BindEditableSongDetail(RectTransform root)
    {
        _titleText = FindText(root, "Selected Song Title Text") ?? FirstText(root.Find("Selected Song Detail/Selected Song Title Clip"));
        _artistText = FindText(root, "Selected Song Hint Text");
        _bpmText = FindText(root, "Selected Song BPM Text");
        _previewArt = (root.Find("Selected Song Detail/Song Art Frame/Song Art") ?? root.Find("Selected Song Detail/Song Art Frame/Art"))?.GetComponent<Image>();
        _previewArtFitter = _previewArt != null ? _previewArt.GetComponent<AspectRatioFitter>() : null;
        if (_previewArt != null && _previewArtFitter == null)
        {
            _previewArtFitter = _previewArt.gameObject.AddComponent<AspectRatioFitter>();
            _previewArtFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        }
        _noArtText = FindText(root, "No Art Label") ?? FirstText(root.Find("Selected Song Detail/Song Art Frame"));
        RectTransform detail = root.Find("Selected Song Detail") as RectTransform;
        RectTransform legacyScore = detail != null ? detail.Find("Score Panel") as RectTransform : null;
        if (legacyScore != null)
            legacyScore.gameObject.SetActive(false);
        RectTransform leaderboard = detail != null ? detail.Find("Leaderboard Panel") as RectTransform : null;
        if (leaderboard == null && detail != null)
        {
            leaderboard = CreatePanel("Leaderboard Panel", detail, new Color(0.075f, 0.025f, 0.13f, 0.94f));
        }
        if (leaderboard != null)
            Anchor(leaderboard, new Vector2(0.035f, 0.07f), new Vector2(0.415f, 0.57f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        BuildLeaderboardPanel(leaderboard);
        _songActionText = FindText(root, "Song Action Label");
        _songActionButton = root.Find("Selected Song Detail/Song Action")?.GetComponent<Button>();
        if (_songActionButton != null)
        {
            _songActionButton.onClick.RemoveAllListeners();
            _songActionButton.onClick.AddListener(PlaySelectedSong);
        }

        BindDifficultyButton(root, Difficulty.Easy, "EASY Difficulty");
        BindDifficultyButton(root, Difficulty.Medium, "NORMAL Difficulty");
        BindDifficultyButton(root, Difficulty.Hard, "HARD Difficulty");
    }

    private void BindDifficultyButton(RectTransform root, Difficulty difficulty, string pathName)
    {
        Transform target = root.Find("Selected Song Detail/Difficulty Buttons/" + pathName);
        if (target == null)
            return;

        Image image = target.GetComponent<Image>();
        Button button = target.GetComponent<Button>();
        if (button == null)
            button = target.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectDifficulty(difficulty));

        _difficultyButtonImages[difficulty] = image;
        TextMeshProUGUI label = FindText(target, "Difficulty Label") ?? FirstText(target);
        TextMeshProUGUI score = FindText(target, "Difficulty Best Score");
        if (label != null)
            _difficultyButtonLabels[difficulty] = label;
        if (score != null)
            _difficultyButtonScores[difficulty] = score;
        if (difficulty == Difficulty.Medium)
            _difficultyText = label;
    }

    private void BindEditableCarousel(RectTransform root)
    {
        _carouselScrollRect = root.Find("Song Carousel")?.GetComponent<ScrollRect>();
        _carouselViewport = root.Find("Song Carousel/Viewport") as RectTransform;
        _carouselContent = root.Find("Song Carousel/Viewport/Content") as RectTransform;
        if (_carouselScrollRect == null || _carouselViewport == null || _carouselContent == null)
            return;

        _carouselScrollRect.viewport = _carouselViewport;
        _carouselScrollRect.content = _carouselContent;
        _carouselScrollRect.onValueChanged.RemoveAllListeners();
        _carouselScrollRect.onValueChanged.AddListener(_ => UpdateCarousel());

        SongSwipeSelector swipeSelector = _carouselViewport.GetComponent<SongSwipeSelector>();
        if (swipeSelector == null)
            swipeSelector = _carouselViewport.gameObject.AddComponent<SongSwipeSelector>();
        swipeSelector.Configure(this);

        _runtimeCardTemplate = ResolveRuntimeCardTemplate();
        ClearCarouselContentButTemplate();
        _carouselContent.sizeDelta = new Vector2(_carouselContent.sizeDelta.x, Mathf.Max(560f, _songList.Count * 86f + 60f));
        CreateCarouselCards(_carouselContent);
    }

    private void BuildTopBar(RectTransform root)
    {
        RectTransform bar = CreatePanel("Top Bar", root, new Color(0.96f, 0.94f, 0.99f, 0.93f));
        Anchor(bar, Vector2.up, Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -31f), new Vector2(0f, 62f));

        RectTransform back = CreateButton("Back", bar, "Back", new Color(0.31f, 0.16f, 0.38f, 0.96f));
        Anchor(back, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(100f, 36f));
        back.GetComponent<Button>().onClick.AddListener(BackToMainMenu);

        CreateText("Select a Song", bar, 25, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.20f, 0.10f, 0.27f, 1f),
            new Vector2(144f, 0f), new Vector2(260f, 44f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        CreateText("RHYTHM GAME", bar, 20, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.22f, 0.16f, 0.31f, 1f),
            Vector2.zero, new Vector2(280f, 44f));

        RectTransform settings = CreateButton("Settings", bar, "\u2699", new Color(0.34f, 0.17f, 0.48f, 0.96f), 27);
        Anchor(settings, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-290f, 0f), new Vector2(48f, 42f));
        settings.GetComponent<Button>().onClick.AddListener(OpenSettings);

        _moneyText = BuildCurrencyBadge(bar, "Money", PlayerWallet.Money.ToString(), new Vector2(-183f, 0f), new Color(0.08f, 0.38f, 0.49f, 0.96f));
        _diamondText = BuildCurrencyBadge(bar, "Diamond", PlayerWallet.Diamond.ToString(), new Vector2(-72f, 0f), new Color(0.43f, 0.21f, 0.66f, 0.96f));
    }

    private static TextMeshProUGUI BuildCurrencyBadge(RectTransform parent, string title, string value, Vector2 position, Color color)
    {
        RectTransform badge = CreatePanel(title + " Badge", parent, color);
        Anchor(badge, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), position, new Vector2(96f, 42f));
        TextMeshProUGUI valueText = CreateText(value, badge, 20, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, new Vector2(0f, 7f), new Vector2(88f, 24f));
        CreateText(title, badge, 10, FontStyles.Normal, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.82f), new Vector2(0f, -11f), new Vector2(88f, 18f));
        return valueText;
    }

    private void BuildSongDetail(RectTransform root)
    {
        RectTransform detail = CreatePanel("Selected Song Detail", root, new Color(0.04f, 0.035f, 0.10f, 0.80f));
        Anchor(detail, new Vector2(0.03f, 0.10f), new Vector2(0.57f, 0.87f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        RectTransform difficultyGroup = CreateRect("Difficulty Buttons", detail);
        Anchor(difficultyGroup, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -29f), new Vector2(348f, 48f));
        CreateDifficultyButton(difficultyGroup, Difficulty.Easy, "EASY", 0f);
        CreateDifficultyButton(difficultyGroup, Difficulty.Medium, "NORMAL", 118f);
        CreateDifficultyButton(difficultyGroup, Difficulty.Hard, "HARD", 236f);

        RectTransform titleClip = CreateRect("Selected Song Title Clip", detail);
        Anchor(titleClip, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -74f), new Vector2(355f, 50f));
        titleClip.gameObject.AddComponent<RectMask2D>();
        _titleText = CreateText("", titleClip, 36, FontStyles.Bold, TextAlignmentOptions.Left, Color.white, Vector2.zero, new Vector2(760f, 50f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        _titleText.gameObject.name = "Selected Song Title Text";
        _titleText.gameObject.AddComponent<SongTitleMarquee>();
        _artistText = CreateText("", detail, 17, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.88f, 0.80f, 0.96f, 1f), new Vector2(24f, -120f), new Vector2(340f, 28f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        _artistText.gameObject.name = "Selected Song Hint Text";
        _bpmText = CreateText("BPM: --", detail, 19, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.81f, 0.32f, 1f), new Vector2(24f, -155f), new Vector2(260f, 30f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        _bpmText.gameObject.name = "Selected Song BPM Text";

        RectTransform artFrame = CreatePanel("Song Art Frame", detail, new Color(0.48f, 0.25f, 0.62f, 0.94f));
        Anchor(artFrame, new Vector2(0.43f, 0.09f), new Vector2(0.96f, 0.76f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        artFrame.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        _previewArt = CreatePanel("Song Art", artFrame, new Color(0.38f, 0.19f, 0.56f, 1f)).GetComponent<Image>();
        Stretch(_previewArt.rectTransform);
        _previewArt.rectTransform.localEulerAngles = new Vector3(0f, 0f, -2f);
        _previewArtFitter = _previewArt.gameObject.AddComponent<AspectRatioFitter>();
        _previewArtFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        _noArtText = CreateText("NO ART", artFrame, 31, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.48f), Vector2.zero, new Vector2(260f, 55f));
        _noArtText.gameObject.name = "No Art Label";

        RectTransform leaderboard = CreatePanel("Leaderboard Panel", detail, new Color(0.075f, 0.025f, 0.13f, 0.94f));
        Anchor(leaderboard, new Vector2(0.035f, 0.07f), new Vector2(0.415f, 0.57f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        BuildLeaderboardPanel(leaderboard);

        RectTransform action = CreateButton("Song Action", detail, "PLAY", new Color(0.40f, 0.17f, 0.53f, 1f), 18f);
        Anchor(action, new Vector2(0.43f, 0.02f), new Vector2(0.96f, 0.08f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        _songActionButton = action.GetComponent<Button>();
        _songActionButton.onClick.AddListener(PlaySelectedSong);
        _songActionText = FirstText(action);
        if (_songActionText != null)
            _songActionText.gameObject.name = "Song Action Label";
    }

    private void CreateDifficultyButton(RectTransform parent, Difficulty difficulty, string label, float x)
    {
        RectTransform rect = CreatePanel(label + " Difficulty", parent, DifficultyColor(difficulty));
        Anchor(rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(112f, 44f));

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.onClick.AddListener(() => SelectDifficulty(difficulty));

        TextMeshProUGUI nameText = CreateText(label, rect, 14, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, new Vector2(0f, 8f), new Vector2(104f, 20f));
        TextMeshProUGUI scoreText = CreateText("0000000", rect, 10, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.80f), new Vector2(0f, -11f), new Vector2(104f, 18f));
        nameText.gameObject.name = "Difficulty Label";
        scoreText.gameObject.name = "Difficulty Best Score";

        _difficultyButtonImages[difficulty] = rect.GetComponent<Image>();
        _difficultyButtonLabels[difficulty] = nameText;
        _difficultyButtonScores[difficulty] = scoreText;
        if (difficulty == Difficulty.Medium)
            _difficultyText = nameText;
    }

    private void BuildCarousel(RectTransform root)
    {
        RectTransform carousel = CreateRect("Song Carousel", root);
        Anchor(carousel, new Vector2(0.60f, 0.12f), new Vector2(0.985f, 0.84f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        _carouselScrollRect = carousel.gameObject.AddComponent<ScrollRect>();
        _carouselScrollRect.horizontal = false;
        _carouselScrollRect.vertical = true;
        _carouselScrollRect.movementType = ScrollRect.MovementType.Elastic;
        _carouselScrollRect.scrollSensitivity = 28f;
        _carouselScrollRect.decelerationRate = 0.08f;

        _carouselViewport = CreatePanel("Viewport", carousel, new Color(1f, 1f, 1f, 0.01f));
        Stretch(_carouselViewport);
        Mask mask = _carouselViewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        SongSwipeSelector swipeSelector = _carouselViewport.gameObject.AddComponent<SongSwipeSelector>();
        swipeSelector.Configure(this);

        _carouselContent = CreateRect("Content", _carouselViewport);
        _carouselContent.anchorMin = new Vector2(0f, 1f);
        _carouselContent.anchorMax = new Vector2(1f, 1f);
        _carouselContent.pivot = new Vector2(0.5f, 1f);
        _carouselContent.anchoredPosition = Vector2.zero;
        _carouselContent.sizeDelta = new Vector2(0f, Mathf.Max(560f, _songList.Count * 68f + 60f));
        _carouselScrollRect.viewport = _carouselViewport;
        _carouselScrollRect.content = _carouselContent;
        _carouselScrollRect.onValueChanged.AddListener(_ => UpdateCarousel());

        CreateCarouselCards(_carouselContent);
    }

    private void CreateCarouselCards(RectTransform parent)
    {
        int songIndex = 0;
        foreach (SongData song in _songList)
        {
            if (song != null)
            {
                CreateSongCard(song, _carouselContent);
                CarouselCard card = _cards[song];
                card.Rect.anchorMin = card.Rect.anchorMax = new Vector2(0.5f, 1f);
                card.Rect.pivot = new Vector2(0.5f, 0.5f);
                card.Rect.anchoredPosition = new Vector2(8f, -38f - songIndex * 68f);
                songIndex++;
            }
        }
    }

    private static GameObject CreateStatusOverlay(Transform artHolder, string overlayName, string labelText, Color labelColor, float tiltDegrees)
    {
        Transform existing = artHolder.Find(overlayName);
        if (existing != null)
            return existing.gameObject;

        RectTransform overlay = CreatePanel(overlayName, artHolder, new Color(0f, 0f, 0f, 0.58f));
        Stretch(overlay);
        overlay.GetComponent<Image>().raycastTarget = false;

        TextMeshProUGUI label = CreateText(
            labelText, overlay, 9f, FontStyles.Bold, TextAlignmentOptions.Center,
            labelColor, Vector2.zero, Vector2.zero);
        Stretch(label.rectTransform);
        label.rectTransform.localEulerAngles = new Vector3(0f, 0f, tiltDegrees);
        label.raycastTarget = false;
        return overlay.gameObject;
    }

    private static GameObject CreateLockOverlay(Transform artHolder)
    {
        return CreateStatusOverlay(artHolder, "Lock Overlay", "LOCKED", new Color(1f, 0.25f, 0.30f, 1f), -12f);
    }

    private void CreateSongCard(SongData song, RectTransform parent)
    {
        if (_runtimeCardTemplate != null && Application.isPlaying)
        {
            CreateSongCardFromTemplate(song, parent);
            return;
        }

        Sprite roundedSprite    = RoundedRectSprite.GetCached(16f);
        Sprite roundedSpriteSm  = RoundedRectSprite.GetCached(10f);

        // ── Card (height 76px, width fills carousel) ──────────────────────────
        const float CardH   = 60f;
        const float ThumbW  = 58f;
        const float ScoreW  = 72f;
        const float CardW   = 360f;

        RectTransform card = CreateRect("Song Card - " + song.SongTitle, parent);
        card.sizeDelta = new Vector2(CardW, CardH);

        // Card background — dark rounded rect
        Image background = card.gameObject.AddComponent<Image>();
        background.sprite        = roundedSprite;
        background.type          = Image.Type.Sliced;
        background.color         = new Color(0.08f, 0.07f, 0.13f, 0.94f);
        background.raycastTarget = true;

        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() => SelectOrPlaySong(song));

        // ── Thumbnail — left square, clipped with rounded sprite ──────────────
        RectTransform artHolder = CreateRect("Card Art", card);
        Anchor(artHolder, Vector2.zero, new Vector2(0f, 1f),
               new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(ThumbW - 8f, -8f));
        Image artHolderImg       = artHolder.gameObject.AddComponent<Image>();
        artHolderImg.sprite      = roundedSprite;
        artHolderImg.type        = Image.Type.Sliced;
        artHolderImg.color       = new Color(0.20f, 0.12f, 0.32f, 1f);
        artHolderImg.raycastTarget = false;
        artHolder.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        Image art          = CreatePanel("Art", artHolder, new Color(0.20f, 0.12f, 0.32f, 1f)).GetComponent<Image>();
        Stretch(art.rectTransform);
        art.sprite         = song.PreviewImage;
        art.type           = Image.Type.Simple;
        art.preserveAspect = false;
        art.raycastTarget  = false;
        GameObject lockOverlay = CreateLockOverlay(artHolder);

        // ── Difficulty badge — sits on the CARD (not inside mask) ─────────────
        // Positioned over the top-left of the thumbnail area
        RectTransform diffBadge = CreateRect("Difficulty", card);
        Anchor(diffBadge, new Vector2(0f, 1f), new Vector2(0f, 1f),
               new Vector2(0f, 1f), new Vector2(ThumbW + 10f, -6f), new Vector2(58f, 15f));
        Image diffBadgeImg       = diffBadge.gameObject.AddComponent<Image>();
        diffBadgeImg.sprite      = roundedSpriteSm;
        diffBadgeImg.type        = Image.Type.Sliced;
        diffBadgeImg.color       = DifficultyColor(song);
        diffBadgeImg.raycastTarget = false;

        TextMeshProUGUI difficultyText = CreateText(
            GetDifficultyLabel(song).ToUpperInvariant(), diffBadge,
            7f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white,
            Vector2.zero, new Vector2(56f, 13f));
        difficultyText.gameObject.name = "Difficulty Label";

        // ── Centre: title + BPM ───────────────────────────────────────────────
        // Text area: from right edge of thumbnail to left edge of score panel
        RectTransform textArea = CreateRect("Text", card);
        Anchor(textArea,
               new Vector2(0f, 0f), new Vector2(1f, 1f),
               new Vector2(0f, 0.5f),
               new Vector2(ThumbW + 10f, 0f),
               new Vector2(-(ScoreW + 8f), 0f));

        // Title — vertically centred, slightly above mid
        RectTransform titleClip = CreateRect("Title Clip", textArea);
        Anchor(titleClip, new Vector2(0f, 1f), new Vector2(1f, 1f),
               new Vector2(0f, 1f), new Vector2(0f, -23f), new Vector2(0f, 21f));
        titleClip.gameObject.AddComponent<RectMask2D>();

        TextMeshProUGUI title = CreateText(
            song.SongTitle, titleClip,
            14f, FontStyles.Bold, TextAlignmentOptions.Left, Color.white,
            Vector2.zero, new Vector2(215f, 21f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        title.gameObject.name = "Card Title";
        SongTitleMarquee marquee = title.gameObject.AddComponent<SongTitleMarquee>();

        // BPM — below title
        TextMeshProUGUI bpm = CreateText(
            GetBpmLabel(song), textArea,
            9f, FontStyles.Normal, TextAlignmentOptions.Left,
            new Color(0.78f, 0.72f, 0.88f, 0.86f),
            new Vector2(0f, -38f), new Vector2(150f, 14f),
            new Vector2(0f, 1f), new Vector2(0f, 1f));
        bpm.gameObject.name = "Card BPM";

        // ── Right score column ────────────────────────────────────────────────
        RectTransform scorePanel = CreateRect("Card Score", card);
        Anchor(scorePanel, new Vector2(1f, 0f), new Vector2(1f, 1f),
               new Vector2(1f, 0.5f), new Vector2(-4f, 0f), new Vector2(ScoreW, 0f));

        // Thin separator on left edge
        RectTransform sep = CreatePanel("Separator", scorePanel, new Color(1f, 1f, 1f, 0.10f));
        Anchor(sep, new Vector2(0f, 0.12f), new Vector2(0f, 0.88f),
               new Vector2(0f, 0.5f), Vector2.zero, new Vector2(1f, 0f));

        // "Score" caption
        CreateText("Score", scorePanel,
            9f, FontStyles.Normal, TextAlignmentOptions.Right,
            new Color(0.78f, 0.72f, 1f, 0.70f),
            new Vector2(-6f, 12f), new Vector2(ScoreW - 10f, 14f),
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));

        // Score digits
        TextMeshProUGUI bestScore = CreateText(
            "0000000", scorePanel,
            9f, FontStyles.Bold, TextAlignmentOptions.Right,
            new Color(1f, 1f, 1f, 0.96f),
            new Vector2(-6f, -4f), new Vector2(ScoreW - 10f, 16f),
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
        bestScore.gameObject.name = "Card Best Score";

        // Rank letter
        TextMeshProUGUI rank = CreateText(
            "-", scorePanel,
            12f, FontStyles.Bold, TextAlignmentOptions.Right,
            new Color(1f, 0.58f, 0.88f, 0.95f),
            new Vector2(-6f, -21f), new Vector2(ScoreW - 10f, 16f),
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
        rank.gameObject.name = "Card Rank";

        _cards[song] = new CarouselCard
        {
            Rect       = card,
            Background = background,
            Art        = art,
            Difficulty = difficultyText,
            Title      = title,
            Bpm        = bpm,
            Rank       = rank,
            BestScore  = bestScore,
            Marquee    = marquee,
            LockOverlay = lockOverlay
        };
    }

    private void CreateSongCardFromTemplate(SongData song, RectTransform parent)
    {
        RectTransform card = Instantiate(_runtimeCardTemplate, parent, false);
        card.name = "Song Card - " + song.SongTitle;
        card.gameObject.SetActive(true);

        Image background = card.GetComponent<Image>();
        Button button = card.GetComponent<Button>();
        if (button == null)
            button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectOrPlaySong(song));

        Image art = (card.Find("Card Art/Art") ?? card.Find("Art"))?.GetComponent<Image>();
        TextMeshProUGUI difficultyText = FindText(card, "Difficulty Label");
        TextMeshProUGUI title = FindText(card, "Card Title");
        TextMeshProUGUI bpm = FindText(card, "Card BPM");
        TextMeshProUGUI rank = FindText(card, "Card Rank");
        TextMeshProUGUI bestScore = FindText(card, "Card Best Score");
        if (background == null || title == null)
        {
            Destroy(card.gameObject);
            CreateDefaultRuntimeSongCard(song, parent);
            return;
        }

        SongTitleMarquee marquee = title.GetComponent<SongTitleMarquee>();
        if (marquee == null)
            marquee = title.gameObject.AddComponent<SongTitleMarquee>();

        if (art != null)
        {
            art.sprite = song.PreviewImage;
            art.preserveAspect = false;
            art.raycastTarget = false;
        }
        title.text = song.SongTitle;
        if (bpm != null)
            bpm.text = GetBpmLabel(song);
        Transform artHolder = art != null ? art.transform.parent : null;
        GameObject lockOverlay = artHolder != null ? CreateLockOverlay(artHolder) : null;
        _cards[song] = new CarouselCard { Rect = card, Background = background, Art = art, Difficulty = difficultyText, Title = title, Bpm = bpm, Rank = rank, BestScore = bestScore, Marquee = marquee, LockOverlay = lockOverlay };
    }

    private void CreateDefaultRuntimeSongCard(SongData song, RectTransform parent)
    {
        RectTransform template = _runtimeCardTemplate;
        _runtimeCardTemplate = null;
        CreateSongCard(song, parent);
        _runtimeCardTemplate = template;
    }

    private void SelectSong(SongData song, bool playPreview)
    {
        if (song == null)
            return;

        _selectedSong = song;
        if (!song.HasPlayableChart(_selectedDifficulty))
            _selectedDifficulty = GetPreferredDifficulty(song);

        if (SelectedSongManager.Instance != null)
            SelectedSongManager.Instance.SetSelectedSong(song, _selectedDifficulty);

        ShowSongDetails(song);
        CenterSelectedCard();
        UpdateCarousel();
        if (playPreview && playPreviewOnSelect)
            PlayPreview(song);
    }

    private void SelectDifficulty(Difficulty difficulty)
    {
        if (_selectedSong != null && !_selectedSong.HasPlayableChart(difficulty))
            difficulty = GetPreferredDifficulty(_selectedSong);

        _selectedDifficulty = difficulty;
        if (_selectedSong != null && SelectedSongManager.Instance != null)
            SelectedSongManager.Instance.SetSelectedSong(_selectedSong, _selectedDifficulty);

        if (_selectedSong != null)
            ShowSongDetails(_selectedSong);

        UpdateDifficultyButtons();
    }

    private void SelectRelativeSong(int direction)
    {
        if (_songList.Count == 0)
            return;
        int index = _songList.IndexOf(_selectedSong);
        if (index < 0) index = 0;
        index = (index + direction + _songList.Count) % _songList.Count;
        SelectSong(_songList[index], true);
    }

    private void ShowSongDetails(SongData song)
    {
        if (_titleText != null)
        {
            _titleText.text = song.SongTitle;
            SongTitleMarquee detailMarquee = _titleText.GetComponent<SongTitleMarquee>();
            if (detailMarquee != null)
                detailMarquee.ResetScroll();
        }
        bool unlocked = SongUnlockService.IsUnlocked(song);
        if (_artistText != null)
            _artistText.text = unlocked
                ? "Tap selected song again to start."
                : $"Locked. Buy for {SongUnlockService.GetPrice(song, CurrencyType.Money)} Money.";
        if (_bpmText != null)
            _bpmText.text = GetBpmLabel(song);
        if (_previewArt != null)
        {
            _previewArt.sprite = song.PreviewImage;
            _previewArt.color = song.PreviewImage != null
                ? (unlocked ? Color.white : new Color(0.42f, 0.42f, 0.42f, 1f))
                : new Color(0.38f, 0.19f, 0.56f, 1f);
        }
        if (_noArtText != null)
            _noArtText.gameObject.SetActive(song.PreviewImage == null);
        if (song.PreviewImage != null && _previewArtFitter != null)
        {
            Rect rect = song.PreviewImage.rect;
            _previewArtFitter.aspectRatio = rect.height > 0f ? rect.width / rect.height : 1f;
        }

        SongPlayStats stats = SongPlayStats.Load(song, _selectedDifficulty);
        if (_lastScoreText != null)
            _lastScoreText.text = stats.LastScore.ToString("D7");
        if (_bestScoreText != null)
            _bestScoreText.text = stats.BestScore.ToString("D7");
        if (_rankText != null)
            _rankText.text = string.IsNullOrEmpty(stats.BestRank) ? "-" : stats.BestRank;
        RefreshLeaderboard(song, stats);
        UpdateSongActionButton(song, unlocked);
        UpdateDifficultyButtons();
    }

    private static void EnsureLocalLeaderboard()
    {
        if (LocalLeaderboardManager.Instance != null)
            return;

        new GameObject("Local Leaderboard Manager").AddComponent<LocalLeaderboardManager>();
    }

    /// <summary>
    /// Replaces the former Last/Best Score block with a compact, scrollable ranking panel.
    /// It is rebuilt on both a generated screen and the editable scene preview so their
    /// proportions remain identical at every canvas resolution.
    /// </summary>
    private void BuildLeaderboardPanel(RectTransform panel)
    {
        if (panel == null)
            return;

        for (int i = panel.childCount - 1; i >= 0; i--)
        {
            GameObject child = panel.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }

        _lastScoreText = null;
        _bestScoreText = null;
        _rankText = null;

        Image background = panel.GetComponent<Image>();
        if (background != null)
            background.color = new Color(0.075f, 0.025f, 0.13f, 0.94f);

        CreateLeaderboardBorder(panel, true, new Color(0.73f, 0.30f, 1f, 0.88f));
        CreateLeaderboardBorder(panel, false, new Color(0.73f, 0.30f, 1f, 0.50f));

        TextMeshProUGUI heading = CreateLeaderboardText("◆  LEADERBOARD", panel, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(14f, -15f), new Vector2(230f, 28f), 18f, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.93f, 0.82f, 1f, 1f));
        heading.characterSpacing = 0.8f;
        _leaderboardSubtitle = CreateLeaderboardText("HARD  •  GLOBAL TOP 50", panel, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(15f, -42f), new Vector2(230f, 18f), 11f, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.80f, 0.55f, 1f, 0.96f));

        RectTransform viewport = CreatePanel("Leaderboard Viewport", panel, new Color(0f, 0f, 0f, 0.12f));
        Anchor(viewport, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -34f), new Vector2(-16f, -72f));
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 1f);
        _leaderboardContent = content;

        ScrollRect scroll = panel.GetComponent<ScrollRect>();
        if (scroll == null)
            scroll = panel.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 22f;
        scroll.verticalNormalizedPosition = 1f;

        _leaderboardEmptyText = CreateLeaderboardText("SYNCING GLOBAL RANKING...", content, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -36f), new Vector2(220f, 24f), 9f, FontStyles.Italic, TextAlignmentOptions.Center, new Color(0.85f, 0.76f, 0.95f, 0.72f));
    }

    private static void CreateLeaderboardBorder(RectTransform parent, bool top, Color color)
    {
        RectTransform line = CreateRect("Leaderboard Border", parent);
        float y = top ? 1f : 0f;
        line.anchorMin = new Vector2(0f, y);
        line.anchorMax = new Vector2(1f, y);
        line.pivot = new Vector2(0.5f, 0.5f);
        line.anchoredPosition = new Vector2(0f, top ? -2f : 2f);
        line.sizeDelta = new Vector2(-8f, 2f);
        Image image = line.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static TextMeshProUGUI CreateLeaderboardText(string text, Transform parent, Vector2 anchor, Vector2 pivot,
        Vector2 position, Vector2 size, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
    {
        RectTransform rect = CreateRect("Leaderboard Text - " + text, parent);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = alignment;
        label.color = color;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        TmpRuntimeFontFallback.Apply(label);
        label.text = text;
        return label;
    }

    private void RefreshLeaderboard(SongData song, SongPlayStats stats)
    {
        if (_leaderboardContent == null || song == null)
            return;

        for (int i = _leaderboardContent.childCount - 1; i >= 0; i--)
        {
            GameObject child = _leaderboardContent.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }

        if (_leaderboardSubtitle != null)
            _leaderboardSubtitle.text = "TOP 50  •  " + _selectedDifficulty.ToString().ToUpperInvariant();

        string leaderboardSongId = string.IsNullOrWhiteSpace(song.songGroupId) ? song.name : song.songGroupId;
        RenderLeaderboardLoading();

        // The panel is intentionally server-authoritative. Local rows are not
        // mixed in, otherwise two accounts on different devices see different ranks.
        Difficulty displayedDifficulty = _selectedDifficulty;
        OnlineLeaderboardService service = OnlineLeaderboardService.EnsureInstance();
        service.SubmitIfImproved(leaderboardSongId, displayedDifficulty, stats.BestScore, Mathf.Clamp01(stats.BestScore / 1000000f), 0, _ => service.FetchTop(leaderboardSongId, displayedDifficulty, remoteEntries =>
        {
            if (_selectedSong != song || _selectedDifficulty != displayedDifficulty)
                return;

            RenderLeaderboard(remoteEntries);
        }));
    }

    private void RenderLeaderboardLoading()
    {
        if (_leaderboardContent == null)
            return;

        ClearLeaderboardRows();
        _leaderboardContent.sizeDelta = new Vector2(0f, 72f);
        CreateLeaderboardText("SYNCING GLOBAL RANKING...", _leaderboardContent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -36f), new Vector2(220f, 24f), 9f, FontStyles.Italic, TextAlignmentOptions.Center, new Color(0.85f, 0.76f, 0.95f, 0.72f));
    }

    private void RenderLeaderboard(List<LeaderboardEntry> entries)
    {
        if (_leaderboardContent == null)
            return;

        ClearLeaderboardRows();

        if (entries == null || entries.Count == 0)
        {
            _leaderboardContent.sizeDelta = new Vector2(0f, 62f);
            _leaderboardEmptyText = CreateLeaderboardText("NO GLOBAL SCORES YET", _leaderboardContent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -34f), new Vector2(220f, 24f), 9f, FontStyles.Italic, TextAlignmentOptions.Center, new Color(0.85f, 0.76f, 0.95f, 0.72f));
            return;
        }

        const float rowHeight = 27f;
        const float topPadding = 2f;
        _leaderboardContent.sizeDelta = new Vector2(0f, topPadding + entries.Count * rowHeight + 3f);
        for (int i = 0; i < entries.Count; i++)
            CreateLeaderboardRow(_leaderboardContent, entries[i], i + 1, topPadding + i * rowHeight, i == 0);
    }

    private void ClearLeaderboardRows()
    {
        if (_leaderboardContent == null)
            return;

        for (int i = _leaderboardContent.childCount - 1; i >= 0; i--)
        {
            GameObject child = _leaderboardContent.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private string DisplayPlayerName => AccountSession.IsSignedIn ? AccountSession.CurrentUsername : "GUEST";

    private static void CreateLeaderboardRow(RectTransform parent, LeaderboardEntry entry, int position, float y, bool firstPlace)
    {
        Color rowColor = firstPlace
            ? new Color(0.44f, 0.24f, 0.09f, 0.78f)
            : new Color(0.18f, 0.09f, 0.27f, position % 2 == 0 ? 0.64f : 0.42f);
        RectTransform row = CreatePanel("Rank " + position, parent, rowColor);
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.anchoredPosition = new Vector2(0f, -y);
        row.sizeDelta = new Vector2(0f, 24f);

        Color numberColor = firstPlace ? new Color(1f, 0.84f, 0.26f, 1f) : new Color(0.92f, 0.80f, 1f, 0.95f);
        CreateLeaderboardText("#" + position.ToString("00"), row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(7f, 0f), new Vector2(38f, 23f), 13f, FontStyles.Bold, TextAlignmentOptions.Left, numberColor);
        RectTransform playerViewport = CreateRect("Username Viewport", row);
        playerViewport.anchorMin = playerViewport.anchorMax = new Vector2(0f, 0.5f);
        playerViewport.pivot = new Vector2(0f, 0.5f);
        playerViewport.anchoredPosition = new Vector2(46f, 0f);
        playerViewport.sizeDelta = new Vector2(118f, 23f);
        playerViewport.gameObject.AddComponent<RectMask2D>();
        TextMeshProUGUI player = CreateLeaderboardText(string.IsNullOrWhiteSpace(entry.playerName) ? "PLAYER" : entry.playerName.ToUpperInvariant(), playerViewport,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(118f, 23f), 13f, FontStyles.Bold, TextAlignmentOptions.Left, Color.white);
        player.gameObject.AddComponent<LeaderboardNameMarquee>().Configure(playerViewport, player);
        CreateLeaderboardText(entry.score.ToString("D7"), row, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-30f, 0f), new Vector2(72f, 23f), 13f, FontStyles.Bold, TextAlignmentOptions.Right, new Color(0.96f, 0.94f, 1f, 1f));
        CreateLeaderboardText(string.IsNullOrWhiteSpace(entry.rank) ? "-" : entry.rank, row, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-6f, 0f), new Vector2(23f, 23f), 13f, FontStyles.Bold, TextAlignmentOptions.Center, firstPlace ? new Color(1f, 0.82f, 0.20f, 1f) : new Color(0.90f, 0.52f, 1f, 1f));
    }

    private void UpdateDifficultyButtons()
    {
        foreach (KeyValuePair<Difficulty, Image> item in _difficultyButtonImages)
        {
            if (item.Value == null)
                continue;
            bool selected = item.Key == _selectedDifficulty;
            item.Value.color = selected ? DifficultyColor(item.Key) : new Color(0.12f, 0.08f, 0.18f, 0.86f);
            RectTransform rect = item.Value.rectTransform;
            rect.localScale = selected ? Vector3.one * 1.08f : Vector3.one;
        }

        foreach (KeyValuePair<Difficulty, TextMeshProUGUI> item in _difficultyButtonLabels)
        {
            if (item.Value == null)
                continue;
            bool selected = item.Key == _selectedDifficulty;
            item.Value.color = selected ? Color.white : new Color(1f, 1f, 1f, 0.68f);
        }

        foreach (KeyValuePair<Difficulty, TextMeshProUGUI> item in _difficultyButtonScores)
        {
            if (item.Value == null)
                continue;
            SongPlayStats stats = _selectedSong != null ? SongPlayStats.Load(_selectedSong, item.Key) : default;
            bool selected = item.Key == _selectedDifficulty;
            item.Value.text = stats.BestScore.ToString("D7");
            item.Value.color = selected ? new Color(1f, 1f, 1f, 0.92f) : new Color(1f, 1f, 1f, 0.55f);
        }
    }

    public void SelectClosestScrolledSong()
    {
        if (_carouselViewport == null || _cards.Count == 0)
            return;

        SongData closestSong = null;
        float closestDistance = float.MaxValue;
        foreach (KeyValuePair<SongData, CarouselCard> item in _cards)
        {
            float distance = Mathf.Abs(_carouselViewport.InverseTransformPoint(item.Value.Rect.position).y);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSong = item.Key;
            }
        }

        if (closestSong != null && closestSong != _selectedSong)
            SelectSong(closestSong, true);
        else
            CenterSelectedCard();
    }

    private void UpdateCarousel()
    {
        if (_carouselViewport == null)
            return;

        foreach (KeyValuePair<SongData, CarouselCard> item in _cards)
        {
            if (item.Value.Rect == null)
                continue;
            float localY = _carouselViewport.InverseTransformPoint(item.Value.Rect.position).y;
            float t = Mathf.Clamp01(1f - Mathf.Abs(localY) / 170f);
            float scale = Mathf.Lerp(0.92f, 1.02f, t);
            float lerpSpeed = Application.isPlaying ? Time.unscaledDeltaTime * 12f : 1f;
            item.Value.Rect.localScale = Vector3.Lerp(item.Value.Rect.localScale, Vector3.one * scale, lerpSpeed);
            item.Value.Rect.anchoredPosition = new Vector2(Mathf.Lerp(item.Value.Rect.anchoredPosition.x, Mathf.Lerp(14f, 4f, t), lerpSpeed), item.Value.Rect.anchoredPosition.y);
            bool unlocked = SongUnlockService.IsUnlocked(item.Key);
            Color targetColor = !unlocked
                ? new Color(0.05f, 0.05f, 0.07f, 0.94f)
                : item.Key == _selectedSong
                    ? new Color(0.18f, 0.08f, 0.18f, 0.96f)
                    : new Color(0.08f, 0.07f, 0.13f, 0.94f);
            if (item.Value.Background != null)
                item.Value.Background.color = Color.Lerp(item.Value.Background.color, targetColor, lerpSpeed);
            if (item.Value.Art != null)
                item.Value.Art.color = unlocked ? Color.white : new Color(0.42f, 0.42f, 0.42f, 1f);
            if (item.Value.LockOverlay != null)
                item.Value.LockOverlay.SetActive(!unlocked);
            Difficulty displayDifficulty = item.Key == _selectedSong ? _selectedDifficulty : GetPreferredDifficulty(item.Key);
            SongPlayStats stats = SongPlayStats.Load(item.Key, displayDifficulty);
            if (item.Value.Difficulty != null)
            {
                item.Value.Difficulty.text = unlocked ? GetDifficultyLabel(displayDifficulty).ToUpperInvariant() : "LOCKED";
                Image difficultyImage = item.Value.Difficulty.transform.parent.GetComponent<Image>();
                if (difficultyImage != null)
                    difficultyImage.color = unlocked ? DifficultyColor(displayDifficulty) : new Color(0.18f, 0.18f, 0.22f, 1f);
            }
            if (item.Value.Rank != null)
                item.Value.Rank.text = unlocked ? (string.IsNullOrEmpty(stats.BestRank) ? "-" : stats.BestRank) : "$";
            if (item.Value.BestScore != null)
                item.Value.BestScore.text = unlocked ? stats.BestScore.ToString("D7") : SongUnlockService.GetPrice(item.Key, CurrencyType.Money).ToString();
        }
    }

    private void CenterSelectedCard()
    {
        if (_selectedSong == null || _carouselContent == null || _carouselViewport == null || !_cards.TryGetValue(_selectedSong, out CarouselCard card))
            return;

        Canvas.ForceUpdateCanvases();
        float desiredY = -card.Rect.anchoredPosition.y - _carouselViewport.rect.height * 0.5f;
        float maxY = Mathf.Max(0f, _carouselContent.rect.height - _carouselViewport.rect.height);
        Vector2 position = _carouselContent.anchoredPosition;
        position.y = Mathf.Clamp(desiredY, 0f, maxY);
        _carouselContent.anchoredPosition = position;
    }

    private void PlayPreview(SongData song)
    {
        if (_previewAudioSource == null || song == null || song.audioClip == null)
            return;
        if (!Application.isPlaying)
            return;
        _previewAudioSource.Stop();
        _previewAudioSource.clip = song.audioClip;
        _previewAudioSource.loop = true;
        _previewAudioSource.volume = RuntimeGameplaySettings.MusicVolume01;
        _previewAudioSource.time = Mathf.Clamp(previewStartSeconds, 0f, Mathf.Max(0f, song.audioClip.length - 0.05f));
        _previewAudioSource.Play();
    }

    private void RefreshStoreUi()
    {
        if (_moneyText != null)
            _moneyText.text = PlayerWallet.Money.ToString();
        if (_diamondText != null)
            _diamondText.text = PlayerWallet.Diamond.ToString();

        if (_selectedSong != null)
            ShowSongDetails(_selectedSong);

        UpdateCarousel();
    }

    private void UpdateSongActionButton(SongData song, bool unlocked)
    {
        if (_songActionText == null)
            return;

        _songActionText.text = unlocked
            ? "PLAY"
            : "LOCKED - VISIT SHOP";
    }

    private void TryPurchaseSelectedSong(CurrencyType currency)
    {
        if (_selectedSong == null)
            return;

        bool success = SongUnlockService.TryPurchase(_selectedSong, currency, out string message);
        Debug.Log("[Store] " + message);

        RefreshStoreUi();

        if (success)
        {
            GameplaySfxPlayer.Play(GameplaySfxCue.Unlock);
            CenterSelectedCard();
        }
    }

    private void HandleStoreDebugInput()
    {
        if (WasAddMoneyPressedThisFrame())
        {
            PlayerWallet.Add(CurrencyType.Money, 1000);
            Debug.Log("[Store Test] Added 1000 Money. Current Money: " + PlayerWallet.Money);
        }

        if (WasAddDiamondPressedThisFrame())
        {
            PlayerWallet.Add(CurrencyType.Diamond, 50);
            Debug.Log("[Store Test] Added 50 Diamond. Current Diamond: " + PlayerWallet.Diamond);
        }

        if (WasResetWalletPressedThisFrame())
        {
            PlayerWallet.ResetForTesting();
            Debug.Log("[Store Test] Reset local wallet.");
        }
    }

    private static UIManager FindSettingsManager()
    {
        UIManager[] managers = FindObjectsByType<UIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (UIManager manager in managers)
        {
            if (manager != null && manager.gameObject.name == "CanvasThai")
                return manager;
        }

        return null;
    }

    private GameObject BuildSettingsOverlay()
    {
        RectTransform root = CreatePanel("Settings Overlay", transform.root, new Color(0f, 0f, 0f, 0.72f));
        Stretch(root);
        RectTransform panel = CreatePanel("Settings Panel", root, new Color(0.12f, 0.07f, 0.19f, 0.98f));
        Anchor(panel, new Vector2(0.25f, 0.16f), new Vector2(0.75f, 0.84f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        CreateText("SETTINGS", panel, 30, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, new Vector2(0f, -44f), new Vector2(400f, 42f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        CreateText("AUDIO", panel, 18, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.92f, 0.68f, 1f, 1f), new Vector2(44f, 56f), new Vector2(160f, 30f));
        CreateSettingRow(panel, "Music Volume", 24f, () => RuntimeGameplaySettings.MusicVolumePercent.ToString() + "%", value => RuntimeGameplaySettings.MusicVolumePercent = Mathf.Clamp(RuntimeGameplaySettings.MusicVolumePercent + value * 5, 0, 100));
        CreateSettingRow(panel, "Offset", -34f, () => RuntimeGameplaySettings.AudioOffsetMs + " ms", value => RuntimeGameplaySettings.AudioOffsetMs = Mathf.Clamp(RuntimeGameplaySettings.AudioOffsetMs + value * 5, -500, 1000));
        CreateText("GAMEPLAY", panel, 18, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.92f, 0.68f, 1f, 1f), new Vector2(44f, -108f), new Vector2(160f, 30f));
        CreateSettingRow(panel, "Note Speed", -166f, () => RuntimeGameplaySettings.NoteSpeedMultiplier.ToString("0.0"), value => RuntimeGameplaySettings.NoteSpeedMultiplier = Mathf.Clamp(RuntimeGameplaySettings.NoteSpeedMultiplier + value * 0.1f, RuntimeGameplaySettings.MinNoteSpeedSetting, RuntimeGameplaySettings.MaxNoteSpeedSetting));
        RectTransform close = CreateButton("Close", panel, "Done", new Color(0.40f, 0.17f, 0.53f, 1f));
        Anchor(close, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(130f, 40f));
        close.GetComponent<Button>().onClick.AddListener(() => { RuntimeGameplaySettings.Save(); root.gameObject.SetActive(false); });
        return root.gameObject;
    }

    private static void CreateSettingRow(RectTransform parent, string label, float y, System.Func<string> read, System.Action<int> change)
    {
        CreateText(label, parent, 20, FontStyles.Normal, TextAlignmentOptions.Left, Color.white, new Vector2(44f, y), new Vector2(260f, 34f));
        TextMeshProUGUI valueText = CreateText(read(), parent, 21, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, new Vector2(0f, y), new Vector2(150f, 34f));
        RectTransform minus = CreateButton("Minus " + label, parent, "-", new Color(0.30f, 0.14f, 0.43f, 1f));
        Anchor(minus, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-160f, y), new Vector2(38f, 34f));
        minus.GetComponent<Button>().onClick.AddListener(() => { change(-1); valueText.text = read(); });
        RectTransform plus = CreateButton("Plus " + label, parent, "+", new Color(0.30f, 0.14f, 0.43f, 1f));
        Anchor(plus, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-44f, y), new Vector2(38f, 34f));
        plus.GetComponent<Button>().onClick.AddListener(() => { change(1); valueText.text = read(); });
    }

    private void MergeProjectSongData()
    {
        if (!includeProjectSongData) return;
        HashSet<SongData> existing = new(_songList);

#if UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(projectSongDataFolder))
        {
            foreach (string guid in AssetDatabase.FindAssets("t:SongData", new[] { projectSongDataFolder }))
            {
                SongData song = AssetDatabase.LoadAssetAtPath<SongData>(AssetDatabase.GUIDToAssetPath(guid));
                if (song != null && existing.Add(song)) _songList.Add(song);
            }
        }

        foreach (SongData song in Resources.LoadAll<SongData>("Songs"))
        {
            if (song != null && existing.Add(song))
                _songList.Add(song);
        }
#else
        foreach (SongData song in Resources.LoadAll<SongData>("Songs"))
        {
            if (song != null && existing.Add(song))
                _songList.Add(song);
        }
#endif
    }

    private void ConsolidateSongListByGroupId()
    {
        Dictionary<string, SongData> hubs = new();
        List<SongData> consolidated = new();

        foreach (SongData song in _songList)
        {
            if (song == null)
                continue;

            string key = GetSongGroupKey(song);
            if (!hubs.TryGetValue(key, out SongData hub))
            {
                hubs[key] = song;
                consolidated.Add(song);
                CopyLegacyDifficultyIntoSlot(song, song);
                continue;
            }

            MergeSongIntoHub(hub, song);
        }

        _songList.Clear();
        _songList.AddRange(consolidated);
    }

    private static void MergeSongIntoHub(SongData hub, SongData source)
    {
        if (hub == null || source == null)
            return;

        if (hub.PreviewImage == null && source.PreviewImage != null)
            hub._previewImage = source.PreviewImage;
        if (hub.audioClip == null && source.audioClip != null)
            hub.audioClip = source.audioClip;
        if (hub._bpm <= 0f && source._bpm > 0f)
            hub._bpm = source._bpm;

        CopyLegacyDifficultyIntoSlot(hub, source);
        CopyDifficultySlots(hub, source);
    }

    private static void CopyLegacyDifficultyIntoSlot(SongData hub, SongData source)
    {
        string chart = source.ComputedChartFileName;
        if (string.IsNullOrWhiteSpace(chart))
            return;

        switch (source.difficultyLevel)
        {
            case Difficulty.Easy:
                if (string.IsNullOrWhiteSpace(hub.easyChartFileName)) hub.easyChartFileName = chart;
                if (hub.easyTimelineAsset == null) hub.easyTimelineAsset = source.GetTimelineAsset(Difficulty.Easy);
                break;
            case Difficulty.Hard:
                if (string.IsNullOrWhiteSpace(hub.hardChartFileName)) hub.hardChartFileName = chart;
                if (hub.hardTimelineAsset == null) hub.hardTimelineAsset = source.GetTimelineAsset(Difficulty.Hard);
                break;
            default:
                if (string.IsNullOrWhiteSpace(hub.normalChartFileName)) hub.normalChartFileName = chart;
                if (hub.normalTimelineAsset == null) hub.normalTimelineAsset = source.GetTimelineAsset(Difficulty.Medium);
                break;
        }
    }

    private static void CopyDifficultySlots(SongData hub, SongData source)
    {
        if (string.IsNullOrWhiteSpace(hub.easyChartFileName)) hub.easyChartFileName = source.easyChartFileName;
        if (string.IsNullOrWhiteSpace(hub.normalChartFileName)) hub.normalChartFileName = source.normalChartFileName;
        if (string.IsNullOrWhiteSpace(hub.hardChartFileName)) hub.hardChartFileName = source.hardChartFileName;

        if (hub.easyTimelineAsset == null) hub.easyTimelineAsset = source.easyTimelineAsset;
        if (hub.normalTimelineAsset == null) hub.normalTimelineAsset = source.normalTimelineAsset;
        if (hub.hardTimelineAsset == null) hub.hardTimelineAsset = source.hardTimelineAsset;
    }

    private static string GetSongGroupKey(SongData song)
    {
        if (song == null)
            return "null";

        if (!string.IsNullOrWhiteSpace(song.songGroupId))
            return SongData.SanitizeForFileName(song.songGroupId);

        string title = song.SongTitle;
        if (!string.IsNullOrWhiteSpace(title))
        {
            int bracket = title.LastIndexOf('[');
            if (bracket > 0)
                title = title[..bracket].Trim();
        }

        return SongData.SanitizeForFileName(string.IsNullOrWhiteSpace(title) ? song.name : title);
    }

    private void HideLegacyLayout(Canvas canvas, RectTransform root)
    {
        if (!hideLegacyLayout) return;
        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            Transform child = canvas.transform.GetChild(i);
            if (child == root)
                continue;

            if (child == transform || transform.IsChildOf(child))
            {
                CanvasGroup group = child.GetComponent<CanvasGroup>();
                if (group == null)
                    group = child.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
            else
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private static void RestoreLegacyLayout(Canvas canvas)
    {
        if (canvas == null)
            return;

        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            Transform child = canvas.transform.GetChild(i);
            if (child.name == GeneratedRootName)
                continue;

            child.gameObject.SetActive(true);
            CanvasGroup group = child.GetComponent<CanvasGroup>();
            if (group == null)
                continue;

            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }

    private RectTransform ResolveRuntimeCardTemplate()
    {
        if (songCardTemplate != null)
            return songCardTemplate;
        if (_carouselContent == null)
            return null;

        Transform named = _carouselContent.Find("Song Card Template");
        if (named is RectTransform namedRect)
            return namedRect;

        for (int i = 0; i < _carouselContent.childCount; i++)
        {
            Transform child = _carouselContent.GetChild(i);
            if (child is RectTransform rect && child.name.StartsWith("Song Card -", System.StringComparison.Ordinal))
                return rect;
        }

        if (_carouselContent.childCount > 0 && _carouselContent.GetChild(0) is RectTransform firstChild)
            return firstChild;

        return null;
    }

    private void ClearCarouselContentButTemplate()
    {
        if (_carouselContent == null)
            return;

        for (int i = _carouselContent.childCount - 1; i >= 0; i--)
        {
            Transform child = _carouselContent.GetChild(i);
            if (child == _runtimeCardTemplate)
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        if (_runtimeCardTemplate == null)
            return;

        _runtimeCardTemplate.name = "Song Card Template";
        _runtimeCardTemplate.gameObject.SetActive(false);
    }

    private void EnsurePreviewAudioSource()
    {
        _previewAudioSource = GetComponent<AudioSource>();
        if (_previewAudioSource == null)
            _previewAudioSource = gameObject.AddComponent<AudioSource>();
        _previewAudioSource.playOnAwake = false;
        _previewAudioSource.spatialBlend = 0f;
    }

    private SongData GetFirstSong()
    {
        foreach (SongData song in _songList) if (song != null) return song;
        return null;
    }

    private SongData GetInitialSong()
    {
        SongData selected = SelectedSongManager.Instance != null ? SelectedSongManager.Instance.SelectedSong : null;
        if (selected != null && _songList.Contains(selected))
            return selected;

        string lastGroupId = PlayerPrefs.GetString(SelectedSongManager.LastSelectedSongGroupKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(lastGroupId))
        {
            foreach (SongData song in _songList)
            {
                if (song != null && GetSongGroupKey(song) == SongData.SanitizeForFileName(lastGroupId))
                    return song;
            }
        }

        return GetFirstSong();
    }

    private static Difficulty LoadLastSelectedDifficulty()
    {
        int saved = PlayerPrefs.GetInt(SelectedSongManager.LastSelectedDifficultyKey, (int)Difficulty.Medium);
        return System.Enum.IsDefined(typeof(Difficulty), saved) ? (Difficulty)saved : Difficulty.Medium;
    }

    private static Difficulty GetPreferredDifficulty(SongData song)
    {
        if (song == null)
            return Difficulty.Medium;

        if (song.HasPlayableChart(Difficulty.Medium))
            return Difficulty.Medium;
        if (song.HasPlayableChart(Difficulty.Easy))
            return Difficulty.Easy;
        if (song.HasPlayableChart(Difficulty.Hard))
            return Difficulty.Hard;

        return song.difficultyLevel;
    }

    private static string GetBpmLabel(SongData song) => song != null && song._bpm > 0f ? $"BPM: {song._bpm:0.#}" : "BPM: --";
    private static string GetDifficultyLabel(SongData song) => song == null ? "--" : !string.IsNullOrWhiteSpace(song._difficulty) ? song._difficulty : GetDifficultyLabel(song.difficultyLevel);
    private static string GetDifficultyLabel(Difficulty difficulty) => difficulty == Difficulty.Medium ? "Normal" : difficulty.ToString();
    private static Color DifficultyColor(SongData song) => song != null ? DifficultyColor(song.difficultyLevel) : DifficultyColor(Difficulty.Easy);
    private static Color DifficultyColor(Difficulty difficulty) =>
        difficulty == Difficulty.Hard   ? new Color(0.85f, 0.22f, 0.22f, 1.00f) :  // Red-orange for Hard
        difficulty == Difficulty.Medium ? new Color(0.18f, 0.62f, 0.30f, 1.00f) :  // Green for Normal
                                         new Color(0.14f, 0.44f, 0.72f, 1.00f);    // Blue for Easy

    private static bool WasConfirmPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
    }

    private static bool WasPreviousPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.leftArrowKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.LeftArrow);
#endif
    }

    private static bool WasNextPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rightArrowKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.RightArrow);
#endif
    }

    private static bool WasAddMoneyPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.f6Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F6);
#endif
    }

    private static bool WasAddDiamondPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.f7Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F7);
#endif
    }

    private static bool WasResetWalletPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F8);
#endif
    }

    private static void EnsureCanvasScaler(Canvas canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        GameObject eventSystem = new("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static void RemoveOldGeneratedRoot(Transform canvasTransform)
    {
        Transform oldRoot = canvasTransform.Find(GeneratedRootName);
        if (oldRoot == null)
            return;

        if (Application.isPlaying)
            Destroy(oldRoot.gameObject);
        else
            DestroyImmediate(oldRoot.gameObject);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject obj = new(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.gameObject.AddComponent<Image>().color = color;
        return rect;
    }

    private static RectTransform CreateButton(string name, Transform parent, string text, Color color, float fontSize = 16f)
    {
        RectTransform rect = CreatePanel(name, parent, color);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        CreateText(text, rect, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, Vector2.zero, new Vector2(120f, 36f));
        return rect;
    }

    private static TextMeshProUGUI CreateText(string text, Transform parent, float size, FontStyles style, TextAlignmentOptions alignment, Color color, Vector2 position, Vector2 dimensions, Vector2? min = null, Vector2? max = null)
    {
        RectTransform rect = CreateRect("Text - " + text, parent);
        Vector2 anchorMin = min ?? new Vector2(0.5f, 0.5f);
        Vector2 anchorMax = max ?? anchorMin;
        Anchor(rect, anchorMin, anchorMax, anchorMin, position, dimensions);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = alignment;
        label.color = color;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        TmpRuntimeFontFallback.Apply(label);
        label.text = text;
        return label;
    }

    private static TextMeshProUGUI FindText(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        TextMeshProUGUI[] labels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI label in labels)
        {
            if (label != null && label.gameObject.name == objectName)
                return label;
        }

        return null;
    }

    private static TextMeshProUGUI FirstText(Transform root)
    {
        return root != null ? root.GetComponentInChildren<TextMeshProUGUI>(true) : null;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = Vector2.zero;
    }

    private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size;
    }
}
