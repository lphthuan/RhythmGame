using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameplayPauseController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private ChartPlaybackClock playbackClock;

    [Header("Navigation")]
    [SerializeField] private string songSelectSceneName = "MusicSelectionScene";

    [Header("Generated UI")]
    [SerializeField] private bool buildRuntimeUi = true;
    [SerializeField] private Vector2 pauseButtonPosition = new Vector2(-42f, -42f);
    [SerializeField] private Vector2 pauseButtonSize = new Vector2(76f, 52f);

    private const string RootName = "RG Gameplay Pause UI";

    private GameObject _root;
    private GameObject _panel;
    private bool _paused;

    private void Awake()
    {
        if (targetCanvas == null)
            targetCanvas = FindFirstObjectByType<Canvas>();

        if (playbackClock == null)
            playbackClock = FindFirstObjectByType<ChartPlaybackClock>();

        EnsureEventSystem();
    }

    private void Start()
    {
        if (buildRuntimeUi)
            BuildUi();
    }

    public void PauseGame()
    {
        if (_paused)
            return;

        _paused = true;
        Time.timeScale = 0f;
        playbackClock?.Pause();

        if (_panel != null)
            _panel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (!_paused)
            return;

        _paused = false;
        Time.timeScale = 1f;
        playbackClock?.Play();

        if (_panel != null)
            _panel.SetActive(false);
    }

    public void RetryGame()
    {
        Time.timeScale = 1f;
        SceneLoadUtility.ReloadActiveScene();
    }

    public void BackToSongSelect()
    {
        Time.timeScale = 1f;
        SceneLoadUtility.LoadSceneByName(songSelectSceneName);
    }

    private void BuildUi()
    {
        if (targetCanvas == null)
            return;

        Transform oldRoot = targetCanvas.transform.Find(RootName);
        if (oldRoot != null)
            Destroy(oldRoot.gameObject);

        RectTransform root = CreateRect(RootName, targetCanvas.transform);
        Stretch(root);
        root.SetAsLastSibling();
        _root = root.gameObject;

        RectTransform pauseButton = CreatePanel("Pause Button", root, new Color(0.22f, 0.06f, 0.28f, 0.86f));
        Anchor(pauseButton, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), pauseButtonPosition, pauseButtonSize);
        Button pause = pauseButton.gameObject.AddComponent<Button>();
        pause.targetGraphic = pauseButton.GetComponent<Image>();
        pause.onClick.AddListener(PauseGame);
        CreateText("II", pauseButton, 24f, FontStyles.Bold);

        RectTransform panel = CreatePanel("Pause Panel", root, new Color(0.03f, 0.02f, 0.06f, 0.84f));
        Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 310f));
        _panel = panel.gameObject;

        CreateText("PAUSED", panel, 34f, FontStyles.Bold, new Vector2(0f, 104f), new Vector2(260f, 54f));
        CreateMenuButton("Resume", panel, new Vector2(0f, 42f), ResumeGame);
        CreateMenuButton("Retry", panel, new Vector2(0f, -24f), RetryGame);
        CreateMenuButton("Back", panel, new Vector2(0f, -90f), BackToSongSelect);

        _panel.SetActive(false);
    }

    private void CreateMenuButton(string label, Transform parent, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        RectTransform buttonRect = CreatePanel(label + " Button", parent, new Color(0.46f, 0.13f, 0.48f, 0.95f));
        Anchor(buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(220f, 48f));

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonRect.GetComponent<Image>();
        button.onClick.AddListener(action);

        CreateText(label, buttonRect, 20f, FontStyles.Bold);
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static RectTransform CreatePanel(string objectName, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(objectName, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    private static TextMeshProUGUI CreateText(
        string text,
        Transform parent,
        float fontSize,
        FontStyles fontStyle,
        Vector2? position = null,
        Vector2? size = null)
    {
        RectTransform rect = CreateRect("Text - " + text, parent);
        Anchor(
            rect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            position ?? Vector2.zero,
            size ?? new Vector2(180f, 42f));

        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
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

    private static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
}
