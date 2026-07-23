using UnityEngine;
using UnityEngine.UI;

public class HoldNote : NoteBase
{
    [Header("Hold Visual")]
    [SerializeField] private Image holdFillImage;
    [SerializeField] private RectTransform holdFillRect;
    [SerializeField, Min(0.03f)] private float sustainEffectInterval = 0.12f;

    [Header("Early Release Judgment")]
    [Tooltip("Nếu bật, thả Hold hơi sớm vẫn có thể được tính Perfect/Great/Good thay vì luôn Miss.")]
    [SerializeField] private bool judgeEarlyRelease = true;
    [SerializeField, Range(0f, 1f)] private float minPerfectHoldProgress = 0.98f;
    [SerializeField, Range(0f, 1f)] private float minGreatHoldProgress = 0.90f;
    [SerializeField, Range(0f, 1f)] private float minGoodHoldProgress = 0.75f;

    private readonly HoldNoteStateMachine stateMachine = new HoldNoteStateMachine();

    private bool visualCreated;
    private bool judgmentEffectShown;
    private float baseVisualWidth;
    private float baseVisualHeight;
    private float runtimeHitlineY;
    private float runtimeScrollSpeed;
    private float runtimeLaneSpacing;
    private float nextSustainEffectTime;
    private Image skinHeadImage;
    private Image skinBodyImage;
    private Image skinTailImage;
    private bool usesSegmentedSkin;

    protected override void Awake()
    {
        base.Awake();
        noteType = NoteType.Hold;
    }

    public override void Initialize(NoteRuntimeData data)
    {
        base.Initialize(data);

        stateMachine.Reset();
        judgmentEffectShown = false;
        runtimeHitlineY = data.hitlineY;
        runtimeScrollSpeed = data.scrollSpeed;
        runtimeLaneSpacing = data.laneSpacing;
        nextSustainEffectTime = 0f;

        CacheBaseVisualSize();
        ApplyDurationVisual(data.scrollSpeed);
        CreateHoldVisualIfNeeded();
        SetHoldProgress(0f);
        SetHoldFillColor(Color.yellow);

        SetColor(Color.white);
        NoteSkinService.ApplyTo(this);
        UpdateSegmentedSkinLayout();
    }

    public override void ApplyScrollSpeed(float newScrollSpeed)
    {
        runtimeScrollSpeed = newScrollSpeed;
        base.ApplyScrollSpeed(newScrollSpeed);

        if (stateMachine.IsHolding())
            ApplyRemainingDurationVisual(owner != null ? owner.CurrentTime : hitTime);
        else
            ApplyDurationVisual(newScrollSpeed);

        SetHoldProgress(stateMachine.IsHolding() ? 1f : stateMachine.Progress01);
        UpdateSegmentedSkinLayout();
    }

    public override void OnPointerBegin(NotePointer pointer)
    {
        float currentTime = owner != null ? owner.CurrentTime : 0f;

        stateMachine.StartHold(
            pointer.fingerId,
            hitTime,
            duration,
            currentTime
        );

        SetColor(Color.yellow);
        SetHoldFillColor(Color.yellow);
        nextSustainEffectTime = currentTime;

        if (movement != null)
            movement.LockY(runtimeHitlineY);

        ApplyRemainingDurationVisual(currentTime);
        SetHoldProgress(1f);
        owner?.NotifySustainEffect(this);
    }

    public override void Tick(float currentTime)
    {
        base.Tick(currentTime);

        if (IsFinished)
            return;

        stateMachine.Tick(currentTime);

        if (stateMachine.IsHolding())
        {
            if (movement != null)
                movement.LockY(runtimeHitlineY);

            SetColor(Color.yellow);
            SetHoldFillColor(Color.yellow);
            ApplyRemainingDurationVisual(currentTime);
            SetHoldProgress(1f);
            NotifySustainEffectIfDue(currentTime);
            return;
        }

        if (stateMachine.IsCompleted())
        {
            CompleteHold();
        }
    }

