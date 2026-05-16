public class HoldNoteStateMachine
{
    public HoldNoteState CurrentState { get; private set; } = HoldNoteState.Idle;

    private int fingerId = -1;

    private float headHitTime;
    private float tailHitTime;

    public float Progress01 { get; private set; }

    public void StartHold(int fingerId, float noteHitTime, float duration, float currentTime)
    {
        this.fingerId = fingerId;

        headHitTime = noteHitTime;
        tailHitTime = noteHitTime + duration;

        Progress01 = CalculateProgress(currentTime);

        CurrentState = HoldNoteState.Holding;
    }

    public void Tick(float currentTime)
    {
        if (CurrentState != HoldNoteState.Holding)
            return;

        Progress01 = CalculateProgress(currentTime);

        if (currentTime >= tailHitTime)
        {
            Progress01 = 1f;
            CurrentState = HoldNoteState.Completed;
        }
    }

    public void Release(int releaseFingerId, float currentTime)
    {
        if (CurrentState != HoldNoteState.Holding)
            return;

        if (releaseFingerId != fingerId)
            return;

        if (currentTime < tailHitTime)
        {
            CurrentState = HoldNoteState.ReleasedEarly;
        }
        else
        {
            Progress01 = 1f;
            CurrentState = HoldNoteState.Completed;
        }
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

    private float CalculateProgress(float currentTime)
    {
        float totalDuration = tailHitTime - headHitTime;

        if (totalDuration <= 0f)
            return 1f;

        float heldTime = currentTime - headHitTime;

        return UnityEngine.Mathf.Clamp01(heldTime / totalDuration);
    }
}