using TMPro;
using System.Collections;
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
    [SerializeField] private Vector2 pauseButtonPosition = new Vector2(92f, -44f);
    [SerializeField] private Vector2 pauseButtonSize = new Vector2(96f, 58f);

    private const string RootName = "RG Gameplay Pause UI";

    private GameObject _root;
    private GameObject _panel;
    private TextMeshProUGUI _countdownText;
    private bool _paused;
    private bool _resuming;
    private Coroutine _resumeCoroutine;

    private void Awake()
    {
        if (pauseButtonPosition.x < 0f)
            pauseButtonPosition = new Vector2(92f, -44f);

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
        if (_paused || _resuming)
            return;

        _paused = true;
        Time.timeScale = 0f;
        playbackClock?.Pause();

        if (_panel != null)
            _panel.SetActive(true);
    }

    public void ResumeGame()
    {
        if (!_paused || _resuming)
            return;

        _resumeCoroutine = StartCoroutine(ResumeCountdown());
    }

    private IEnumerator ResumeCountdown()
    {
        _resuming = true;
        if (_panel != null)
            _panel.SetActive(false);

        if (_countdownText != null)
        {
            _countdownText.gameObject.SetActive(true);
            for (int i = 3; i > 0; i--)
            {
                _countdownText.text = i.ToString();
                yield return new WaitForSecondsRealtime(1f);
            }

            _countdownText.text = "GO";
            yield return new WaitForSecondsRealtime(0.35f);
            _countdownText.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSecondsRealtime(1f);
        }

        _paused = false;
        _resuming = false;
        _resumeCoroutine = null;
        Time.timeScale = 1f;
        playbackClock?.Play();
    }

    public void RetryGame()
    {
        StopResumeCountdownIfNeeded();
        Time.timeScale = 1f;
        playbackClock?.Stop();
        SceneLoadUtility.ReloadActiveScene();
    }

    public void BackToSongSelect()
    {
        StopResumeCountdownIfNeeded();
        Time.timeScale = 1f;
        playbackClock?.Stop();
        SceneLoadUtility.LoadSceneByName(songSelectSceneName);
    }

    private void OnDisable()
    {
        if (_paused || _resuming)
            Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (_paused || _resuming)
            Time.timeScale = 1f;
    }

    private void StopResumeCountdownIfNeeded()
    {
        if (_resumeCoroutine != null)
        {
            StopCoroutine(_resumeCoroutine);
            _resumeCoroutine = null;
        }

        _resuming = false;
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
        Anchor(pauseButton, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), pauseButtonPosition, pauseButtonSize);
        Button pause = pauseButton.gameObject.AddComponent<Button>();
        pause.targetGraphic = pauseButton.GetComponent<Image>();
        pause.onClick.AddListener(PauseGame);
        CreateText("PAUSE", pauseButton, 12f, FontStyles.Bold, new Vector2(0f, 14f), new Vector2(80f, 18f));
        CreateText("II", pauseButton, 25f, FontStyles.Bold, new Vector2(0f, -7f), new Vector2(80f, 30f));

        RectTransform panel = CreatePanel("Pause Panel", root, new Color(0.03f, 0.02f, 0.06f, 0.84f));
        Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 310f));
        _panel = panel.gameObject;

        CreateText("PAUSED", panel, 34f, FontStyles.Bold, new Vector2(0f, 104f), new Vector2(260f, 54f));
        CreateMenuButton("Resume", panel, new Vector2(0f, 42f), ResumeGame);
        CreateMenuButton("Retry", panel, new Vector2(0f, -24f), RetryGame);
        CreateMenuButton("Back", panel, new Vector2(0f, -90f), BackToSongSelect);

        _countdownText = CreateText("3", root, 90f, FontStyles.Bold, Vector2.zero, new Vector2(220f, 130f));
        _countdownText.gameObject.SetActive(false);
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
