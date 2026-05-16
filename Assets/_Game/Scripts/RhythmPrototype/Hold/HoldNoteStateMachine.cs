public class HoldNoteStateMachine
{
    public HoldNoteState CurrentState { get; private set; } = HoldNoteState.Idle;

    private float requiredDuration;
    private float holdStartTime;
    private int fingerId;

    public void StartHold(int fingerId, float startTime, float duration)
    {
        this.fingerId = fingerId;
        holdStartTime = startTime;
        requiredDuration = duration;
        CurrentState = HoldNoteState.Holding;
    }

    public void Tick(float currentTime)
    {
        if (CurrentState != HoldNoteState.Holding)
            return;

        float heldTime = currentTime - holdStartTime;

        if (heldTime >= requiredDuration)
            CurrentState = HoldNoteState.Completed;
    }

    public void Release(int releaseFingerId)
    {
        if (CurrentState != HoldNoteState.Holding)
            return;

        if (releaseFingerId != fingerId)
            return;

        CurrentState = HoldNoteState.ReleasedEarly;
    }

    public bool IsHolding()
    {
        return CurrentState == HoldNoteState.Holding;
    }

    public bool IsCompleted()
    {
        return CurrentState == HoldNoteState.Completed;
    }

    public bool IsReleasedEarly()
    {
        return CurrentState == HoldNoteState.ReleasedEarly;
    }
}