using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public abstract class NoteBase : MonoBehaviour
{
    [Header("Base")]
    [SerializeField] protected NoteType noteType;
    [SerializeField] protected int noteId;
    [SerializeField] protected int laneIndex;
    [SerializeField] protected float hitTime;
    [SerializeField] protected float duration;

    [Header("Touch")]
    [SerializeField] protected float touchRadius = 120f;

    [Header("Visual")]
    [SerializeField] protected Image noteImage;
    [SerializeField] protected NoteVisualConfig visualConfig;

    [SerializeField] private HitJudgment lastJudgment = HitJudgment.None;
    [SerializeField] private float lastDeltaMs;

    protected NoteManager owner;
    protected NoteMovement movement;
    protected RectTransform rectTransform;

    private bool completed;
    private bool failed;
    private bool assigned;
    private int assignedFingerId = -1;

    public NoteType NoteType => noteType;
    public int NoteId => noteId;
    public int LaneIndex => laneIndex;
    public float HitTime => hitTime;
    public float Duration => duration;
    public float TouchRadius => touchRadius;

    public bool IsFinished => completed || failed;
    public bool IsAssigned => assigned;
    public HitJudgment LastJudgment => lastJudgment;
    public float LastDeltaMs => lastDeltaMs;

    public Vector2 AnchoredPosition
    {
        get
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            return rectTransform.anchoredPosition;
        }
    }
    public virtual float GetAutoMissTime(float missAfterHitTime)
    {
        return hitTime + missAfterHitTime;
    }

    public void ForceMiss(float deltaMs = 0f)
    {
        SetJudgment(HitJudgment.Miss, deltaMs);
        Fail(NoteResult.Missed);
    }

    protected virtual void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        movement = GetComponent<NoteMovement>();

        if (noteImage == null)
            noteImage = GetComponent<Image>();
    }

    public virtual void Initialize(NoteRuntimeData data)
    {
        if (visualConfig == null)
            visualConfig = data.visualConfig;

        noteId = data.noteId;
        laneIndex = data.laneIndex;
        noteType = data.noteType;
        hitTime = data.hitTime;
        duration = data.duration;
        touchRadius = data.touchRadius;
        lastJudgment = HitJudgment.None;
        lastDeltaMs = 0f;

        completed = false;
        failed = false;
        assigned = false;
        assignedFingerId = -1;

        if (movement == null)
            movement = GetComponent<NoteMovement>();

        if (movement != null)
            movement.Initialize(data);

        ResetVisual();

        // Hold creates its secondary visual objects after this base method.
        // Applying its skin here would dereference that not-yet-created state.
        if (this is not HoldNote)
            NoteSkinService.ApplyTo(this);
    }

    public void SetOwner(NoteManager manager)
    {
        owner = manager;
    }

    public virtual void Tick(float currentTime)
    {
        if (IsFinished)
            return;

        if (movement != null)
            movement.Tick(currentTime);
    }

    public bool CanReceivePointer()
    {
        return !completed && !failed && !assigned;
    }

    public void AssignFinger(int fingerId)
    {
        assigned = true;
        assignedFingerId = fingerId;
    }

    public void ReleaseFinger()
    {
        assigned = false;
        assignedFingerId = -1;
    }

    public bool IsAssignedToFinger(int fingerId)
    {
        return assigned && assignedFingerId == fingerId;
    }

    public virtual float DistanceToPointer(Vector2 screenPosition)
    {
        Vector2 noteScreenPosition = RectTransformUtility.WorldToScreenPoint(
            null,
            rectTransform.position
        );

        return Vector2.Distance(noteScreenPosition, screenPosition);
    }

    public virtual Vector3 GetHitEffectWorldPosition()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        return rectTransform.position;
    }

    public void SetJudgment(HitJudgment judgment, float deltaMs)
    {
        lastJudgment = judgment;
        lastDeltaMs = deltaMs;
    }

    public virtual void ApplyScrollSpeed(float newScrollSpeed)
    {
        if (movement == null)
            movement = GetComponent<NoteMovement>();

        if (movement != null)
            movement.ApplyScrollSpeed(newScrollSpeed);
    }

    public virtual void OnPointerBegin(NotePointer pointer) { }
    public virtual void OnPointerMove(NotePointer pointer) { }
    public virtual void OnPointerStationary(NotePointer pointer) { }
    public virtual void OnPointerEnd(NotePointer pointer) { }

    protected void Complete(NoteResult result = NoteResult.Completed)
    {
        if (IsFinished)
            return;

        completed = true;
        ReleaseFinger();

        SetColor(Color.green);

        if (owner != null)
            owner.NotifyNoteFinished(this, result);
    }

    protected void Fail(NoteResult result = NoteResult.Failed)
    {
        if (IsFinished)
            return;

        failed = true;
        ReleaseFinger();

        SetColor(Color.red);

        if (owner != null)
            owner.NotifyNoteFinished(this, result);
    }

    protected void SetColor(Color color)
    {
        if (noteImage != null)
            noteImage.color = color;
    }

    protected virtual void ResetVisual()
    {
        ApplyVisualStyle();
    }

    protected void ApplyVisualStyle()
    {
        if (noteImage == null)
            noteImage = GetComponent<Image>();

        if (visualConfig == null || noteImage == null)
        {
            SetColor(Color.white);
            return;
        }

        NoteVisualStyle style = visualConfig.GetStyle(noteType);

        if (style.sprite != null)
            noteImage.sprite = style.sprite;

        noteImage.color = style.color;
        noteImage.preserveAspect = style.preserveAspect;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null && style.uiSize.x > 0f && style.uiSize.y > 0f)
            rectTransform.sizeDelta = style.uiSize;
    }

    public virtual void ApplySkinSprite(Sprite sprite)
    {
        if (sprite == null)
            return;

        if (noteImage == null)
            noteImage = GetComponent<Image>();

        if (noteImage != null)
        {
            noteImage.sprite = sprite;
            noteImage.preserveAspect = true;
            noteImage.color = Color.white;
        }
    }
}
