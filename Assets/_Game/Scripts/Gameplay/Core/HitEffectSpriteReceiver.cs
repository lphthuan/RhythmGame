using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HitEffectSpriteReceiver : MonoBehaviour, INoteResultReceiver
{
    [Header("References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private NoteManager noteManager;

    [Header("Judgment Sprites")]
    [SerializeField] private Sprite perfectSprite;
    [SerializeField] private Sprite greatSprite;
    [SerializeField] private Sprite goodSprite;
    [SerializeField] private Sprite missSprite;

    [Header("Center Position")]
    [SerializeField] private Vector2 centerOffset = new Vector2(0f, 80f);

    [Header("Visual")]
    [SerializeField] private Vector2 effectSize = new Vector2(280f, 100f);
    [SerializeField] private float effectLifetime = 0.48f;

    [Header("Layered Effect")]
    [SerializeField] private bool allowStackedEffects = true;
    [SerializeField] private int maxVisibleEffects = 5;
    [SerializeField] private float stackedOffsetRadius = 28f;
    [SerializeField] private float stackedScaleStep = 0.04f;
    [SerializeField] private float stackedRotationRange = 4f;

    [Header("Animation")]
    [SerializeField] private float startScale = 0.65f;
    [SerializeField] private float popScale = 1.18f;
    [SerializeField] private float settleScale = 1.0f;
    [SerializeField] private float popTime = 0.08f;
    [SerializeField] private float fadeStartTime = 0.20f;
    [SerializeField] private float floatUpDistance = 22f;

    private RectTransform canvasRect;

    private int activeEffectCount;
    private int stackSerial;

    private void Awake()
    {
        if (targetCanvas == null)
            targetCanvas = FindFirstObjectByType<Canvas>();

        if (noteManager == null)
            noteManager = FindFirstObjectByType<NoteManager>();

        if (targetCanvas != null)
            canvasRect = targetCanvas.GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (noteManager != null)
        {
            noteManager.OnNoteJudgedEvent += HandleNoteJudged;
        }
    }

    private void OnDisable()
    {
        if (noteManager != null)
        {
            noteManager.OnNoteJudgedEvent -= HandleNoteJudged;
        }
    }

    private void HandleNoteJudged(NoteBase note, HitJudgment judgment, float deltaMs)
    {
        if (note == null)
            return;

        if (judgment == HitJudgment.None || judgment == HitJudgment.Miss)
            return;

        SpawnCenterEffect(judgment);
    }

    public void OnNoteFinished(NoteBase note, NoteResult result)
    {
        if (note == null)
            return;

        if (result == NoteResult.Missed ||
            result == NoteResult.Failed ||
            result == NoteResult.ReleasedEarly)
        {
            SpawnCenterEffect(HitJudgment.Miss);
        }
    }

    private void SpawnCenterEffect(HitJudgment judgment)
    {
        if (targetCanvas == null || canvasRect == null)
            return;

        Sprite sprite = GetSprite(judgment);

        if (sprite == null)
        {
            Debug.LogWarning($"No sprite assigned for judgment: {judgment}");
            return;
        }

        if (!allowStackedEffects && activeEffectCount > 0)
            return;

        if (activeEffectCount >= maxVisibleEffects)
            return;

        GameObject effectObject = new GameObject(
            $"CENTER_HIT_EFFECT_{judgment}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup)
        );

        effectObject.transform.SetParent(targetCanvas.transform, false);
        effectObject.transform.SetAsLastSibling();

        RectTransform rect = effectObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = effectSize;

        Vector2 layeredOffset = GetLayeredOffset();
        rect.anchoredPosition = centerOffset + layeredOffset;

        float layerScaleBonus = Mathf.Min(activeEffectCount, maxVisibleEffects - 1) * stackedScaleStep;
        rect.localScale = Vector3.one * (startScale + layerScaleBonus);

        float rotation = Random.Range(-stackedRotationRange, stackedRotationRange);
        rect.localRotation = Quaternion.Euler(0f, 0f, rotation);

        Image image = effectObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        CanvasGroup canvasGroup = effectObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        activeEffectCount++;
        stackSerial++;

        StartCoroutine(AnimateAndDestroy(
            effectObject,
            rect,
            canvasGroup,
            centerOffset + layeredOffset,
            layerScaleBonus
        ));
    }

    private Vector2 GetLayeredOffset()
    {
        if (!allowStackedEffects)
            return Vector2.zero;

        int patternIndex = stackSerial % 8;

        switch (patternIndex)
        {
            case 0:
                return Vector2.zero;

            case 1:
                return new Vector2(stackedOffsetRadius, 0f);

            case 2:
                return new Vector2(-stackedOffsetRadius, 0f);

            case 3:
                return new Vector2(0f, stackedOffsetRadius * 0.65f);

            case 4:
                return new Vector2(0f, -stackedOffsetRadius * 0.65f);

            case 5:
                return new Vector2(stackedOffsetRadius * 0.7f, stackedOffsetRadius * 0.45f);

            case 6:
                return new Vector2(-stackedOffsetRadius * 0.7f, stackedOffsetRadius * 0.45f);

            default:
                return new Vector2(0f, 0f);
        }
    }

    private IEnumerator AnimateAndDestroy(
        GameObject effectObject,
        RectTransform rect,
        CanvasGroup canvasGroup,
        Vector2 startAnchoredPosition,
        float layerScaleBonus
    )
    {
        float elapsed = 0f;

        float finalStartScale = startScale + layerScaleBonus;
        float finalPopScale = popScale + layerScaleBonus;
        float finalSettleScale = settleScale + layerScaleBonus;

        while (elapsed < effectLifetime)
        {
            if (effectObject == null)
                yield break;

            elapsed += Time.unscaledDeltaTime;

            float scale;

            if (elapsed <= popTime)
            {
                float t = elapsed / popTime;
                scale = Mathf.Lerp(finalStartScale, finalPopScale, EaseOutBack(t));
            }
            else
            {
                float t = Mathf.InverseLerp(popTime, effectLifetime, elapsed);
                scale = Mathf.Lerp(finalPopScale, finalSettleScale, t);
            }

            rect.localScale = Vector3.one * scale;

            float moveT = Mathf.Clamp01(elapsed / effectLifetime);
            rect.anchoredPosition = startAnchoredPosition + new Vector2(0f, floatUpDistance * moveT);

            if (elapsed >= fadeStartTime)
            {
                float fadeT = Mathf.InverseLerp(fadeStartTime, effectLifetime, elapsed);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeT);
            }

            yield return null;
        }

        activeEffectCount = Mathf.Max(0, activeEffectCount - 1);

        Destroy(effectObject);
    }

    private float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t);

        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private Sprite GetSprite(HitJudgment judgment)
    {
        switch (judgment)
        {
            case HitJudgment.Perfect:
                return perfectSprite;

            case HitJudgment.Great:
                return greatSprite;

            case HitJudgment.Good:
                return goodSprite;

            case HitJudgment.Miss:
                return missSprite;

            default:
                return null;
        }
    }
}