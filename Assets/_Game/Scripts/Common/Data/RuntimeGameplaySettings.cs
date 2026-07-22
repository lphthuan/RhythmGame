using UnityEngine;

public static class RuntimeGameplaySettings
{
    public enum TapSoundEffectOption
    {
        Tap = 0,
        Arc = 1,
        Mute = 2
    }

    public const string NoteSpeedKey = "NoteSpeed";
    public const string NoteVolumeKey = "NoteVolume";
    public const string MusicVolumeKey = NoteVolumeKey;
    public const string AudioOffsetKey = "AudioOffset";
    public const string AudioPresetKey = "AudioPreset";
    public const string FrameRateKey = "VisualQuality";
    public const string TapSoundEffectKey = "TapSoundEffect";
    public const string TapSoundVolumeKey = "TapSoundVolume";

    public const float DefaultNoteSpeedMultiplier = 2.5f;
    public const int DefaultNoteVolumePercent = 100;
    public const int DefaultTapSoundVolumePercent = 100;
    public const int DefaultAudioOffsetMs = 0;
    // The project now uses Unity's low-latency 256-sample Android DSP buffer.
    // Keep the small remaining output-buffer compensation separate from the
    // player's own visible calibration stored in PlayerPrefs.
    public const int DefaultAndroidAudioOffsetMs = -22;
    public const int DefaultFrameRateIndex = 0;
    public const int UnlimitedFrameRate = -1;
    public const float MinNoteSpeedSetting = 1f;
    public const float MaxNoteSpeedSetting = 6.5f;
    public const float MinScrollSpeed = 50f;
    public const float MaxScrollSpeed = 3500f;
    public static readonly int[] FrameRateOptions = { 60, 120, UnlimitedFrameRate };
    public static readonly string[] FrameRateLabels = { "60 FPS", "120 FPS", "Display FPS (max 120)" };

    public static float NoteSpeedMultiplier
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat(NoteSpeedKey, DefaultNoteSpeedMultiplier), MinNoteSpeedSetting, MaxNoteSpeedSetting);
        set => PlayerPrefs.SetFloat(NoteSpeedKey, Mathf.Clamp(value, MinNoteSpeedSetting, MaxNoteSpeedSetting));
    }

    public static float ScrollSpeed
    {
        get => Mathf.Lerp(
            MinScrollSpeed,
            MaxScrollSpeed,
            Mathf.InverseLerp(MinNoteSpeedSetting, MaxNoteSpeedSetting, NoteSpeedMultiplier));
        set => NoteSpeedMultiplier = Mathf.Lerp(
            MinNoteSpeedSetting,
            MaxNoteSpeedSetting,
            Mathf.InverseLerp(MinScrollSpeed, MaxScrollSpeed, value));
    }

    public static int NoteVolumePercent
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(NoteVolumeKey, DefaultNoteVolumePercent), 0, 100);
        set => PlayerPrefs.SetInt(NoteVolumeKey, Mathf.Clamp(value, 0, 100));
    }

    public static float NoteVolume01 => NoteVolumePercent / 100f;
    public static int MasterVolumePercent
    {
        get => NoteVolumePercent;
        set
        {
            NoteVolumePercent = value;
            ApplyAudioVolumes();
        }
    }

    public static float MasterVolume01 => MasterVolumePercent / 100f;
    public static int MusicVolumePercent { get => MasterVolumePercent; set => MasterVolumePercent = value; }
    public static float MusicVolume01 => MasterVolume01;

    public static int TapSoundVolumePercent
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(TapSoundVolumeKey, DefaultTapSoundVolumePercent), 0, 100);
        set => PlayerPrefs.SetInt(TapSoundVolumeKey, Mathf.Clamp(value, 0, 100));
    }

    public static float TapSoundVolume01 => TapSoundVolumePercent / 100f;

    public static int AudioOffsetMs
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(AudioOffsetKey, DefaultAudioOffsetMs), -500, 1000);
        set => PlayerPrefs.SetInt(AudioOffsetKey, Mathf.Clamp(value, -500, 1000));
    }

    public static float AudioOffsetSeconds => AudioOffsetMs / 1000f;

    public static int PlatformAudioOffsetMs =>
        Application.platform == RuntimePlatform.Android
            ? DefaultAndroidAudioOffsetMs
            : 0;

    public static float PlatformAudioOffsetSeconds => PlatformAudioOffsetMs / 1000f;

    public static float EffectiveAudioOffsetSeconds =>
        AudioOffsetSeconds + PlatformAudioOffsetSeconds;

    public static bool HeadphonesPreset
    {
        get => PlayerPrefs.GetInt(AudioPresetKey, 0) == 1;
        set => PlayerPrefs.SetInt(AudioPresetKey, value ? 1 : 0);
    }

    public static TapSoundEffectOption TapSoundEffect
    {
        get => (TapSoundEffectOption)Mathf.Clamp(PlayerPrefs.GetInt(TapSoundEffectKey, (int)TapSoundEffectOption.Tap), (int)TapSoundEffectOption.Tap, (int)TapSoundEffectOption.Mute);
        set => PlayerPrefs.SetInt(TapSoundEffectKey, Mathf.Clamp((int)value, (int)TapSoundEffectOption.Tap, (int)TapSoundEffectOption.Mute));
    }

    public static int FrameRateIndex
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(FrameRateKey, DefaultFrameRateIndex), 0, FrameRateOptions.Length - 1);
        set => PlayerPrefs.SetInt(FrameRateKey, Mathf.Clamp(value, 0, FrameRateOptions.Length - 1));
    }

    public static string FrameRateLabel => FrameRateLabels[FrameRateIndex];

    // Apply these before the first scene is rendered. Previously the frame cap was
    // only applied after a UIManager had started, so the opening scene could still
    // inherit the QualitySettings v-sync configuration.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedRuntimeSettings()
    {
        ApplyAudioVolumes();
        ApplyFrameRate();
    }

    public static void ApplyAudioVolumes()
    {
        AudioListener.volume = MasterVolume01;
    }

    public static void ApplyFrameRate()
    {
        ApplyFrameRate(FrameRateIndex);
    }

    public static void ApplyFrameRate(int frameRateIndex)
    {
        frameRateIndex = Mathf.Clamp(frameRateIndex, 0, FrameRateOptions.Length - 1);
        int targetFrameRate = ResolveTargetFrameRate(FrameRateOptions[frameRateIndex]);

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }

    private static int ResolveTargetFrameRate(int requestedFrameRate)
    {
#if UNITY_ANDROID || UNITY_IOS
        // An uncapped render loop consumes every available CPU/GPU slice, then
        // stutters under thermal pressure. Cap this option to the physical
        // display; a 120 Hz panel still gets a 120 FPS target.
        if (requestedFrameRate == UnlimitedFrameRate)
        {
            float refreshRate = (float)Screen.currentResolution.refreshRateRatio.value;
            int displayFrameRate = Mathf.RoundToInt(refreshRate);
            return Mathf.Clamp(displayFrameRate > 0 ? displayFrameRate : 60, 60, 120);
        }
#endif

        return requestedFrameRate;
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }
}
