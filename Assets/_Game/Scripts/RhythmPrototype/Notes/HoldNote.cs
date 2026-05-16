using UnityEngine;

public class HoldNote : NoteBase
{
    private readonly HoldNoteStateMachine stateMachine = new HoldNoteStateMachine();

    protected override void Awake()
    {
        base.Awake();
        noteType = NoteType.Hold;
    }

    public override void OnPointerBegin(NotePointer pointer)
    {
        stateMachine.StartHold(pointer.fingerId, pointer.time, duration);
        SetColor(Color.yellow);
    }

    public override void Tick(float currentTime)
    {
        base.Tick(currentTime);

        if (IsFinished)
            return;

        stateMachine.Tick(Time.unscaledTime);

        if (stateMachine.IsCompleted())
            Complete(NoteResult.Completed);
    }

    public override void OnPointerEnd(NotePointer pointer)
    {
        stateMachine.Release(pointer.fingerId);

        if (stateMachine.IsReleasedEarly())
            Fail(NoteResult.ReleasedEarly);
    }

    public override float GetAutoMissTime(float missAfterHitTime)
    {
        return hitTime + duration + missAfterHitTime;
    }
}