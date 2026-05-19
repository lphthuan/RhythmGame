using UnityEngine;

[System.Serializable]
public class JudgmentWindow
{
    [Header("Judgment Window In Seconds")]
    public float perfectWindow = 0.05f; // 50ms
    public float greatWindow = 0.09f;   // 90ms
    public float goodWindow = 0.15f;    // 150ms

    public HitJudgment Judge(float inputTime, float hitTime, out float deltaMs)
    {
        float delta = inputTime - hitTime;
        float absDelta = Mathf.Abs(delta);

        deltaMs = delta * 1000f;

        if (absDelta <= perfectWindow)
            return HitJudgment.Perfect;

        if (absDelta <= greatWindow)
            return HitJudgment.Great;

        if (absDelta <= goodWindow)
            return HitJudgment.Good;

        return HitJudgment.Miss;
    }

    public bool IsInsideHitWindow(float inputTime, float hitTime)
    {
        float delta = Mathf.Abs(inputTime - hitTime);
        return delta <= goodWindow;
    }
}