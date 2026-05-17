using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// TEST SCENE ONLY
// Hiển thị combo lên UI và log ra Console.
// Khi combo break → ẩn UI (chỉ hiện khoảng trống).
// Khi combo tăng → hiện số + hiệu ứng scale.
// Không đem qua scene chính.
public class TestComboLogger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ComboManager _comboManager;
    [SerializeField] private Canvas _targetCanvas;

    [Header("UI Position")]
    [SerializeField] private Vector2 _comboAnchoredPosition = new Vector2(0f, 100f);

    [Header("Animation")]
    [SerializeField] private float _punchScale = 1.35f;
    [SerializeField] private float _punchDuration = 0.12f;
    [SerializeField] private float _milestonePunchScale = 1.6f;
    [SerializeField] private float _milestonePunchDuration = 0.2f;
    [SerializeField] private float _fadeInDuration = 0.1f;
    [SerializeField] private float _fadeOutDuration = 0.25f;

    [Header("Visuals")]
    [SerializeField] private int _labelFontSize = 28;
    [SerializeField] private int _numberFontSize = 72;
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _milestoneColor = new Color(1f, 0.85f, 0.2f, 1f);

    private RectTransform _comboRoot;
    private CanvasGroup _comboCanvasGroup;
    private TextMeshProUGUI _labelText;
    private TextMeshProUGUI _numberText;

    private Coroutine _scaleCoroutine;
    private Coroutine _fadeCoroutine;

    private bool _isVisible;

    private void Start()
    {
        ResolveReferences();

        if (_comboManager == null)
        {
            Debug.LogError("TestComboLogger needs a ComboManager.");
            enabled = false;
            return;
        }

        if (_targetCanvas == null)
        {
            Debug.LogError("TestComboLogger needs a Canvas.");
            enabled = false;
            return;
        }

        CreateComboUI();
        HideImmediate();
    }

    private void OnEnable()
    {
        if (_comboManager != null)
        {
            _comboManager.OnComboChanged += HandleComboChanged;
            _comboManager.OnComboMilestone += HandleMilestone;
            _comboManager.OnComboBreak += HandleComboBreak;
        }
    }

    private void OnDisable()
    {
        if (_comboManager != null)
        {
            _comboManager.OnComboChanged -= HandleComboChanged;
            _comboManager.OnComboMilestone -= HandleMilestone;
            _comboManager.OnComboBreak -= HandleComboBreak;
        }
    }

    // ──────────────────────────────────────
    // Event Handlers
    // ──────────────────────────────────────

    private void HandleComboChanged(ComboData data)
    {
        if (data.IsBreak)
        {
            // Combo break → ẩn UI, chỉ hiện khoảng trống.
            FadeOut();

            Debug.Log($"[COMBO] Break → combo reset to 0 | Max: {data.MaxCombo}");
            return;
        }

        // Combo tăng → hiện số.
        _numberText.text = data.CurrentCombo.ToString();
        _numberText.color = _normalColor;
        _labelText.color = _normalColor;

        if (!_isVisible)
        {
            FadeIn();
        }

        PlayPunchScale(_punchScale, _punchDuration);

        Debug.Log($"[COMBO] Current: {data.CurrentCombo} | Max: {data.MaxCombo}");
    }

    private void HandleMilestone(int combo)
    {
        _numberText.color = _milestoneColor;
        _labelText.color = _milestoneColor;

        PlayPunchScale(_milestonePunchScale, _milestonePunchDuration);

        Debug.Log($"<color=yellow>[COMBO MILESTONE]</color> Reached {combo} combo!");
    }

    private void HandleComboBreak(int previousCombo)
    {
        Debug.Log($"<color=red>[COMBO BREAK]</color> Lost {previousCombo} combo!");
    }

    // ──────────────────────────────────────
    // UI Creation (Runtime)
    // ──────────────────────────────────────

    private void CreateComboUI()
    {
        // Root container.
        GameObject rootObject = new GameObject(
            "TEST_COMBO_UI",
            typeof(RectTransform),
            typeof(CanvasGroup)
        );

        rootObject.transform.SetParent(_targetCanvas.transform, false);

        _comboRoot = rootObject.GetComponent<RectTransform>();
        _comboRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _comboRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _comboRoot.pivot = new Vector2(0.5f, 0.5f);
        _comboRoot.sizeDelta = new Vector2(400f, 160f);
        _comboRoot.anchoredPosition = _comboAnchoredPosition;

        _comboCanvasGroup = rootObject.GetComponent<CanvasGroup>();

        // Vertical layout: label on top, number below.
        VerticalLayoutGroup layout = rootObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = -4f;

        // "COMBO" label.
        _labelText = CreateTextElement(
            rootObject.transform,
            "TEST_COMBO_LABEL",
            "COMBO",
            _labelFontSize,
            FontStyles.Bold,
            60f
        );

        _labelText.alpha = 0.7f;
        _labelText.characterSpacing = 12f;

        // Number text.
        _numberText = CreateTextElement(
            rootObject.transform,
            "TEST_COMBO_NUMBER",
            "0",
            _numberFontSize,
            FontStyles.Bold,
            90f
        );
    }

    private TextMeshProUGUI CreateTextElement(
        Transform parent,
        string objectName,
        string text,
        int fontSize,
        FontStyles fontStyle,
        float preferredHeight
    )
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400f, preferredHeight);

        LayoutElement layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = preferredHeight;

        TextMeshProUGUI tmpText = textObject.GetComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = fontSize;
        tmpText.fontStyle = fontStyle;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = _normalColor;
        tmpText.raycastTarget = false;
        tmpText.enableWordWrapping = false;

        return tmpText;
    }

    // ──────────────────────────────────────
    // Animations
    // ──────────────────────────────────────

    private void PlayPunchScale(float targetScale, float duration)
    {
        if (_scaleCoroutine != null)
            StopCoroutine(_scaleCoroutine);

        _scaleCoroutine = StartCoroutine(PunchScaleRoutine(targetScale, duration));
    }

    private IEnumerator PunchScaleRoutine(float targetScale, float duration)
    {
        if (_comboRoot == null)
            yield break;

        float halfDuration = duration * 0.5f;

        // Scale up.
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scale = Mathf.Lerp(1f, targetScale, EaseOutQuad(t));
            _comboRoot.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        // Scale down.
        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scale = Mathf.Lerp(targetScale, 1f, EaseInQuad(t));
            _comboRoot.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        _comboRoot.localScale = Vector3.one;
        _scaleCoroutine = null;
    }

    private void FadeIn()
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeRoutine(1f, _fadeInDuration));
        _isVisible = true;
    }

    private void FadeOut()
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeRoutine(0f, _fadeOutDuration));
        _isVisible = false;
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        if (_comboCanvasGroup == null)
            yield break;

        float startAlpha = _comboCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _comboCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, EaseOutQuad(t));
            yield return null;
        }

        _comboCanvasGroup.alpha = targetAlpha;
        _fadeCoroutine = null;
    }

    private void HideImmediate()
    {
        if (_comboCanvasGroup != null)
            _comboCanvasGroup.alpha = 0f;

        _isVisible = false;
    }

    // ──────────────────────────────────────
    // Resolve
    // ──────────────────────────────────────

    private void ResolveReferences()
    {
        if (_comboManager == null)
            _comboManager = FindFirstObjectByType<ComboManager>();

        if (_targetCanvas == null)
            _targetCanvas = FindFirstObjectByType<Canvas>();
    }

    // ──────────────────────────────────────
    // Easing
    // ──────────────────────────────────────

    private static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private static float EaseInQuad(float t)
    {
        return t * t;
    }
}
