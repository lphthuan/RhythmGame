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
    }

    private void Awake() => Instance = this;

    private void Start()
    {
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
            canvas = FindFirstObjectByType<Canvas>();
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
    }

    public void PopulateList()
    {
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
        {
            SelectSong(song, false);
            PlaySelectedSong();
        }
        else
            SelectSong(song, true);
    }

    public void PlaySelectedSong()
    {
        SongData song = _selectedSong != null
            ? _selectedSong
            : SelectedSongManager.Instance != null ? SelectedSongManager.Instance.SelectedSong : null;
        if (song == null)
            return;

        if (SelectedSongManager.Instance != null)
            SelectedSongManager.Instance.SetSelectedSong(song, _selectedDifficulty);

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
            canvas = FindFirstObjectByType<Canvas>();
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
        _lastScoreText = FindText(root, "Last Score Value");
        _bestScoreText = FindText(root, "Best Score Value");
        _rankText = FindText(root, "Best Rank Value");

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
        _carouselContent.sizeDelta = new Vector2(_carouselContent.sizeDelta.x, Mathf.Max(560f, _songList.Count * 118f + 84f));
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

        BuildCurrencyBadge(bar, "Money", "0", new Vector2(-183f, 0f), new Color(0.08f, 0.38f, 0.49f, 0.96f));
        BuildCurrencyBadge(bar, "Diamond", "0", new Vector2(-72f, 0f), new Color(0.43f, 0.21f, 0.66f, 0.96f));
    }

    private static void BuildCurrencyBadge(RectTransform parent, string title, string value, Vector2 position, Color color)
    {
        RectTransform badge = CreatePanel(title + " Badge", parent, color);
        Anchor(badge, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), position, new Vector2(96f, 42f));
        CreateText(value, badge, 20, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, new Vector2(0f, 7f), new Vector2(88f, 24f));
        CreateText(title, badge, 10, FontStyles.Normal, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.82f), new Vector2(0f, -11f), new Vector2(88f, 18f));
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

        RectTransform score = CreatePanel("Score Panel", detail, new Color(0.10f, 0.05f, 0.16f, 0.78f));
        Anchor(score, new Vector2(0.035f, 0.07f), new Vector2(0.40f, 0.43f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        CreateText("LAST SCORE", score, 13, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.86f, 0.75f, 1f, 1f), new Vector2(0f, 46f), new Vector2(210f, 22f));
        _lastScoreText = CreateText("0000000", score, 24, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, new Vector2(0f, 20f), new Vector2(220f, 30f));
        _lastScoreText.gameObject.name = "Last Score Value";
        CreateText("BEST SCORE", score, 13, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.86f, 0.75f, 1f, 1f), new Vector2(-35f, -17f), new Vector2(160f, 22f));
        _bestScoreText = CreateText("0000000", score, 19, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, new Vector2(-25f, -39f), new Vector2(165f, 28f));
        _bestScoreText.gameObject.name = "Best Score Value";
        _rankText = CreateText("-", score, 32, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.55f, 0.25f, 1f), new Vector2(77f, -28f), new Vector2(56f, 54f));
        _rankText.gameObject.name = "Best Rank Value";
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
        _carouselContent.sizeDelta = new Vector2(0f, Mathf.Max(560f, _songList.Count * 118f + 84f));
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
                card.Rect.anchoredPosition = new Vector2(14f, -62f - songIndex * 118f);
                songIndex++;
            }
        }
    }

    private void CreateSongCard(SongData song, RectTransform parent)
    {
        if (_runtimeCardTemplate != null && Application.isPlaying)
        {
            CreateSongCardFromTemplate(song, parent);
            return;
        }

        RectTransform card = CreatePanel("Song Card - " + song.SongTitle, parent, new Color(0.11f, 0.03f, 0.16f, 0.92f));
        card.sizeDelta = new Vector2(430f, 96f);
        Image background = card.GetComponent<Image>();
        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() => SelectOrPlaySong(song));

        RectTransform artMask = CreatePanel("Card Art", card, Color.white);
        Anchor(artMask, new Vector2(0f, 0f), new Vector2(0.24f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        artMask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        Image art = CreatePanel("Art", artMask, new Color(1f, 1f, 1f, 0.32f)).GetComponent<Image>();
        Stretch(art.rectTransform);
        art.sprite = song.PreviewImage;
        art.preserveAspect = false;
        art.raycastTarget = false;

        RectTransform difficulty = CreatePanel("Difficulty", card, DifficultyColor(song));
        Anchor(difficulty, new Vector2(0.24f, 0f), new Vector2(0.39f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI difficultyText = CreateText(GetDifficultyLabel(song), difficulty, 13, FontStyles.Bold, TextAlignmentOptions.Center, Color.white, Vector2.zero, new Vector2(62f, 34f));
        difficultyText.gameObject.name = "Difficulty Label";
        RectTransform textArea = CreateRect("Text", card);
        Anchor(textArea, new Vector2(0.40f, 0f), new Vector2(0.78f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform titleClip = CreateRect("Title Clip", textArea);
        Anchor(titleClip, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 15f), new Vector2(-8f, 28f));
        titleClip.gameObject.AddComponent<RectMask2D>();
        TextMeshProUGUI title = CreateText(song.SongTitle, titleClip, 17, FontStyles.Bold, TextAlignmentOptions.Left, Color.white, Vector2.zero, new Vector2(460f, 28f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        title.gameObject.name = "Card Title";
        SongTitleMarquee marquee = title.gameObject.AddComponent<SongTitleMarquee>();
        TextMeshProUGUI bpm = CreateText(GetBpmLabel(song), textArea, 12, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.97f, 0.86f, 1f, 1f), new Vector2(4f, -18f), new Vector2(170f, 22f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        bpm.gameObject.name = "Card BPM";
        RectTransform scorePanel = CreatePanel("Card Score", card, new Color(0.18f, 0.04f, 0.17f, 0.72f));
        Anchor(scorePanel, new Vector2(0.79f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI rank = CreateText("-", scorePanel, 28, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.55f, 0.82f, 1f), new Vector2(0f, 14f), new Vector2(84f, 34f));
        TextMeshProUGUI bestScore = CreateText("0000000", scorePanel, 12, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.86f), new Vector2(0f, -18f), new Vector2(86f, 20f));
        rank.gameObject.name = "Card Rank";
        bestScore.gameObject.name = "Card Best Score";
        _cards[song] = new CarouselCard { Rect = card, Background = background, Art = art, Difficulty = difficultyText, Title = title, Bpm = bpm, Rank = rank, BestScore = bestScore, Marquee = marquee };
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
        button.onClick = new Button.ButtonClickedEvent();
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
        _cards[song] = new CarouselCard { Rect = card, Background = background, Art = art, Difficulty = difficultyText, Title = title, Bpm = bpm, Rank = rank, BestScore = bestScore, Marquee = marquee };
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
        if (_artistText != null)
            _artistText.text = "Tap selected song again to start.";
        if (_bpmText != null)
            _bpmText.text = GetBpmLabel(song);
        if (_previewArt != null)
        {
            _previewArt.sprite = song.PreviewImage;
            _previewArt.color = song.PreviewImage != null ? Color.white : new Color(0.38f, 0.19f, 0.56f, 1f);
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
        UpdateDifficultyButtons();
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
            float t = Mathf.Clamp01(1f - Mathf.Abs(localY) / 220f);
            float scale = Mathf.Lerp(0.74f, 1.12f, t);
            float lerpSpeed = Application.isPlaying ? Time.unscaledDeltaTime * 12f : 1f;
            item.Value.Rect.localScale = Vector3.Lerp(item.Value.Rect.localScale, Vector3.one * scale, lerpSpeed);
            item.Value.Rect.anchoredPosition = new Vector2(Mathf.Lerp(item.Value.Rect.anchoredPosition.x, Mathf.Lerp(38f, -14f, t), lerpSpeed), item.Value.Rect.anchoredPosition.y);
            Color targetColor = item.Key == _selectedSong
                ? new Color(0.60f, 0.08f, 0.44f, 0.98f)
                : new Color(0.11f, 0.03f, 0.16f, 0.92f);
            if (item.Value.Background != null)
                item.Value.Background.color = Color.Lerp(item.Value.Background.color, targetColor, lerpSpeed);
            Difficulty displayDifficulty = item.Key == _selectedSong ? _selectedDifficulty : GetPreferredDifficulty(item.Key);
            SongPlayStats stats = SongPlayStats.Load(item.Key, displayDifficulty);
            if (item.Value.Difficulty != null)
            {
                item.Value.Difficulty.text = GetDifficultyLabel(displayDifficulty).ToUpperInvariant();
                Image difficultyImage = item.Value.Difficulty.transform.parent.GetComponent<Image>();
                if (difficultyImage != null)
                    difficultyImage.color = DifficultyColor(displayDifficulty);
            }
            if (item.Value.Rank != null)
                item.Value.Rank.text = string.IsNullOrEmpty(stats.BestRank) ? "-" : stats.BestRank;
            if (item.Value.BestScore != null)
                item.Value.BestScore.text = stats.BestScore.ToString("D7");
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
#if UNITY_EDITOR
        if (!includeProjectSongData || string.IsNullOrWhiteSpace(projectSongDataFolder)) return;
        HashSet<SongData> existing = new(_songList);
        foreach (string guid in AssetDatabase.FindAssets("t:SongData", new[] { projectSongDataFolder }))
        {
            SongData song = AssetDatabase.LoadAssetAtPath<SongData>(AssetDatabase.GUIDToAssetPath(guid));
            if (song != null && existing.Add(song)) _songList.Add(song);
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

        if (!string.IsNullOrWhiteSpace(song.normalChartFileName) || song.normalTimelineAsset != null)
            return Difficulty.Medium;
        if (!string.IsNullOrWhiteSpace(song.easyChartFileName) || song.easyTimelineAsset != null)
            return Difficulty.Easy;
        if (!string.IsNullOrWhiteSpace(song.hardChartFileName) || song.hardTimelineAsset != null)
            return Difficulty.Hard;

        return song.difficultyLevel;
    }

    private static string GetBpmLabel(SongData song) => song != null && song._bpm > 0f ? $"BPM: {song._bpm:0.#}" : "BPM: --";
    private static string GetDifficultyLabel(SongData song) => song == null ? "--" : !string.IsNullOrWhiteSpace(song._difficulty) ? song._difficulty : GetDifficultyLabel(song.difficultyLevel);
    private static string GetDifficultyLabel(Difficulty difficulty) => difficulty == Difficulty.Medium ? "Normal" : difficulty.ToString();
    private static Color DifficultyColor(SongData song) => song != null ? DifficultyColor(song.difficultyLevel) : DifficultyColor(Difficulty.Easy);
    private static Color DifficultyColor(Difficulty difficulty) => difficulty == Difficulty.Hard ? new Color(0.68f, 0.08f, 0.24f, 0.98f) : difficulty == Difficulty.Medium ? new Color(0.66f, 0.08f, 0.45f, 0.98f) : new Color(0.12f, 0.42f, 0.59f, 0.98f);

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
