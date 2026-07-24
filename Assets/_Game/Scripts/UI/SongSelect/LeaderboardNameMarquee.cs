using TMPro;
using UnityEngine;

/// <summary>
/// Reveals an overflowing leaderboard username without changing its score/rank columns.
/// Long names hold at each end for three seconds and move at a constant speed.
/// </summary>
public sealed class LeaderboardNameMarquee : MonoBehaviour
{
    private const float START_HOLD_SECONDS = 3f;
    private const float END_HOLD_SECONDS = 3f;
    private const float RESET_HOLD_SECONDS = 0.15f;
    private const float PIXELS_PER_SECOND = 26f;

    private RectTransform _viewport;
    private TextMeshProUGUI _label;
    private RectTransform _labelRect;
    private float _timer;
    private Vector2 _startPosition;

    public void Configure(RectTransform viewport, TextMeshProUGUI label)
    {
        _viewport = viewport;
        _label = label;
        _labelRect = label != null ? label.rectTransform : null;
        ResetScroll();

        if (_label != null)
        {
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.overflowMode = TextOverflowModes.Overflow;
        }
    }

    private void OnEnable() => ResetScroll();

    private void Update()
    {
        if (_viewport == null || _label == null || _labelRect == null)
            return;

        _label.ForceMeshUpdate();
        float overflow = _label.preferredWidth - _viewport.rect.width;
        if (overflow <= 1f)
        {
            _labelRect.anchoredPosition = _startPosition;
            return;
        }

        float scrollDuration = overflow / PIXELS_PER_SECOND;
        float cycleDuration = START_HOLD_SECONDS + scrollDuration + END_HOLD_SECONDS + RESET_HOLD_SECONDS;
        _timer = Mathf.Repeat(_timer + Time.unscaledDeltaTime, cycleDuration);

        float x = _startPosition.x;
        if (_timer >= START_HOLD_SECONDS && _timer < START_HOLD_SECONDS + scrollDuration)
        {
            float t = (_timer - START_HOLD_SECONDS) / scrollDuration;
            x -= overflow * t;
        }
        else if (_timer >= START_HOLD_SECONDS + scrollDuration && _timer < START_HOLD_SECONDS + scrollDuration + END_HOLD_SECONDS)
        {
            x -= overflow;
        }

        _labelRect.anchoredPosition = new Vector2(x, _startPosition.y);
    }

    private void ResetScroll()
    {
        _timer = 0f;
        if (_labelRect == null)
            _labelRect = GetComponent<RectTransform>();
        if (_labelRect == null)
            return;

        _startPosition = _labelRect.anchoredPosition;
        _labelRect.anchoredPosition = _startPosition;
    }
}
