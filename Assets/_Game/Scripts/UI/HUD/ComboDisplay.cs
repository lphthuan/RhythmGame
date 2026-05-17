using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Hiển thị combo lên HUD.
/// Chỉ chịu trách nhiệm hiển thị — mọi logic combo nằm ở ComboManager.
/// 
/// Setup trong Inspector:
/// 1. Kéo ComboManager vào trường _comboManager.
/// 2. Kéo TMP text đã tạo sẵn trên Canvas vào _labelText và _numberText.
/// 3. Tuỳ chỉnh font, size, màu, position trực tiếp trên các TMP component.
/// 4. Tuỳ chỉnh animation trong Inspector (punchScale, duration, ...).
/// 
/// Khi combo break → UI fade out (chỉ hiện khoảng trống).
/// Khi combo tăng lại → UI fade in + punch scale.
/// </summary>
public class ComboDisplay : MonoBehaviour
{
    // ──────────────────────────────────────
    // References (kéo từ Inspector)
    // ──────────────────────────────────────

    [Header("References")]
    [Tooltip("ComboManager cung cấp dữ liệu combo.")]
    [SerializeField] private ComboManager _comboManager;

    [Tooltip("Text hiển thị chữ 'COMBO' (hoặc label bất kỳ). Để trống nếu không cần label.")]
    [SerializeField] private TextMeshProUGUI _labelText;

    [Tooltip("Text hiển thị số combo.")]
    [SerializeField] private TextMeshProUGUI _numberText;

    [Tooltip("CanvasGroup trên root chứa label + number. Dùng để fade in/out.")]
    [SerializeField] private CanvasGroup _canvasGroup;

    // ──────────────────────────────────────
    // Animation Settings
    // ──────────────────────────────────────

    [Header("Punch Scale")]
    [Tooltip("Scale tối đa khi combo tăng.")]
    [SerializeField] private float _punchScale = 1.3f;

    [Tooltip("Thời gian punch (giây).")]
    [SerializeField] private float _punchDuration = 0.12f;

    [Header("Milestone Punch")]
    [Tooltip("Scale tối đa khi đạt milestone.")]
    [SerializeField] private float _milestonePunchScale = 1.55f;

    [Tooltip("Thời gian punch milestone (giây).")]
    [SerializeField] private float _milestonePunchDuration = 0.2f;

    [Header("Fade")]
    [Tooltip("Thời gian fade in khi combo bắt đầu tăng lại.")]
    [SerializeField] private float _fadeInDuration = 0.1f;

    [Tooltip("Thời gian fade out khi combo break.")]
    [SerializeField] private float _fadeOutDuration = 0.25f;

    // ──────────────────────────────────────
    // Color Settings
    // ──────────────────────────────────────

    [Header("Colors")]
    [Tooltip("Màu mặc định của số combo.")]
    [SerializeField] private Color _normalColor = Color.white;

    [Tooltip("Màu khi đạt milestone.")]
    [SerializeField] private Color _milestoneColor = new Color(1f, 0.85f, 0.2f, 1f);

    [Tooltip("Thời gian giữ màu milestone trước khi trở về màu thường (giây).")]
    [SerializeField] private float _milestoneColorDuration = 0.5f;

    // ──────────────────────────────────────
    // Private State
    // ──────────────────────────────────────

    private RectTransform _scaleTarget;

    private Coroutine _scaleCoroutine;
    private Coroutine _fadeCoroutine;
    private Coroutine _milestoneColorCoroutine;

    private bool _isVisible;

    // ──────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────

    private void Awake()
    {
        CacheScaleTarget();
        HideImmediate();
    }

    private void OnEnable()
    {
        if (_comboManager != null)
        {
            _comboManager.OnComboChanged += HandleComboChanged;
            _comboManager.OnComboMilestone += HandleMilestone;
        }
    }

    private void OnDisable()
    {
        if (_comboManager != null)
        {
            _comboManager.OnComboChanged -= HandleComboChanged;
            _comboManager.OnComboMilestone -= HandleMilestone;
        }
    }

    // ──────────────────────────────────────
    // Event Handlers
    // ──────────────────────────────────────

    private void HandleComboChanged(ComboData data)
    {
        if (data.IsBreak)
        {
            FadeOut();
            return;
        }

        UpdateNumberText(data.CurrentCombo);
        SetTextColor(_normalColor);

        if (!_isVisible)
        {
            FadeIn();
        }

        PlayPunchScale(_punchScale, _punchDuration);
    }

    private void HandleMilestone(int combo)
    {
        SetTextColor(_milestoneColor);

        PlayPunchScale(_milestonePunchScale, _milestonePunchDuration);

        if (_milestoneColorCoroutine != null)
            StopCoroutine(_milestoneColorCoroutine);

        _milestoneColorCoroutine = StartCoroutine(
            MilestoneColorRoutine(_milestoneColorDuration)
        );
    }

    // ──────────────────────────────────────
    // UI Update
    // ──────────────────────────────────────

    private void UpdateNumberText(int combo)
    {
        if (_numberText != null)
            _numberText.text = combo.ToString();
    }

    private void SetTextColor(Color color)
    {
        if (_numberText != null)
            _numberText.color = color;

        if (_labelText != null)
            _labelText.color = color;
    }

    // ──────────────────────────────────────
    // Animations — Punch Scale
    // ──────────────────────────────────────

    private void PlayPunchScale(float targetScale, float duration)
    {
        if (_scaleTarget == null)
            return;

        if (_scaleCoroutine != null)
            StopCoroutine(_scaleCoroutine);

        _scaleCoroutine = StartCoroutine(PunchScaleRoutine(targetScale, duration));
    }

    private IEnumerator PunchScaleRoutine(float targetScale, float duration)
    {
        float halfDuration = duration * 0.5f;

        // Scale up.
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scale = Mathf.Lerp(1f, targetScale, EaseOutQuad(t));
            _scaleTarget.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        // Scale down.
        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scale = Mathf.Lerp(targetScale, 1f, EaseInQuad(t));
            _scaleTarget.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        _scaleTarget.localScale = Vector3.one;
        _scaleCoroutine = null;
    }

    // ──────────────────────────────────────
    // Animations — Fade
    // ──────────────────────────────────────

    private void FadeIn()
    {
        if (_canvasGroup == null)
            return;

        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeRoutine(1f, _fadeInDuration));
        _isVisible = true;
    }

    private void FadeOut()
    {
        if (_canvasGroup == null)
            return;

        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeRoutine(0f, _fadeOutDuration));
        _isVisible = false;
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = _canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, EaseOutQuad(t));
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
        _fadeCoroutine = null;
    }

    private void HideImmediate()
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;

        _isVisible = false;
    }

    // ──────────────────────────────────────
    // Animations — Milestone Color
    // ──────────────────────────────────────

    private IEnumerator MilestoneColorRoutine(float holdDuration)
    {
        yield return new WaitForSeconds(holdDuration);

        SetTextColor(_normalColor);
        _milestoneColorCoroutine = null;
    }

    // ──────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────

    private void CacheScaleTarget()
    {
        if (_canvasGroup != null)
        {
            _scaleTarget = _canvasGroup.GetComponent<RectTransform>();
        }
        else
        {
            _scaleTarget = GetComponent<RectTransform>();
        }
    }

    private static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private static float EaseInQuad(float t)
    {
        return t * t;
    }
}
