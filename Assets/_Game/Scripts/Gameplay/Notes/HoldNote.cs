using UnityEngine;
using UnityEngine.UI;

public class HoldNote : NoteBase
{
    [Header("Hold Visual")]
    [SerializeField] private Image holdFillImage;
    [SerializeField] private RectTransform holdFillRect;

    private readonly HoldNoteStateMachine stateMachine = new HoldNoteStateMachine();

    private bool visualCreated;

    protected override void Awake()
    {
        base.Awake();
        noteType = NoteType.Hold;
    }

    public override void Initialize(NoteRuntimeData data)
    {
        base.Initialize(data);

        CreateHoldVisualIfNeeded();
        SetHoldProgress(0f);

        SetColor(Color.white);
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
        SetHoldProgress(stateMachine.Progress01);
    }

    public override void Tick(float currentTime)
    {
        base.Tick(currentTime);

        if (IsFinished)
            return;

        stateMachine.Tick(currentTime);

        if (stateMachine.IsHolding())
        {
            SetColor(Color.yellow);
            SetHoldProgress(stateMachine.Progress01);
        }

        if (stateMachine.IsCompleted())
        {
            SetHoldProgress(1f);
            SetHoldFillColor(Color.green);
            Complete(NoteResult.Completed);
        }
    }

    public override void OnPointerEnd(NotePointer pointer)
    {
        float currentTime = owner != null ? owner.CurrentTime : 0f;

        stateMachine.Release(pointer.fingerId, currentTime);

        if (stateMachine.IsReleasedEarly())
        {
            SetHoldFillColor(Color.red);
            Fail(NoteResult.ReleasedEarly);
        }

        if (stateMachine.IsCompleted())
        {
            SetHoldProgress(1f);
            SetHoldFillColor(Color.green);
            Complete(NoteResult.Completed);
        }
    }

    public override float GetAutoMissTime(float missAfterHitTime)
    {
        // Quan trọng:
        // Nếu người chơi không chạm đầu Hold lúc nó tới hitline,
        // nó phải miss sớm giống Tap Note.
        // Không được chờ tới hitTime + duration.
        return hitTime + missAfterHitTime;
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

        holdFillRect.sizeDelta = new Vector2(70f, 0f);
        holdFillRect.anchoredPosition = Vector2.zero;

        holdFillImage = fillObject.GetComponent<Image>();
        holdFillImage.color = Color.yellow;
        holdFillImage.raycastTarget = false;
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
}