using TMPro;
using UnityEngine;

/// <summary>Scrolls a long TMP title inside its RectTransform, then resets after the full title has been shown.</summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class SongTitleMarquee : MonoBehaviour
{
    [SerializeField] private float startDelay = 0.75f;
    [SerializeField] private float endHold = 1.0f;
    [SerializeField] private float resetHold = 0.45f;
    [SerializeField] private float pixelsPerSecond = 28f;
    [SerializeField] private float extraPadding = 24f;

    private TextMeshProUGUI _label;
    private RectTransform _rect;
    private RectTransform _viewport;
    private Vector2 _startPosition;
    private float _timer;

    private void Awake()
    {
        _label = GetComponent<TextMeshProUGUI>();
        _rect = transform as RectTransform;
        _viewport = transform.parent as RectTransform;
        _startPosition = _rect != null ? _rect.anchoredPosition : Vector2.zero;
        ConfigureText();
    }

    private void OnEnable()
    {
        ResetScroll();
        ConfigureText();
    }

    private void OnRectTransformDimensionsChange()
    {
        ResetScroll();
    }

    private void Update()
    {
        if (_label == null || _rect == null || string.IsNullOrEmpty(_label.text))
            return;

        _label.ForceMeshUpdate();
        float viewportWidth = _viewport != null ? _viewport.rect.width : _rect.rect.width;
        float textWidth = _label.preferredWidth;
        float overflow = textWidth - viewportWidth;

        if (overflow <= 1f)
        {
            _rect.anchoredPosition = _startPosition;
            return;
        }

        float scrollDistance = overflow + extraPadding;
        float scrollDuration = Mathf.Max(0.1f, scrollDistance / Mathf.Max(1f, pixelsPerSecond));
        float cycleDuration = startDelay + scrollDuration + endHold + resetHold;
        _timer = Mathf.Repeat(_timer + Time.unscaledDeltaTime, cycleDuration);

        float x = _startPosition.x;
        if (_timer > startDelay)
        {
            float scrollTime = Mathf.Min(_timer - startDelay, scrollDuration);
            x = _startPosition.x - scrollTime / scrollDuration * scrollDistance;
        }

        if (_timer > startDelay + scrollDuration + endHold)
            x = _startPosition.x;

        _rect.anchoredPosition = new Vector2(x, _startPosition.y);
    }

    public void ResetScroll()
    {
        _timer = 0f;
        if (_rect != null)
        {
            _startPosition = _rect.anchoredPosition;
            _rect.anchoredPosition = _startPosition;
        }
    }

    private void ConfigureText()
    {
        if (_label == null)
            return;

        _label.textWrappingMode = TextWrappingModes.NoWrap;
        _label.overflowMode = TextOverflowModes.Overflow;
    }
}
