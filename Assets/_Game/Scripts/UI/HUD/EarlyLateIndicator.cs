using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Hiển thị chỉ báo EARLY / LATE mỗi khi người chơi bấm lệch timing.
///
/// Cách setup trong Inspector:
/// 1. Kéo component này vào bất kỳ GameObject nào trong Gameplay scene.
/// 2. Kéo NoteManager vào trường _noteManager (hoặc để trống → tự tìm).
/// 3. Kéo Canvas chứa HUD vào _targetCanvas (hoặc để trống → tự tìm Canvas đầu tiên).
/// 4. Tuỳ chỉnh màu, font size, vị trí trong Inspector.
///
/// Quy ước deltaMs (từ JudgmentCalculator):
///   delta = inputTime - hitTime
///   delta &lt; 0 → bấm SỚM hơn → EARLY
///   delta &gt; 0 → bấm TRỄ hơn  → LATE
///
/// Settings tích hợp (đọc từ PlayerPrefs, đồng bộ với UIManager):
///   "PureLateEarly"    = 1 → ẩn indicator khi judgment là Perfect
///   "VisualLateEarlyPos" = 0 Middle | 1 Top | 2 Bottom
/// </summary>
public class EarlyLateIndicator : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────────
    // Inspector Fields
    // ──────────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("NoteManager phát ra event judgment. Để trống → tự tìm.")]
    [SerializeField] private NoteManager _noteManager;

    [Tooltip("Canvas để spawn text label lên. Để trống → tự tìm Canvas đầu tiên.")]
    [SerializeField] private Canvas _targetCanvas;

    [Header("Position Presets")]
    [Tooltip("Vị trí Y (anchoredPosition) khi chọn 'Middle' trong Settings.")]
    [SerializeField] private float _posYMiddle = 30f;

    [Tooltip("Vị trí Y (anchoredPosition) khi chọn 'Top' trong Settings.")]
    [SerializeField] private float _posYTop = 200f;

    [Tooltip("Vị trí Y (anchoredPosition) khi chọn 'Bottom' trong Settings.")]
    [SerializeField] private float _posYBottom = -80f;

    [Tooltip("Vị trí X của label (0 = chính giữa màn hình).")]
    [SerializeField] private float _posX = 0f;

    [Header("Visual")]
    [Tooltip("Sprite hiển thị khi EARLY (bấm sớm).")]
    [SerializeField] private Sprite _earlySprite;

    [Tooltip("Sprite hiển thị khi LATE (bấm trễ).")]
    [SerializeField] private Sprite _lateSprite;

    [Header("Animation")]
    [Tooltip("Scale bắt đầu khi label vừa xuất hiện (< 1 = bé hơn).")]
    [SerializeField] private float _startScale = 0.55f;

    [Tooltip("Scale đỉnh (pop).")]
    [SerializeField] private float _popScale = 1.15f;

    [Tooltip("Thời gian (giây) cho pha pop scale.")]
    [SerializeField] private float _popDuration = 0.10f;

    [Tooltip("Tổng thời gian hiển thị label (giây) trước khi fade-out hoàn toàn.")]
    [SerializeField] private float _lifetime = 0.55f;

    [Tooltip("Thời điểm bắt đầu fade-out (giây, tính từ lúc xuất hiện). Phải < _lifetime.")]
    [SerializeField] private float _fadeStartTime = 0.28f;

    [Tooltip("Khoảng cách trôi lên theo chiều Y trong suốt vòng đời label (px).")]
    [SerializeField] private float _floatUpDistance = 18f;

    // ──────────────────────────────────────────────────────────────
    // Private State
    // ──────────────────────────────────────────────────────────────

    private bool _isSubscribed;
    private bool _pureLateEarlyEnabled;   // true = ẩn indicator với Perfect
    private float _anchoredY;             // Vị trí Y hiện tại dựa theo Setting

    // ──────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        ResolveReferences();
        ReadSettings();
    }

    private void OnEnable()
    {
        ReadSettings();
        EnsureSubscribed();
    }

    private void Start()
    {
        ResolveReferences();
        ReadSettings();
        EnsureSubscribed();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    // ──────────────────────────────────────────────────────────────
    // Settings
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Đọc lại PlayerPrefs.
    /// Gọi lại hàm này sau khi người chơi thay đổi Settings để áp dụng ngay.
    /// </summary>
    public void ReadSettings()
    {
        _pureLateEarlyEnabled = PlayerPrefs.GetInt("PureLateEarly", 1) == 1;

        int posIndex = PlayerPrefs.GetInt("VisualLateEarlyPos", 0);
        _anchoredY = posIndex switch
        {
            1 => _posYTop,
            2 => _posYBottom,
            _ => _posYMiddle   // 0 = Middle (default)
        };
    }

    // ──────────────────────────────────────────────────────────────
    // Subscribe / Unsubscribe
    // ──────────────────────────────────────────────────────────────

    private void EnsureSubscribed()
    {
        if (_isSubscribed)
            return;

        ResolveReferences();

        if (_noteManager == null)
            return;

        _noteManager.OnNoteJudgedEvent += HandleNoteJudged;
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (_noteManager != null && _isSubscribed)
            _noteManager.OnNoteJudgedEvent -= HandleNoteJudged;

        _isSubscribed = false;
    }

    // ──────────────────────────────────────────────────────────────
    // Event Handler
    // ──────────────────────────────────────────────────────────────

    private void HandleNoteJudged(NoteBase note, HitJudgment judgment, float deltaMs)
    {
        if (note == null)
            return;

        // Bỏ qua Miss (người chơi không thực sự bấm)
        if (judgment == HitJudgment.None || judgment == HitJudgment.Miss)
            return;

        // "Pure Late/Early" = true → ẩn với Perfect (chỉ hiện với Great & Good)
        if (_pureLateEarlyEnabled && judgment == HitJudgment.Perfect)
            return;

        bool isEarly = deltaMs < 0f;

        Sprite spriteToUse = isEarly ? _earlySprite : _lateSprite;
        string nameSuffix = isEarly ? "EARLY" : "LATE";

        SpawnLabel(nameSuffix, spriteToUse);
    }

    // ──────────────────────────────────────────────────────────────
    // Spawn & Animate
    // ──────────────────────────────────────────────────────────────

    private void SpawnLabel(string nameSuffix, Sprite sprite)
    {
        if (sprite == null)
            return; // Tránh lỗi nếu chưa gán Sprite

        Transform parentTransform = null;

        // Tìm GameObject chứa UI gameplay để đảm bảo scale và vị trí chính xác
        GameObject hudRoot = GameObject.Find("RG Gameplay Live HUD");
        if (hudRoot != null)
        {
            parentTransform = hudRoot.transform;
        }
        else if (_targetCanvas != null)
        {
            parentTransform = _targetCanvas.transform;
        }

        if (parentTransform == null)
            return;

        // Tạo GameObject chứa Image
        GameObject obj = new GameObject(
            $"EL_{nameSuffix}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(UnityEngine.UI.Image),
            typeof(CanvasGroup)
        );

        obj.transform.SetParent(parentTransform, false);
        obj.transform.SetAsLastSibling();

        // Setup RectTransform
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);
        
        // Tự động scale dựa trên kích thước thật của ảnh
        float imgWidth = sprite.rect.width;
        float imgHeight = sprite.rect.height;
        // Chia tỷ lệ nhỏ lại chút nếu ảnh gốc quá to, hoặc cứ để nguyên kích thước
        rect.sizeDelta = new Vector2(imgWidth, imgHeight); 
        
        rect.anchoredPosition = new Vector2(_posX, _anchoredY);
        rect.localScale = Vector3.one * _startScale;

        // Setup Image
        UnityEngine.UI.Image img = obj.GetComponent<UnityEngine.UI.Image>();
        img.sprite = sprite;
        img.color = Color.white;
        img.preserveAspect = true;
        img.raycastTarget = false;

        // Setup CanvasGroup (dùng để fade)
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        cg.alpha         = 1f;
        cg.interactable  = false;
        cg.blocksRaycasts = false;

        StartCoroutine(AnimateLabel(obj, rect, cg));
    }

    private IEnumerator AnimateLabel(GameObject obj, RectTransform rect, CanvasGroup cg)
    {
        Vector2 startPos = rect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < _lifetime)
        {
            if (obj == null)
                yield break;

            elapsed += Time.unscaledDeltaTime;

            // ── Scale animation ──
            float scale;
            if (elapsed <= _popDuration)
            {
                float t = elapsed / _popDuration;
                scale = Mathf.Lerp(_startScale, _popScale, EaseOutBack(t));
            }
            else
            {
                float t = Mathf.InverseLerp(_popDuration, _lifetime, elapsed);
                scale = Mathf.Lerp(_popScale, 1f, EaseOutQuad(t));
            }
            rect.localScale = Vector3.one * scale;

            // ── Float up ──
            float moveT = Mathf.Clamp01(elapsed / _lifetime);
            rect.anchoredPosition = startPos + new Vector2(0f, _floatUpDistance * moveT);

            // ── Fade out ──
            if (elapsed >= _fadeStartTime)
            {
                float fadeT = Mathf.InverseLerp(_fadeStartTime, _lifetime, elapsed);
                cg.alpha = Mathf.Lerp(1f, 0f, EaseOutQuad(fadeT));
            }

            yield return null;
        }

        if (obj != null)
            Destroy(obj);
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private void ResolveReferences()
    {
        if (_noteManager == null)
            _noteManager = FindFirstObjectByType<NoteManager>();

        if (_targetCanvas == null)
            _targetCanvas = FindFirstObjectByType<Canvas>();
    }

    // Easing — đồng nhất với HitEffectSpriteReceiver
    private static float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private static float EaseOutQuad(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t);
    }
}
