using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class HitEffectSpriteReceiver : MonoBehaviour, INoteResultReceiver
{
    public static HitEffectSpriteReceiver ActiveReceiver { get; private set; }

    [Header("References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private NoteManager noteManager;
    [SerializeField] private GameplayLaneLayout laneLayout;

    [Header("Judgment Sprites")]
    [SerializeField] private Sprite perfectSprite;
    [SerializeField] private Sprite greatSprite;
    [SerializeField] private Sprite goodSprite;
    [SerializeField] private Sprite missSprite;

    [Header("Center Position")]
    [SerializeField] private Vector2 centerOffset = new Vector2(0f, 80f);

    [Header("Visual")]
    [SerializeField] private Vector2 effectSize = new Vector2(180f, 64f);
    [SerializeField] private float effectLifetime = 0.48f;

    [Header("Layered Effect")]
    [SerializeField] private bool allowStackedEffects = true;
    [SerializeField] private int maxVisibleEffects = 5;
    [SerializeField] private float stackedOffsetRadius = 28f;
    [SerializeField] private float stackedScaleStep = 0.04f;
    [SerializeField] private float stackedRotationRange = 4f;

    [Header("Lane Flash")]
    [SerializeField] private Sprite[] laneFlashSprites = new Sprite[0];
    [SerializeField] private Vector2 laneFlashSize = new Vector2(190f, 150f);
    [SerializeField] private float laneFlashFrameTime = 0.025f;
    [SerializeField] private float laneFlashYOffset = 42f;
    [SerializeField] private bool useLaneLayoutSize = true;

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
    private bool isSubscribed;
    public bool IsSubscribed => isSubscribed;

    private void Awake()
    {
        RestoreMissingSprites();

        if (targetCanvas == null)
            targetCanvas = FindFirstObjectByType<Canvas>();

        if (noteManager == null)
            noteManager = FindFirstObjectByType<NoteManager>();

        if (laneLayout == null)
            laneLayout = FindFirstObjectByType<GameplayLaneLayout>();

        if (targetCanvas != null)
            canvasRect = targetCanvas.GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        ActiveReceiver = this;
        EnsureSubscribed();
    }

    private void Start()
    {
        EnsureReferences();
        EnsureSubscribed();
    }

    private void OnDisable()
    {
        if (ActiveReceiver == this)
            ActiveReceiver = null;

        if (noteManager != null && isSubscribed)
        {
            noteManager.OnNoteJudgedEvent -= HandleNoteJudged;
            noteManager.OnNoteFinishedEvent -= HandleNoteFinished;
            noteManager.OnNoteSustainEvent -= HandleNoteSustain;
            isSubscribed = false;
        }
    }

    private void EnsureReferences()
    {
        if (targetCanvas == null)
            targetCanvas = FindFirstObjectByType<Canvas>();

        if (noteManager == null)
            noteManager = FindFirstObjectByType<NoteManager>();

        if (laneLayout == null)
            laneLayout = FindFirstObjectByType<GameplayLaneLayout>();

        if (targetCanvas != null && canvasRect == null)
            canvasRect = targetCanvas.GetComponent<RectTransform>();
    }

    private void EnsureSubscribed()
    {
        if (isSubscribed)
            return;

        EnsureReferences();

        if (noteManager == null)
            return;

        noteManager.OnNoteJudgedEvent += HandleNoteJudged;
        noteManager.OnNoteFinishedEvent += HandleNoteFinished;
        noteManager.OnNoteSustainEvent += HandleNoteSustain;
        isSubscribed = true;
    }

    private void HandleNoteJudged(NoteBase note, HitJudgment judgment, float deltaMs)
    {
        if (note == null)
            return;

        if (judgment == HitJudgment.None || judgment == HitJudgment.Miss)
            return;

        SpawnCenterEffect(judgment);
        SpawnLaneFlash(note);
    }

    public void ShowJudgmentEffect(NoteBase note, HitJudgment judgment)
    {
        HandleNoteJudged(note, judgment, note != null ? note.LastDeltaMs : 0f);
    }

    public void ShowMissEffect(NoteBase note)
    {
        HandleNoteFinished(note, NoteResult.Missed);
    }

    public void OnNoteFinished(NoteBase note, NoteResult result)
    {
        HandleNoteFinished(note, result);
    }

    private void HandleNoteFinished(NoteBase note, NoteResult result)
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

    private void HandleNoteSustain(NoteBase note)
    {
        SpawnLaneFlash(note);
    }

    private void SpawnLaneFlash(NoteBase note)
    {
        if (targetCanvas == null || canvasRect == null)
            return;

        if (note == null || laneFlashSprites == null || laneFlashSprites.Length == 0)
            return;

        float laneX = laneLayout != null
            ? laneLayout.GetLaneAnchoredX(note.LaneIndex)
            : 0f;

        float hitlineY = laneLayout != null
            ? laneLayout.AppliedHitlineY
            : -330f;

        Vector2 size = laneFlashSize;
        if (useLaneLayoutSize && laneLayout != null)
        {
            float laneWidth = Mathf.Max(1f, laneLayout.AppliedLaneSpacing);
            size = new Vector2(laneWidth * 1.18f, laneWidth * 0.82f);
        }

        GameObject flashObject = new GameObject(
            $"LANE_HIT_FLASH_L{note.LaneIndex}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        flashObject.transform.SetParent(targetCanvas.transform, false);
        flashObject.transform.SetAsLastSibling();

        RectTransform rect = flashObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(laneX, hitlineY + laneFlashYOffset);

        Image image = flashObject.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;

        StartCoroutine(AnimateLaneFlash(flashObject, image));
    }

    private IEnumerator AnimateLaneFlash(GameObject flashObject, Image image)
    {
        for (int i = 0; i < laneFlashSprites.Length; i++)
        {
            if (flashObject == null || image == null)
                yield break;

            image.sprite = laneFlashSprites[i];
            yield return new WaitForSecondsRealtime(laneFlashFrameTime);
        }

        if (flashObject != null)
            Destroy(flashObject);
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

    private void RestoreMissingSprites()
    {
#if UNITY_EDITOR
        if (laneFlashSprites == null || laneFlashSprites.Length == 0)
            laneFlashSprites = LoadLightingSprites();

        if (perfectSprite == null)
            perfectSprite = LoadSprite(
                "Assets/_Game/Sprites/GamePlay/PerfectEF/mania-hit300@2x.png",
                "Assets/_Game/Sprites/GamePlay/PerfectEF/mania-hit300.png"
            );
        if (greatSprite == null)
            greatSprite = LoadSprite(
                "Assets/_Game/Sprites/GamePlay/PerfectEF/mania-hit200@2x.png",
                "Assets/_Game/Sprites/GamePlay/PerfectEF/mania-hit200.png"
            );
        if (goodSprite == null)
            goodSprite = LoadSprite(
                "Assets/_Game/Sprites/GamePlay/PerfectEF/mania-hit100@2x.png",
                "Assets/_Game/Sprites/GamePlay/PerfectEF/mania-hit100.png"
            );
        if (missSprite == null)
            missSprite = LoadSprite(
                "Assets/_Game/Sprites/GamePlay/PerfectEF/mania-hit0@2x.png",
                "Assets/_Game/Sprites/GamePlay/PerfectEF/mania-hit0.png"
            );
#endif
    }

#if UNITY_EDITOR
    private static Sprite[] LoadLightingSprites()
    {
        const int frameCount = 16;
        Sprite[] sprites = new Sprite[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            sprites[i] = LoadSprite(
                $"Assets/_Game/Sprites/GamePlay/HitEf/lightingN-{i}@2x.png",
                $"Assets/_Game/Sprites/GamePlay/HitEf/lightingN-{i}.png"
            );
        }

        return sprites;
    }

    private static Sprite LoadSprite(params string[] paths)
    {
        foreach (string path in paths)
        {
            Sprite sprite = LoadSpriteAtPath(path);
            if (sprite != null)
                return sprite;
        }

        return null;
    }

    private static Sprite LoadSpriteAtPath(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
            return sprite;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (Object asset in assets)
        {
            if (asset is Sprite nestedSprite)
                return nestedSprite;
        }

        return null;
    }
#endif
}
