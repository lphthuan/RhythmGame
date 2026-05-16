using UnityEngine;

public class SlideNote : NoteBase
{
    [Header("Slide")]
    [SerializeField] private SlideCheckpointSystem checkpointSystem;

    protected override void Awake()
    {
        base.Awake();
        noteType = NoteType.Slide;

        if (checkpointSystem == null)
            checkpointSystem = GetComponent<SlideCheckpointSystem>();
    }

    public override void OnPointerBegin(NotePointer pointer)
    {
        if (checkpointSystem == null)
        {
            Fail(NoteResult.Failed);
            return;
        }

        bool started = checkpointSystem.Begin(pointer.position);

        if (!started)
        {
            Fail(NoteResult.Failed);
            return;
        }

        SetColor(Color.yellow);
    }

    public override void OnPointerMove(NotePointer pointer)
    {
        if (checkpointSystem == null)
            return;

        checkpointSystem.Move(pointer.position);

        if (checkpointSystem.IsCompleted())
        {
            Complete(NoteResult.Completed);
        }
    }

    public override void OnPointerEnd(NotePointer pointer)
    {
        if (IsFinished)
            return;

        if (checkpointSystem == null)
        {
            Fail(NoteResult.Failed);
            return;
        }

        if (!checkpointSystem.IsCompleted())
        {
            checkpointSystem.Cancel();
            Fail(NoteResult.Failed);
        }
    }

    public override float GetAutoMissTime(float missAfterHitTime)
    {
        return hitTime + duration + missAfterHitTime;
    }
}