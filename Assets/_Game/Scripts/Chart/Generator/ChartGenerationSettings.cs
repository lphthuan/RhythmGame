using UnityEngine;

public enum ChartDifficultyPreset
{
    Easy,
    Normal,
    Hard
}

[CreateAssetMenu(
    fileName = "ChartGenerationSettings",
    menuName = "Rhythm Game/Chart Generation Settings"
)]
public class ChartGenerationSettings : ScriptableObject
{
    [Header("Preset")]
    public ChartDifficultyPreset difficulty = ChartDifficultyPreset.Normal;

    [Header("Layout")]
    public int laneCount = 4;

    [Header("Timing")]
    public int subdivision = 2;

    [Header("Note Density")]
    [Range(0f, 1f)]
    public float noteChance = 0.5f;

    [Header("Lane Rules")]
    public float minSameLaneIntervalBeat = 1f;

    public void ApplyPreset()
    {
        switch (difficulty)
        {
            case ChartDifficultyPreset.Easy:
                subdivision = 1;
                noteChance = 0.25f;
                minSameLaneIntervalBeat = 1.5f;
                break;

            case ChartDifficultyPreset.Normal:
                subdivision = 2;
                noteChance = 0.5f;
                minSameLaneIntervalBeat = 1f;
                break;

            case ChartDifficultyPreset.Hard:
                subdivision = 4;
                noteChance = 0.65f;
                minSameLaneIntervalBeat = 0.5f;
                break;
        }
    }
}