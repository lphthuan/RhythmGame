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

    public void ForceMiss()
    {
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
        noteId = data.noteId;
        laneIndex = data.laneIndex;
        hitTime = data.hitTime;
        duration = data.duration;
        touchRadius = data.touchRadius;

        completed = false;
        failed = false;
        assigned = false;
        assignedFingerId = -1;

        if (movement == null)
            movement = GetComponent<NoteMovement>();

        if (movement != null)
            movement.Initialize(data);

        ResetVisual();
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
        SetColor(Color.white);
    }
}