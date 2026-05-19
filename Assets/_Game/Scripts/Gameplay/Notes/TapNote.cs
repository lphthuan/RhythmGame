using UnityEngine;

public class TapNote : NoteBase
{
    protected override void Awake()
    {
        base.Awake();
        noteType = NoteType.Tap;
    }

    public override void OnPointerBegin(NotePointer pointer)
    {
        owner?.NotifyJudgmentEffect(this);
        Complete(NoteResult.Completed);
    }
}