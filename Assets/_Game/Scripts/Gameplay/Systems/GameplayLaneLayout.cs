using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class GameplayLaneLayout : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private ChartNoteSpawner chartNoteSpawner;
    [SerializeField] private NoteManager noteManager;

    [Header("Parts")]
    [SerializeField] private RectTransform stageShade;
    [SerializeField] private RectTransform stageLeft;
    [SerializeField] private RectTransform stageRight;
    [SerializeField] private RectTransform hitHint;
    [SerializeField] private RectTransform[] laneLights = new RectTransform[0];
    [SerializeField] private RectTransform[] laneBottoms = new RectTransform[0];

    [Header("Layout")]
    [SerializeField] private int laneCount = 4;
    [SerializeField, Range(0.12f, 0.8f)] private float laneAreaWidthRatio = 0.68f;
    [SerializeField] private float minLaneSpacing = 180f;
    [SerializeField] private float maxLaneSpacing = 290f;
    [SerializeField, Range(0.05f, 0.48f)] private float hitlineFromBottomRatio = 0.16f;
    [SerializeField] private float hitlineOffsetY = 0f;
    [SerializeField] private float hitlineJudgeDistanceRatio = 0.78f;
    [SerializeField] private float touchRadiusRatio = 1.08f;
    [SerializeField, Range(0.5f, 1.4f)] private float laneBottomWidthRatio = 1.08f;
    [SerializeField, Range(0.08f, 0.32f)] private float laneLightWidthRatio = 0.18f;
    [SerializeField, Range(0.75f, 1.25f)] private float hitHintWidthRatio = 1.04f;
    [SerializeField] private float verticalBleed = 96f;


    [Header("Debug")]
    [SerializeField] private float appliedLaneSpacing;
    [SerializeField] private float appliedHitlineY;
    [SerializeField] private float appliedTouchRadius;
    [SerializeField] private float appliedHitlineJudgeDistance;

    public float AppliedLaneSpacing => appliedLaneSpacing;
    public float AppliedHitlineY => appliedHitlineY;
    public float AppliedTouchRadius => appliedTouchRadius;
    public float AppliedHitlineJudgeDistance => appliedHitlineJudgeDistance;

    private RectTransform rectTransform;
    private Vector2 lastCanvasSize;

    public void Configure(
        Canvas targetCanvas,
        ChartNoteSpawner spawner,
        NoteManager manager,
        RectTransform shade,
        RectTransform left,
        RectTransform right,
        RectTransform hint,
        RectTransform[] lights,
        RectTransform[] bottoms)
    {
        canvas = targetCanvas;
        chartNoteSpawner = spawner;
        noteManager = manager;
        stageShade = shade;
        stageLeft = left;
        stageRight = right;
        hitHint = hint;
        laneLights = lights;
        laneBottoms = bottoms;

        ApplyLayout();
    }

    private void Awake()
    {
        ResolveReferences();
        ApplyLayout();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ApplyLayout();
    }

    private void Update()
    {
        if (canvas == null)
            ResolveReferences();

        Vector2 canvasSize = GetCanvasSize();
        if ((canvasSize - lastCanvasSize).sqrMagnitude > 0.5f)
            ApplyLayout();
    }

    private void OnValidate()
    {
        laneCount = Mathf.Max(1, laneCount);
        minLaneSpacing = Mathf.Max(1f, minLaneSpacing);
        maxLaneSpacing = Mathf.Max(minLaneSpacing, maxLaneSpacing);
        hitlineJudgeDistanceRatio = Mathf.Max(0f, hitlineJudgeDistanceRatio);
        touchRadiusRatio = Mathf.Max(0f, touchRadiusRatio);
        verticalBleed = Mathf.Max(0f, verticalBleed);

        ResolveReferences();
        ApplyLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyLayout();
    }

    public void ApplyLayout()
    {
        if (!isActiveAndEnabled)
            return;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        Vector2 canvasSize = GetCanvasSize();
        if (canvasSize.x <= 0f || canvasSize.y <= 0f)
            return;

        lastCanvasSize = canvasSize;

        Stretch(rectTransform);

        int activeLaneCount = Mathf.Max(1, laneCount);
        float laneAreaWidth = canvasSize.x * laneAreaWidthRatio;
        appliedLaneSpacing = Mathf.Clamp(
            laneAreaWidth / activeLaneCount,
            minLaneSpacing,
            maxLaneSpacing);

        appliedHitlineY = -canvasSize.y * 0.5f
            + canvasSize.y * hitlineFromBottomRatio
            + hitlineOffsetY;

        float firstLaneX = -((activeLaneCount - 1) * appliedLaneSpacing) * 0.5f;
        float laneBandWidth = activeLaneCount * appliedLaneSpacing;
        float stageHeight = canvasSize.y + verticalBleed * 2f;
        float leftSideWidth = Mathf.Clamp(appliedLaneSpacing * 0.28f, 36f, 62f);
        float rightSideWidth = leftSideWidth;
        float stageWidth = laneBandWidth + leftSideWidth + rightSideWidth;
        float leftSideX = laneBandWidth * 0.5f + leftSideWidth * 0.5f;
        float rightSideX = laneBandWidth * 0.5f + rightSideWidth * 0.5f;

        SetRect(stageShade, new Vector2(stageWidth, stageHeight), Vector2.zero);
        SetRect(stageLeft, new Vector2(leftSideWidth, stageHeight), new Vector2(-leftSideX, 0f));
        SetRect(stageRight, new Vector2(rightSideWidth, stageHeight), new Vector2(rightSideX, 0f));
        SetRect(hitHint, new Vector2(laneBandWidth * hitHintWidthRatio, Mathf.Max(22f, appliedLaneSpacing * 0.16f)),
            new Vector2(0f, appliedHitlineY + Mathf.Max(12f, appliedLaneSpacing * 0.12f)));

        for (int i = 0; i < activeLaneCount; i++)
        {
            float laneX = firstLaneX + i * appliedLaneSpacing;

            if (laneLights != null && i < laneLights.Length)
            {
                SetRect(laneLights[i],
                    new Vector2(Mathf.Max(18f, appliedLaneSpacing * laneLightWidthRatio), stageHeight),
                    new Vector2(laneX, 0f));
            }

            if (laneBottoms != null && i < laneBottoms.Length)
            {
                float targetSize = Mathf.Max(24f, appliedLaneSpacing * 0.18f);
                SetRect(laneBottoms[i],
                    new Vector2(appliedLaneSpacing * laneBottomWidthRatio, targetSize),
                    new Vector2(laneX, appliedHitlineY));
            }
        }

        appliedHitlineJudgeDistance = Mathf.Max(100f, appliedLaneSpacing * hitlineJudgeDistanceRatio);
        appliedTouchRadius = Mathf.Max(110f, appliedLaneSpacing * touchRadiusRatio);

        chartNoteSpawner?.ApplyGameplayLayout(appliedLaneSpacing, appliedHitlineY, appliedTouchRadius);
        noteManager?.ApplyHitlineLayout(appliedHitlineY, appliedHitlineJudgeDistance);
    }

    public float GetLaneAnchoredX(int laneIndex)
    {
        int activeLaneCount = Mathf.Max(1, laneCount);
        laneIndex = Mathf.Clamp(laneIndex, 0, activeLaneCount - 1);

        float firstLaneX = -((activeLaneCount - 1) * appliedLaneSpacing) * 0.5f;
        return firstLaneX + laneIndex * appliedLaneSpacing;
    }

    public Vector2 GetLaneHitScreenPosition(int laneIndex)
    {
        if (canvas == null)
            ResolveReferences();

        if (canvas == null)
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.2f);

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null)
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.2f);

        Vector2 localPosition = new Vector2(GetLaneAnchoredX(laneIndex), appliedHitlineY);
        Vector3 worldPosition = canvasRect.TransformPoint(localPosition);
        Camera targetCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        return RectTransformUtility.WorldToScreenPoint(targetCamera, worldPosition);
    }

    public bool TryGetLaneIndexFromScreenPosition(
        Vector2 screenPosition,
        float laneWidthRatio,
        out int laneIndex,
        out float distanceToLaneCenter)
    {
        laneIndex = -1;
        distanceToLaneCenter = float.MaxValue;

        if (canvas == null)
            ResolveReferences();

        if (canvas == null || appliedLaneSpacing <= 0f)
            return false;

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null)
            return false;

        Camera targetCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                targetCamera,
                out Vector2 localPosition))
        {
            return false;
        }

        int activeLaneCount = Mathf.Max(1, laneCount);
        float nearestDistance = float.MaxValue;
        int nearestLane = -1;

        for (int i = 0; i < activeLaneCount; i++)
        {
            float distance = Mathf.Abs(localPosition.x - GetLaneAnchoredX(i));
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestLane = i;
            }
        }

        float halfLaneWidth = appliedLaneSpacing * Mathf.Clamp01(laneWidthRatio) * 0.5f;
        if (nearestLane < 0 || nearestDistance > halfLaneWidth)
            return false;

        laneIndex = nearestLane;
        distanceToLaneCenter = nearestDistance;
        return true;
    }

    public void ApplySkinSprites(Sprite left, Sprite right, Sprite hint, Sprite bottom, Sprite light)
    {
        ApplySprite(stageLeft, left);
        ApplySprite(stageRight, right);
        ApplySprite(hitHint, hint);

        foreach (RectTransform laneBottom in laneBottoms)
            ApplySprite(laneBottom, bottom);
        // mania-stage-light is a transient glow, not a repeating lane texture.
        // Stretching it through every lane produced the four opaque bars seen
        // in gameplay, so lane lights deliberately keep their native effect.
        ApplyLayout();
    }

    private static void ApplySprite(RectTransform target, Sprite sprite)
    {
        if (target == null || sprite == null)
            return;

        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = sprite;
            image.preserveAspect = false;
            image.color = Color.white;
        }
    }

    private void ResolveReferences()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (chartNoteSpawner == null)
            chartNoteSpawner = FindFirstObjectByType<ChartNoteSpawner>();

        if (noteManager == null)
            noteManager = FindFirstObjectByType<NoteManager>();
    }

    private Vector2 GetCanvasSize()
    {
        if (canvas == null)
            return Vector2.zero;

        RectTransform canvasRect = canvas.transform as RectTransform;
        return canvasRect != null ? canvasRect.rect.size : Vector2.zero;
    }

    private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }
}
