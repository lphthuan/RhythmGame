using UnityEngine;

/// <summary>
/// Runs lightweight, non-gameplay performance setup before any scene logic.
/// It deliberately does not change quality tiers or note logic, preserving the
/// project's current visuals and rhythm timing.
/// </summary>
public static class PerformanceBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void WarmUpSharedFeedback()
    {
        // Gameplay must accept a held note and additional chord fingers at the
        // same time, including after an Android focus change.
        Input.multiTouchEnabled = true;
        GameplaySfxPlayer.WarmUp();
    }
}
