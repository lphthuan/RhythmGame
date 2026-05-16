using UnityEngine;

public class FlickNote : NoteBase
{
    [Header("Flick Settings")]
    [SerializeField] private FlickDirection requiredDirection = FlickDirection.Any;
    [SerializeField] private float minDistance = 90f;
    [SerializeField] private float minVelocity = 650f;
    [SerializeField] private float maxFlickTime = 0.25f;
    [SerializeField] private float allowedAngle = 50f;

    private Vector2 startPosition;
    private float startTime;
    private bool checkedFlick;

    protected override void Awake()
    {
        base.Awake();
        noteType = NoteType.Flick;
    }

    public override void Initialize(NoteRuntimeData data)
    {
        base.Initialize(data);
        requiredDirection = data.flickDirection;
    }

    public override void OnPointerBegin(NotePointer pointer)
    {
        startPosition = pointer.position;
        startTime = pointer.time;
        checkedFlick = false;

        SetColor(Color.cyan);
    }

    public override void OnPointerMove(NotePointer pointer)
    {
        if (checkedFlick)
            return;

        float elapsed = pointer.time - startTime;

        if (elapsed > maxFlickTime)
        {
            checkedFlick = true;
            Fail(NoteResult.Failed);
            return;
        }

        Vector2 delta = pointer.position - startPosition;
        float distance = delta.magnitude;

        if (distance < minDistance)
            return;

        float velocity = distance / Mathf.Max(elapsed, 0.001f);

        if (velocity < minVelocity)
            return;

        checkedFlick = true;

        if (IsDirectionValid(delta))
            Complete(NoteResult.Completed);
        else
            Fail(NoteResult.Failed);
    }

    public override void OnPointerEnd(NotePointer pointer)
    {
        if (!checkedFlick && !IsFinished)
            Fail(NoteResult.Failed);
    }

    private bool IsDirectionValid(Vector2 delta)
    {
        if (requiredDirection == FlickDirection.Any)
            return true;

        Vector2 requiredVector = DirectionToVector(requiredDirection);

        float angle = Vector2.Angle(delta.normalized, requiredVector);

        return angle <= allowedAngle;
    }

    private Vector2 DirectionToVector(FlickDirection direction)
    {
        switch (direction)
        {
            case FlickDirection.Up:
                return Vector2.up;

            case FlickDirection.Down:
                return Vector2.down;

            case FlickDirection.Left:
                return Vector2.left;

            case FlickDirection.Right:
                return Vector2.right;

            default:
                return Vector2.zero;
        }
    }
}