    public override void OnPointerEnd(NotePointer pointer)
    {
        if (IsFinished)
            return;

        float currentTime = owner != null ? owner.CurrentTime : 0f;

        stateMachine.Release(pointer.fingerId, currentTime);

        if (stateMachine.IsReleasedEarly())
        {
            if (movement != null)
                movement.UnlockY();

            HitJudgment releaseJudgment = JudgeReleaseProgress(stateMachine.Progress01);
            HitJudgment finalJudgment = GetLowerJudgment(LastJudgment, releaseJudgment);
            SetJudgment(finalJudgment, (currentTime - (hitTime + duration)) * 1000f);

            if (finalJudgment == HitJudgment.Miss)
            {
                SetHoldFillColor(Color.red);
                SetColor(Color.red);
                Fail(NoteResult.ReleasedEarly);
                return;
            }

            SetHoldProgress(stateMachine.Progress01);
            SetHoldFillColor(GetJudgmentColor(finalJudgment));
            SetColor(GetJudgmentColor(finalJudgment));
            ShowJudgmentEffectOnce();
            Complete(NoteResult.Completed);
            return;
        }

        if (stateMachine.IsCompleted())
        {
            CompleteHold();
        }
    }

    public override float GetAutoMissTime(float missAfterHitTime)
    {
        // Nếu người chơi không chạm đầu Hold lúc nó tới hitline,
        // nó miss sớm giống Tap Note.
        return hitTime + missAfterHitTime;
    }

    public override Vector3 GetHitEffectWorldPosition()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        float holdHeight = rectTransform.rect.height;

