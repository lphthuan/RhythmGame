using System.Collections.Generic;
using UnityEngine;

public enum GameplaySfxCue
{
    FadeIn,
    FadeOut,
    Tap,
    Arc,
    HpClear,
    TrackClear,
    TrackFail,
    TrackAllPerfect,
    Unlock
}

/// <summary>Shared, persistent player for the small set of gameplay and store feedback sounds.</summary>
public class GameplaySfxPlayer : MonoBehaviour
{
    private const string CatalogPath = "GameplaySfxCatalog";
    // A rhythm chart can trigger several taps in one frame. Keep a small fixed
    // voice pool so bursts do not continuously add AudioSource components.
    private const int InitialSourceCount = 8;
    private const int MaxSourceCount = 16;
    private static GameplaySfxPlayer instance;
    private static GameplaySfxCatalog catalog;

    private readonly List<AudioSource> sources = new List<AudioSource>();
    private int nextSourceToRecycle;

    public static GameplaySfxCatalog Catalog
    {
        get
        {
            if (catalog == null)
                catalog = Resources.Load<GameplaySfxCatalog>(CatalogPath);
            return catalog;
        }
    }

    public static float Play(GameplaySfxCue cue)
    {
        AudioClip clip = GetClip(cue);
        if (clip == null)
            return 0f;

        GameplaySfxPlayer player = EnsureInstance();
        AudioSource source = player.GetAvailableSource();
        source.clip = clip;
        source.volume = IsTapSound(cue) ? RuntimeGameplaySettings.TapSoundVolume01 : 1f;
        source.Play();
        return clip.length;
    }

    /// <summary>Creates the bounded voice pool before gameplay starts.</summary>
    public static void WarmUp()
    {
        // Load the small SFX catalog at a controlled point instead of during a
        // dense first input burst.
        _ = Catalog;
        EnsureInstance().EnsureSourceCapacity(InitialSourceCount);
    }

    public static void PlayTapSound()
    {
        switch (RuntimeGameplaySettings.TapSoundEffect)
        {
            case RuntimeGameplaySettings.TapSoundEffectOption.Tap:
                Play(GameplaySfxCue.Tap);
                break;
            case RuntimeGameplaySettings.TapSoundEffectOption.Arc:
                Play(GameplaySfxCue.Arc);
                break;
        }
    }

    public static void PreviewTapSound()
    {
        RuntimeGameplaySettings.TapSoundEffectOption effect = RuntimeGameplaySettings.TapSoundEffect;
        if (effect == RuntimeGameplaySettings.TapSoundEffectOption.Mute)
            return;

        Play(effect == RuntimeGameplaySettings.TapSoundEffectOption.Arc ? GameplaySfxCue.Arc : GameplaySfxCue.Tap);
    }

    public static AudioClip GetClip(GameplaySfxCue cue)
    {
        GameplaySfxCatalog sourceCatalog = Catalog;
        if (sourceCatalog == null)
            return null;

        switch (cue)
        {
            case GameplaySfxCue.FadeIn: return sourceCatalog.fadeIn;
            case GameplaySfxCue.FadeOut: return sourceCatalog.fadeOut;
            case GameplaySfxCue.Tap: return sourceCatalog.tap;
            case GameplaySfxCue.Arc: return sourceCatalog.arc;
            case GameplaySfxCue.HpClear: return sourceCatalog.hpClear;
            case GameplaySfxCue.TrackClear: return sourceCatalog.trackClear;
            case GameplaySfxCue.TrackFail: return sourceCatalog.trackFail;
            case GameplaySfxCue.TrackAllPerfect: return sourceCatalog.trackAllPerfect;
            case GameplaySfxCue.Unlock: return sourceCatalog.unlock;
            default: return null;
        }
    }

    private static bool IsTapSound(GameplaySfxCue cue)
    {
        return cue == GameplaySfxCue.Tap || cue == GameplaySfxCue.Arc;
    }

    private static GameplaySfxPlayer EnsureInstance()
    {
        if (instance != null)
            return instance;

        GameObject root = new GameObject("RG Gameplay SFX");
        instance = root.AddComponent<GameplaySfxPlayer>();
        DontDestroyOnLoad(root);
        return instance;
    }

    private AudioSource GetAvailableSource()
    {
        for (int i = 0; i < sources.Count; i++)
        {
            if (!sources[i].isPlaying)
                return sources[i];
        }

        if (sources.Count < MaxSourceCount)
        {
            AudioSource source = CreateSource();
            sources.Add(source);
            return source;
        }

        // At the configured ceiling, recycle the oldest round-robin voice. This
        // caps component and mixer work while still preserving responsive taps.
        AudioSource recycled = sources[nextSourceToRecycle];
        nextSourceToRecycle = (nextSourceToRecycle + 1) % sources.Count;
        recycled.Stop();
        return recycled;
    }

    private void EnsureSourceCapacity(int count)
    {
        int target = Mathf.Min(count, MaxSourceCount);
        while (sources.Count < target)
            sources.Add(CreateSource());
    }

    private AudioSource CreateSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.ignoreListenerPause = true;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        return source;
    }
}