        // Hold Note đang dùng pivot ở đáy:
        // đáy = đầu Hold
        // đỉnh = đuôi Hold
        // nên effect lấy vị trí đuôi Hold.
        return rectTransform.TransformPoint(new Vector3(0f, holdHeight, 0f));
    }

    private void CompleteHold()
    {
        if (IsFinished)
            return;

        SetHoldProgress(1f);
        SetHoldFillColor(Color.green);
        SetColor(Color.green);

        if (movement != null)
            movement.UnlockY();

        ShowJudgmentEffectOnce();

        Complete(NoteResult.Completed);
    }

    private HitJudgment JudgeReleaseProgress(float progress01)
    {
        if (!judgeEarlyRelease)
            return HitJudgment.Miss;

        progress01 = Mathf.Clamp01(progress01);
        float perfect = Mathf.Clamp01(minPerfectHoldProgress);
        float great = Mathf.Min(Mathf.Clamp01(minGreatHoldProgress), perfect);
        float good = Mathf.Min(Mathf.Clamp01(minGoodHoldProgress), great);

        if (progress01 >= perfect)
            return HitJudgment.Perfect;

        if (progress01 >= great)
            return HitJudgment.Great;

        if (progress01 >= good)
            return HitJudgment.Good;

        return HitJudgment.Miss;
    }

    private static HitJudgment GetLowerJudgment(HitJudgment first, HitJudgment second)
    {
        if (first == HitJudgment.None)
            return second;

        return GetJudgmentRank(first) >= GetJudgmentRank(second) ? first : second;
    }

    private static int GetJudgmentRank(HitJudgment judgment)
    {
        return judgment switch
        {
            HitJudgment.Perfect => 0,
            HitJudgment.Great => 1,
            HitJudgment.Good => 2,
            HitJudgment.Miss => 3,
            _ => 3
        };
    }

    private static Color GetJudgmentColor(HitJudgment judgment)
    {
        return judgment switch
        {
            HitJudgment.Perfect => Color.green,
            HitJudgment.Great => new Color(0.25f, 0.7f, 1f),
            HitJudgment.Good => Color.yellow,
            _ => Color.red
        };
    }

    private void ShowJudgmentEffectOnce()
    {
        if (judgmentEffectShown)
            return;

        judgmentEffectShown = true;

        owner?.NotifyJudgmentEffect(this);
    }

    private void CreateHoldVisualIfNeeded()
    {
        if (visualCreated)
            return;

        visualCreated = true;

        GameObject fillObject = new GameObject(
            "HOLD_PROGRESS_FILL",
            typeof(RectTransform),
            typeof(Image)
        );

        fillObject.transform.SetParent(transform, false);

        holdFillRect = fillObject.GetComponent<RectTransform>();

        holdFillRect.anchorMin = new Vector2(0.5f, 0f);
        holdFillRect.anchorMax = new Vector2(0.5f, 0f);
        holdFillRect.pivot = new Vector2(0.5f, 0f);

        float fillWidth = rectTransform != null && rectTransform.sizeDelta.x > 0f
            ? rectTransform.sizeDelta.x * 0.75f
            : 70f;

        holdFillRect.sizeDelta = new Vector2(fillWidth, 0f);
        holdFillRect.anchoredPosition = Vector2.zero;

        holdFillImage = fillObject.GetComponent<Image>();
        if (noteImage != null)
            holdFillImage.sprite = noteImage.sprite;

        holdFillImage.type = Image.Type.Simple;
        holdFillImage.preserveAspect = false;
        holdFillImage.color = Color.yellow;
        holdFillImage.raycastTarget = false;
    }

    private void ApplyDurationVisual(float scrollSpeed)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
            return;

        if (baseVisualWidth <= 0f || baseVisualHeight <= 0f)
            CacheBaseVisualSize();

        float durationHeight = Mathf.Max(0f, duration) * Mathf.Max(0f, scrollSpeed);

        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.sizeDelta = new Vector2(baseVisualWidth, baseVisualHeight + durationHeight);
        UpdateSegmentedSkinLayout();
    }

    private void ApplyRemainingDurationVisual(float currentTime)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
            return;

        if (baseVisualWidth <= 0f || baseVisualHeight <= 0f)
            CacheBaseVisualSize();

        float tailTime = hitTime + Mathf.Max(0f, duration);
        float remainingSeconds = Mathf.Max(0f, tailTime - currentTime);
        float remainingHeight = remainingSeconds * Mathf.Max(0f, runtimeScrollSpeed);

        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, runtimeHitlineY);
        rectTransform.sizeDelta = new Vector2(baseVisualWidth, baseVisualHeight + remainingHeight);
        UpdateSegmentedSkinLayout();
    }

    private void CacheBaseVisualSize()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
            return;

        baseVisualWidth = rectTransform.sizeDelta.x;
        baseVisualHeight = rectTransform.sizeDelta.y;
    }

    private void SetHoldProgress(float progress01)
    {
        if (holdFillRect == null)
            return;

        progress01 = Mathf.Clamp01(progress01);

        float maxHeight = rectTransform.rect.height;

        Vector2 size = holdFillRect.sizeDelta;
        size.y = maxHeight * progress01;
        holdFillRect.sizeDelta = size;
    }

    private void SetHoldFillColor(Color color)
    {
        if (holdFillImage != null)
            holdFillImage.color = color;
    }

    private void NotifySustainEffectIfDue(float currentTime)
    {
        if (owner == null || currentTime < nextSustainEffectTime)
            return;

        nextSustainEffectTime = currentTime + sustainEffectInterval;
        owner.NotifySustainEffect(this);
    }

    /// <summary>Uses osu!mania's separate hold head/body/tail without stretching the head sprite.</summary>
    public void ApplySkinSprites(Sprite head, Sprite body, Sprite tail)
    {
        if (head == null || body == null || tail == null)
            return;

        CreateSegmentedSkinVisuals();
        usesSegmentedSkin = true;
        if (runtimeLaneSpacing > 0f)
        {
            baseVisualWidth = Mathf.Max(54f, runtimeLaneSpacing * 0.66f);
            baseVisualHeight = baseVisualWidth;
            ApplyDurationVisual(runtimeScrollSpeed);
        }
        if (noteImage != null)
            noteImage.enabled = false;
        if (holdFillImage != null)
            holdFillImage.enabled = false;

        skinHeadImage.sprite = head;
        skinBodyImage.sprite = body;
        skinTailImage.sprite = tail;
        skinHeadImage.color = skinBodyImage.color = skinTailImage.color = Color.white;
        UpdateSegmentedSkinLayout();
    }

    private void CreateSegmentedSkinVisuals()
    {
        if (skinHeadImage != null)
            return;

        skinHeadImage = CreateSegment("SKIN_HOLD_HEAD", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        skinBodyImage = CreateSegment("SKIN_HOLD_BODY", Vector2.zero, Vector2.one, new Vector2(0.5f, 0f));
        skinTailImage = CreateSegment("SKIN_HOLD_TAIL", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
    }

    private Image CreateSegment(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        Image image = go.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = false;
        return image;
    }

    private void UpdateSegmentedSkinLayout()
    {
        if (!usesSegmentedSkin || rectTransform == null || skinHeadImage == null)
            return;

        float width = Mathf.Max(1f, rectTransform.rect.width);
        float capHeight = Mathf.Min(Mathf.Max(1f, baseVisualHeight), rectTransform.rect.height * 0.5f);
        RectTransform head = skinHeadImage.rectTransform;
        RectTransform body = skinBodyImage.rectTransform;
        RectTransform tail = skinTailImage.rectTransform;

        head.sizeDelta = new Vector2(width, capHeight);
        head.anchoredPosition = Vector2.zero;
        tail.sizeDelta = new Vector2(width, capHeight);
        tail.anchoredPosition = Vector2.zero;
        body.offsetMin = new Vector2(0f, capHeight);
        body.offsetMax = new Vector2(0f, -capHeight);
    }
}
