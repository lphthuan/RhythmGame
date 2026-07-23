#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Dypsloom.RhythmTimeline.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SongImportWindow : EditorWindow
{
    private const string MusicFolder = "Assets/_Game/Audio/Music/OsuImported";
    private const string CoverFolder = "Assets/_Game/Sprites/UI/OsuImported";
    private const string SongDataFolder = "Assets/_Game/Data/Songs/OsuImported";
    private const string TimelineFolder = "Assets/_Game/Data/Timelines/OsuImported";

    private int selectedTab;

    private string osuFilePath = string.Empty;
    private string chartFileName = string.Empty;
    private bool copyAudioToProject = true;
    private bool copyCoverToProject = true;
    private bool createOrUpdateSongData = true;
    private bool createTimelineAsset = true;
    private bool previewInOpenTool = true;
    private bool prepareRuntimeSpawner = true;
    private SongData targetSongData;
    private Difficulty difficultySlot = Difficulty.Medium;
    private bool autoDetectDifficulty = true;
    private bool applyStoreAccessOnOsuImport = true;
    private SongUnlockType osuUnlockType = SongUnlockType.Free;
    private int osuMoneyPrice = 500;
    private int osuDiamondPrice = 10;

    private string mp3FilePath = string.Empty;
    private string mp3SongTitle = string.Empty;
    private string mp3Artist = string.Empty;
    private Sprite mp3Cover;
    private SongData mp3TargetSongData;
    private Difficulty mp3DifficultySlot = Difficulty.Medium;
    private bool mp3GenerateAllDifficulties = true;
    private bool mp3AutoDetectBpm = true;
    private float mp3Bpm = 120f;
    private float mp3ChartOffsetSeconds;
    private float mp3FirstBeatSeconds;
    private int mp3LaneCount = 4;
    private float mp3EasyDensity = 0.38f;
    private float mp3NormalDensity = 0.58f;
    private float mp3HardDensity = 0.78f;
    private float mp3HoldRatio = 0.08f;
    private float mp3FlickRatio = 0.05f;
    private bool mp3CreateTimelineAsset = true;
    private bool mp3CreateOrUpdateSongData = true;
    private bool mp3PreviewInOpenTool = true;
    private bool mp3PrepareRuntimeSpawner = true;
    private bool applyStoreAccessOnMp3Import = true;
    private SongUnlockType mp3UnlockType = SongUnlockType.Free;
    private int mp3MoneyPrice = 500;
    private int mp3DiamondPrice = 10;
    private ChartData mp3PreviewChart;
    private GeneratedChartPreview mp3PreviewSummary;
    private Difficulty mp3PreviewDifficulty = Difficulty.Medium;

    [MenuItem("Tools/RhythmGame/Song Import")]
    public static void Open()
    {
        GetWindow<SongImportWindow>("Song Import");
    }

    [MenuItem("Tools/RhythmGame/Chart/Import Osu Mania Beatmap")]
    public static void OpenLegacy()
    {
        Open();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Song Import", EditorStyles.boldLabel);
        selectedTab = GUILayout.Toolbar(selectedTab, new[] { "osu!mania Chart", "MP3 Auto Chart" });
        EditorGUILayout.Space(8f);

        if (selectedTab == 0)
            DrawOsuImport();
        else
            DrawMp3Import();
    }

    private void DrawOsuImport()
    {
        EditorGUILayout.HelpBox(
            "Import .osu Mode: 3 into chart JSON, optional Timeline, and one shared SongData hub with Easy/Normal/Hard slots.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.TextField("osu File", osuFilePath);
            if (GUILayout.Button("Browse", GUILayout.Width(86f)))
                BrowseOsuFile();
        }

        chartFileName = EditorGUILayout.TextField("Chart File Name", chartFileName);
        copyAudioToProject = EditorGUILayout.Toggle("Copy Audio To Project", copyAudioToProject);
        copyCoverToProject = EditorGUILayout.Toggle("Copy Cover To Project", copyCoverToProject);
        createTimelineAsset = EditorGUILayout.Toggle("Create Timeline Asset", createTimelineAsset);
        createOrUpdateSongData = EditorGUILayout.Toggle("Create/Update SongData Hub", createOrUpdateSongData);
        targetSongData = (SongData)EditorGUILayout.ObjectField("Target SongData (Optional)", targetSongData, typeof(SongData), false);
        autoDetectDifficulty = EditorGUILayout.Toggle("Auto Detect Difficulty", autoDetectDifficulty);
        using (new EditorGUI.DisabledScope(autoDetectDifficulty))
            difficultySlot = (Difficulty)EditorGUILayout.EnumPopup("Difficulty Slot", difficultySlot);
        DrawOsuStoreAccess();
        previewInOpenTool = EditorGUILayout.Toggle("Preview In Open Tool", previewInOpenTool);
        prepareRuntimeSpawner = EditorGUILayout.Toggle("Prepare Runtime Spawner", prepareRuntimeSpawner);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(osuFilePath)))
        {
            if (GUILayout.Button("Import osu!mania Beatmap", GUILayout.Height(30f)))
                ImportBeatmap();
        }
    }

    private void DrawMp3Import()
    {
        EditorGUILayout.HelpBox(
            "Create auto-generated chart JSON and Timeline assets from one MP3. This does not read melody perfectly yet; it estimates BPM and places notes from beat-grid patterns.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.TextField("MP3 File", mp3FilePath);
            if (GUILayout.Button("Browse", GUILayout.Width(86f)))
                BrowseMp3File();
        }

        mp3SongTitle = EditorGUILayout.TextField("Song Title", mp3SongTitle);
        mp3Artist = EditorGUILayout.TextField("Artist", mp3Artist);
        mp3Cover = (Sprite)EditorGUILayout.ObjectField("Cover Sprite", mp3Cover, typeof(Sprite), false);
        mp3TargetSongData = (SongData)EditorGUILayout.ObjectField("Target SongData (Optional)", mp3TargetSongData, typeof(SongData), false);
        mp3GenerateAllDifficulties = EditorGUILayout.Toggle("Generate Easy/Normal/Hard", mp3GenerateAllDifficulties);
        using (new EditorGUI.DisabledScope(mp3GenerateAllDifficulties))
            mp3DifficultySlot = (Difficulty)EditorGUILayout.EnumPopup("Difficulty Slot", mp3DifficultySlot);
        mp3AutoDetectBpm = EditorGUILayout.Toggle("Auto Detect BPM", mp3AutoDetectBpm);
        using (new EditorGUI.DisabledScope(mp3AutoDetectBpm))
            mp3Bpm = EditorGUILayout.FloatField("BPM", mp3Bpm);
        mp3ChartOffsetSeconds = EditorGUILayout.FloatField("Chart Offset Seconds", mp3ChartOffsetSeconds);
        mp3FirstBeatSeconds = Mathf.Max(0f, EditorGUILayout.FloatField("First Beat Seconds", mp3FirstBeatSeconds));
        mp3LaneCount = Mathf.Clamp(EditorGUILayout.IntField("Lane Count", mp3LaneCount), 1, 8);
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Density", EditorStyles.boldLabel);
        mp3EasyDensity = EditorGUILayout.Slider("Easy Density", mp3EasyDensity, 0.05f, 1f);
        mp3NormalDensity = EditorGUILayout.Slider("Normal Density", mp3NormalDensity, 0.05f, 1f);
        mp3HardDensity = EditorGUILayout.Slider("Hard Density", mp3HardDensity, 0.05f, 1f);
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Note Type Ratio", EditorStyles.boldLabel);
        mp3HoldRatio = EditorGUILayout.Slider("Hold Ratio", mp3HoldRatio, 0f, 0.45f);
        mp3FlickRatio = EditorGUILayout.Slider("Flick Ratio", mp3FlickRatio, 0f, 0.35f);
        mp3CreateTimelineAsset = EditorGUILayout.Toggle("Create Timeline Asset", mp3CreateTimelineAsset);
        mp3CreateOrUpdateSongData = EditorGUILayout.Toggle("Create/Update SongData Hub", mp3CreateOrUpdateSongData);
        DrawMp3StoreAccess();
        mp3PreviewInOpenTool = EditorGUILayout.Toggle("Preview In Open Tool", mp3PreviewInOpenTool);
        mp3PrepareRuntimeSpawner = EditorGUILayout.Toggle("Prepare Runtime Spawner", mp3PrepareRuntimeSpawner);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(mp3FilePath)))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                mp3PreviewDifficulty = (Difficulty)EditorGUILayout.EnumPopup("Preview Difficulty", mp3PreviewDifficulty);
                if (GUILayout.Button("Preview Chart", GUILayout.Width(120f)))
                    PreviewMp3AutoChart();
            }

            DrawMp3Preview();

            if (GUILayout.Button("Generate From MP3", GUILayout.Height(30f)))
                ImportMp3AutoChart();
        }
    }

    private void DrawOsuStoreAccess()
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Store Access", EditorStyles.boldLabel);
        applyStoreAccessOnOsuImport = EditorGUILayout.Toggle("Apply Store Access", applyStoreAccessOnOsuImport);
        using (new EditorGUI.DisabledScope(!applyStoreAccessOnOsuImport))
        {
            osuUnlockType = (SongUnlockType)EditorGUILayout.EnumPopup("Availability", osuUnlockType);
            if (osuUnlockType == SongUnlockType.Purchase)
            {
                osuMoneyPrice = Mathf.Max(0, EditorGUILayout.IntField("Money Price", osuMoneyPrice));
                osuDiamondPrice = Mathf.Max(0, EditorGUILayout.IntField("Diamond Price", osuDiamondPrice));
            }
        }
    }

    private void DrawMp3StoreAccess()
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Store Access", EditorStyles.boldLabel);
        applyStoreAccessOnMp3Import = EditorGUILayout.Toggle("Apply Store Access", applyStoreAccessOnMp3Import);
        using (new EditorGUI.DisabledScope(!applyStoreAccessOnMp3Import))
        {
            mp3UnlockType = (SongUnlockType)EditorGUILayout.EnumPopup("Availability", mp3UnlockType);
            if (mp3UnlockType == SongUnlockType.Purchase)
            {
                mp3MoneyPrice = Mathf.Max(0, EditorGUILayout.IntField("Money Price", mp3MoneyPrice));
                mp3DiamondPrice = Mathf.Max(0, EditorGUILayout.IntField("Diamond Price", mp3DiamondPrice));
            }
        }
    }

    private void BrowseOsuFile()
    {
        string selectedPath = EditorUtility.OpenFilePanel("Select osu!mania .osu file", string.Empty, "osu");
        if (string.IsNullOrWhiteSpace(selectedPath))
            return;

        osuFilePath = selectedPath;

        if (OsuManiaBeatmapParser.TryParse(osuFilePath, out OsuManiaBeatmapParser.ImportResult result, out _))
        {
            chartFileName = BuildChartFileName(result);
            difficultySlot = GuessDifficulty(result.Version);
        }
        else
            chartFileName = "chart_" + SongData.SanitizeForFileName(Path.GetFileNameWithoutExtension(osuFilePath));
    }

    private void BrowseMp3File()
    {
        string selectedPath = EditorUtility.OpenFilePanel("Select MP3 file", string.Empty, "mp3");
        if (string.IsNullOrWhiteSpace(selectedPath))
            return;

        mp3FilePath = selectedPath;
        if (string.IsNullOrWhiteSpace(mp3SongTitle))
            mp3SongTitle = Path.GetFileNameWithoutExtension(selectedPath);
    }

    private void ImportBeatmap()
    {
        if (!OsuManiaBeatmapParser.TryParse(osuFilePath, out OsuManiaBeatmapParser.ImportResult result, out string error))
        {
            EditorUtility.DisplayDialog("Import Failed", error, "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(chartFileName))
            chartFileName = BuildChartFileName(result);

        ChartSaveLoad.Save(result.Chart, chartFileName);

        string songGroupId = BuildSongGroupId(result);
        AudioClip importedClip = copyAudioToProject ? ImportAudio(result, songGroupId) : null;
        Sprite importedCover = copyCoverToProject ? ImportCover(result) : null;
        Difficulty selectedDifficulty = autoDetectDifficulty ? GuessDifficulty(result.Version) : difficultySlot;
        RhythmTimelineAsset timeline = createTimelineAsset ? CreateTimeline(result.Chart, songGroupId, importedClip, selectedDifficulty) : null;

        SongData songData = null;
        if (createOrUpdateSongData)
        {
            songData = CreateOrUpdateSongData(
                BuildSongTitle(result),
                songGroupId,
                result.Chart.bpm,
                importedClip,
                importedCover,
                chartFileName,
                timeline,
                selectedDifficulty,
                targetSongData,
                applyStoreAccessOnOsuImport,
                osuUnlockType,
                osuMoneyPrice,
                osuDiamondPrice);
        }

        SyncOpenScene(result.Chart, importedClip, chartFileName, previewInOpenTool, prepareRuntimeSpawner);

        string message =
            $"Imported '{Path.GetFileName(osuFilePath)}'\n" +
            $"Chart: {chartFileName}.json\n" +
            $"Difficulty Slot: {selectedDifficulty}\n" +
            $"Lanes: {result.LaneCount}\n" +
            $"Notes: {result.Chart.notes.Count}\n" +
            $"BPM: {result.Chart.bpm:0.###}";

        if (timeline != null)
            message += $"\nTimeline: {AssetDatabase.GetAssetPath(timeline)}";
        if (songData != null)
            message += $"\nSongData: {AssetDatabase.GetAssetPath(songData)}";

        Debug.Log($"SongImportWindow osu: {message.Replace("\n", " | ")}");
        EditorUtility.DisplayDialog("osu!mania Import Complete", message, "OK");
    }

    private void ImportMp3AutoChart()
    {
        if (!File.Exists(mp3FilePath))
        {
            EditorUtility.DisplayDialog("MP3 Import Failed", "Selected MP3 file does not exist.", "OK");
            return;
        }

        string title = string.IsNullOrWhiteSpace(mp3SongTitle)
            ? Path.GetFileNameWithoutExtension(mp3FilePath)
            : mp3SongTitle.Trim();
        string groupId = BuildSongGroupId(mp3Artist, title);

        AudioClip clip = ImportAudioFile(mp3FilePath, groupId);
        if (clip == null)
        {
            EditorUtility.DisplayDialog("MP3 Import Failed", "MP3 was copied but Unity could not load it as AudioClip.", "OK");
            return;
        }

        float bpm = mp3AutoDetectBpm ? BpmDetector.Detect(clip) : Mathf.Max(1f, mp3Bpm);
        mp3Bpm = bpm;

        Difficulty[] difficulties = mp3GenerateAllDifficulties
            ? new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard }
            : new[] { mp3DifficultySlot };

        SongData songData = null;
        ChartData lastChart = null;
        string lastChartName = string.Empty;
        int totalNotes = 0;

        foreach (Difficulty difficulty in difficulties)
        {
            ChartDifficultyPreset preset = ToGeneratorPreset(difficulty);
            string difficultyName = GetDifficultyFileSuffix(difficulty);
            string generatedChartFileName = "chart_" + SongData.SanitizeForFileName(groupId) + "_" + difficultyName;
            ChartData chart = AudioOnsetChartGenerator.Generate(
                title,
                clip,
                bpm,
                mp3ChartOffsetSeconds,
                mp3FirstBeatSeconds,
                mp3LaneCount,
                preset,
                GetMp3Density(difficulty),
                mp3HoldRatio,
                mp3FlickRatio,
                out GeneratedChartPreview preview);

            ChartSaveLoad.Save(chart, generatedChartFileName);
            RhythmTimelineAsset timeline = mp3CreateTimelineAsset
                ? CreateTimeline(chart, groupId, clip, difficulty)
                : null;

            if (mp3CreateOrUpdateSongData)
            {
                songData = CreateOrUpdateSongData(
                    BuildSongTitle(mp3Artist, title),
                    groupId,
                    bpm,
                    clip,
                    mp3Cover,
                    generatedChartFileName,
                    timeline,
                    difficulty,
                    songData != null ? songData : mp3TargetSongData,
                    applyStoreAccessOnMp3Import,
                    mp3UnlockType,
                    mp3MoneyPrice,
                    mp3DiamondPrice);
            }

            lastChart = chart;
            lastChartName = generatedChartFileName;
            totalNotes += chart.notes.Count;
            mp3PreviewChart = chart;
            mp3PreviewSummary = preview;
            mp3PreviewDifficulty = difficulty;
        }

        if (lastChart != null)
            SyncOpenScene(lastChart, clip, lastChartName, mp3PreviewInOpenTool, mp3PrepareRuntimeSpawner);

        string message =
            $"Generated from '{Path.GetFileName(mp3FilePath)}'\n" +
            $"Song: {BuildSongTitle(mp3Artist, title)}\n" +
            $"BPM: {bpm:0.###}\n" +
            $"Difficulties: {difficulties.Length}\n" +
            $"Total Notes: {totalNotes}";

        if (songData != null)
            message += $"\nSongData: {AssetDatabase.GetAssetPath(songData)}";

        Debug.Log($"SongImportWindow mp3: {message.Replace("\n", " | ")}");
        EditorUtility.DisplayDialog("MP3 Auto Chart Complete", message, "OK");
    }

    private void PreviewMp3AutoChart()
    {
        if (!File.Exists(mp3FilePath))
        {
            EditorUtility.DisplayDialog("Preview Failed", "Selected MP3 file does not exist.", "OK");
            return;
        }

        string title = string.IsNullOrWhiteSpace(mp3SongTitle)
            ? Path.GetFileNameWithoutExtension(mp3FilePath)
            : mp3SongTitle.Trim();
        string groupId = BuildSongGroupId(mp3Artist, title);

        AudioClip clip = ImportAudioFile(mp3FilePath, groupId);
        if (clip == null)
        {
            EditorUtility.DisplayDialog("Preview Failed", "MP3 was copied but Unity could not load it as AudioClip.", "OK");
            return;
        }

        float bpm = mp3AutoDetectBpm ? BpmDetector.Detect(clip) : Mathf.Max(1f, mp3Bpm);
        mp3Bpm = bpm;

        ChartDifficultyPreset preset = ToGeneratorPreset(mp3PreviewDifficulty);
        mp3PreviewChart = AudioOnsetChartGenerator.Generate(
            title,
            clip,
            bpm,
            mp3ChartOffsetSeconds,
            mp3FirstBeatSeconds,
            mp3LaneCount,
            preset,
            GetMp3Density(mp3PreviewDifficulty),
            mp3HoldRatio,
            mp3FlickRatio,
            out mp3PreviewSummary);

        SyncOpenScene(mp3PreviewChart, clip, "preview_mp3_auto_chart", preview: true, prepareSpawner: false);
    }

    private void DrawMp3Preview()
    {
        if (mp3PreviewChart == null)
            return;

        EditorGUILayout.Space(6f);
        EditorGUILayout.HelpBox(
            $"Preview {mp3PreviewDifficulty}: {mp3PreviewSummary.NoteCount} notes | " +
            $"Tap {mp3PreviewSummary.TapCount}, Hold {mp3PreviewSummary.HoldCount}, Flick {mp3PreviewSummary.FlickCount} | " +
            $"Onset avg {mp3PreviewSummary.AverageOnset:0.00}, max {mp3PreviewSummary.StrongestOnset:0.00}",
            MessageType.None);

        Rect rect = GUILayoutUtility.GetRect(1f, 64f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.07f, 0.05f, 0.10f, 1f));
        if (mp3PreviewChart.notes == null || mp3PreviewChart.notes.Count == 0)
            return;

        float length = Mathf.Max(1f, mp3PreviewChart.notes[mp3PreviewChart.notes.Count - 1].time);
        foreach (NoteData note in mp3PreviewChart.notes)
        {
            float x = rect.x + Mathf.Clamp01(note.time / length) * rect.width;
            float laneHeight = rect.height / Mathf.Max(1, mp3PreviewChart.laneCount);
            float y = rect.yMax - (Mathf.Clamp(note.lane, 0, mp3PreviewChart.laneCount - 1) + 1) * laneHeight;
            Color color = note.type == NoteType.Hold
                ? new Color(0.28f, 0.78f, 1f, 1f)
                : note.type == NoteType.Flick
                    ? new Color(1f, 0.45f, 0.75f, 1f)
                    : new Color(1f, 0.88f, 0.30f, 1f);
            EditorGUI.DrawRect(new Rect(x, y + 2f, 2f, Mathf.Max(2f, laneHeight - 4f)), color);
        }
    }

    private static string BuildChartFileName(OsuManiaBeatmapParser.ImportResult result)
    {
        return BuildChartFileName(
            BuildSongGroupId(result),
            GuessDifficulty(result.Version));
    }

    private static string BuildChartFileName(string songGroupId, Difficulty difficulty)
    {
        string safeGroupId = SongData.SanitizeForFileName(songGroupId);
        return "chart_" + safeGroupId + "_" + GetDifficultyFileSuffix(difficulty);
    }

    [MenuItem("Tools/Rhythm Game/Repair Imported Song Chart Names")]
    private static void RepairImportedSongChartNames()
    {
        string[] songGuids = AssetDatabase.FindAssets("t:SongData", new[] { SongDataFolder });
        List<SongData> songs = new List<SongData>();
        Dictionary<string, int> chartNameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (string guid in songGuids)
        {
            SongData song = AssetDatabase.LoadAssetAtPath<SongData>(AssetDatabase.GUIDToAssetPath(guid));
            if (song == null)
                continue;

            songs.Add(song);
            CountChartName(chartNameCounts, song.easyChartFileName);
            CountChartName(chartNameCounts, song.normalChartFileName);
            CountChartName(chartNameCounts, song.hardChartFileName);
        }

        int repairedSlots = 0;
        int recreatedCharts = 0;

        foreach (SongData song in songs)
        {
            repairedSlots += RepairChartSlot(song, Difficulty.Easy, chartNameCounts, ref recreatedCharts);
            repairedSlots += RepairChartSlot(song, Difficulty.Medium, chartNameCounts, ref recreatedCharts);
            repairedSlots += RepairChartSlot(song, Difficulty.Hard, chartNameCounts, ref recreatedCharts);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"SongImportWindow: Repaired {repairedSlots} duplicate/generic chart name slot(s) and recreated {recreatedCharts} JSON chart(s) from their assigned timelines.");
    }

    private static void CountChartName(Dictionary<string, int> counts, string chartFileName)
    {
        if (string.IsNullOrWhiteSpace(chartFileName))
            return;

        counts.TryGetValue(chartFileName, out int count);
        counts[chartFileName] = count + 1;
    }

    private static int RepairChartSlot(
        SongData song,
        Difficulty difficulty,
        Dictionary<string, int> chartNameCounts,
        ref int recreatedCharts)
    {
        string currentName = song.GetChartFileName(difficulty);
        if (string.IsNullOrWhiteSpace(currentName))
            return 0;

        bool isGenericAudioName = currentName.StartsWith("chart_audio", StringComparison.OrdinalIgnoreCase);
        bool isDuplicate = chartNameCounts.TryGetValue(currentName, out int count) && count > 1;
        if (!isGenericAudioName && !isDuplicate)
            return 0;

        RhythmTimelineAsset timeline = song.GetTimelineAsset(difficulty);
        if (timeline == null)
        {
            Debug.LogWarning($"SongImportWindow: Cannot repair '{song.SongTitle}' ({difficulty}) because it has no timeline asset.");
            return 0;
        }

        string groupId = string.IsNullOrWhiteSpace(song.songGroupId)
            ? song.SongTitle
            : song.songGroupId;
        string repairedName = BuildChartFileName(groupId, difficulty);
        SetChartFileName(song, difficulty, repairedName);

        if (ChartSpawnDataProvider.TryGetChartAndSpawnData(timeline, out ChartData chart, out _))
        {
            ChartSaveLoad.Save(chart, repairedName);
            recreatedCharts++;
        }

        EditorUtility.SetDirty(song);
        return 1;
    }

    private static void SetChartFileName(SongData song, Difficulty difficulty, string chartFileName)
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                song.easyChartFileName = chartFileName;
                break;
            case Difficulty.Hard:
                song.hardChartFileName = chartFileName;
                break;
            default:
                song.normalChartFileName = chartFileName;
                break;
        }
    }

    private static AudioClip ImportAudio(OsuManiaBeatmapParser.ImportResult result, string songGroupId)
    {
        if (string.IsNullOrWhiteSpace(result.AudioFilePath) || !File.Exists(result.AudioFilePath))
        {
            Debug.LogWarning($"SongImportWindow: Audio file not found: {result.AudioFilePath}");
            return null;
        }

        return ImportAudioFile(result.AudioFilePath, songGroupId);
    }

    private static AudioClip ImportAudioFile(string sourcePath, string songGroupId)
    {
        EnsureFolder(MusicFolder);

        string extension = Path.GetExtension(sourcePath);
        // osu! exports commonly name every track "audio.mp3". Use the song group
        // instead, otherwise later imports silently reuse another song's AudioClip.
        string safeName = SongData.SanitizeForFileName(songGroupId);
        if (string.IsNullOrWhiteSpace(safeName) || safeName == "unknown")
            safeName = SongData.SanitizeForFileName(Path.GetFileNameWithoutExtension(sourcePath));
        string targetPath = $"{MusicFolder}/{safeName}{extension}";
        AudioClip existing = AssetDatabase.LoadAssetAtPath<AudioClip>(targetPath);
        if (existing != null)
            return existing;

        File.Copy(sourcePath, targetPath, overwrite: false);
        AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceSynchronousImport);

        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(targetPath);
        if (clip == null)
            Debug.LogWarning($"SongImportWindow: Copied audio but could not load AudioClip: {targetPath}");

        return clip;
    }

    private static Sprite ImportCover(OsuManiaBeatmapParser.ImportResult result)
    {
        if (string.IsNullOrWhiteSpace(result.BackgroundFilePath) || !File.Exists(result.BackgroundFilePath))
        {
            Debug.LogWarning($"SongImportWindow: Background file not found: {result.BackgroundFilePath}");
            return null;
        }

        EnsureFolder(CoverFolder);

        string extension = Path.GetExtension(result.BackgroundFilePath);
        string safeName = SongData.SanitizeForFileName(BuildSongGroupId(result));
        string targetPath = AssetDatabase.GenerateUniqueAssetPath($"{CoverFolder}/{safeName}{extension}");

        File.Copy(result.BackgroundFilePath, targetPath, overwrite: false);
        AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = AssetImporter.GetAtPath(targetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(targetPath);
        if (sprite == null)
            Debug.LogWarning($"SongImportWindow: Copied cover but could not load Sprite: {targetPath}");

        return sprite;
    }

    private static RhythmTimelineAsset CreateTimeline(ChartData chart, string groupId, AudioClip clip, Difficulty difficulty)
    {
        EnsureFolder(TimelineFolder);

        string safeGroupId = SongData.SanitizeForFileName(groupId);
        string difficultyName = GetDifficultyFileSuffix(difficulty);
        string path = AssetDatabase.GenerateUniqueAssetPath($"{TimelineFolder}/{safeGroupId}_{difficultyName}_timeline.asset");

        return ChartTimelineConverter.CreateTimelineFromChart(chart, path, clip);
    }

    private static SongData CreateOrUpdateSongData(
        string songTitle,
        string groupId,
        float bpm,
        AudioClip clip,
        Sprite cover,
        string importedChartFileName,
        RhythmTimelineAsset timeline,
        Difficulty difficulty,
        SongData explicitTarget,
        bool applyStoreAccess,
        SongUnlockType unlockType,
        int moneyPrice,
        int diamondPrice)
    {
        EnsureFolder(SongDataFolder);

        string safeGroupId = SongData.SanitizeForFileName(groupId);
        SongData song = explicitTarget != null ? explicitTarget : FindExistingSongData(safeGroupId);
        bool created = false;

        if (song == null)
        {
            string path = AssetDatabase.GenerateUniqueAssetPath($"{SongDataFolder}/{safeGroupId}.asset");
            song = CreateInstance<SongData>();
            AssetDatabase.CreateAsset(song, path);
            created = true;
        }

        Undo.RecordObject(song, created ? "Create SongData Hub" : "Update SongData Hub");
        song._songTitle = songTitle;
        if (string.IsNullOrWhiteSpace(song._sceneName))
            song._sceneName = "KhoaCuBu";
        song._bpm = bpm;
        song._difficulty = "Normal";
        song.songGroupId = safeGroupId;
        song.difficultyLevel = Difficulty.Medium;
        if (created || applyStoreAccess)
        {
            song.unlockType = unlockType;
            song.moneyPrice = Mathf.Max(0, moneyPrice);
            song.diamondPrice = Mathf.Max(0, diamondPrice);
        }
        if (clip != null)
            song.audioClip = clip;
        if (cover != null)
            song._previewImage = cover;

        ApplyDifficultySlot(song, difficulty, importedChartFileName, timeline);

        EditorUtility.SetDirty(song);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return song;
    }

    private static void ApplyDifficultySlot(
        SongData song,
        Difficulty difficulty,
        string importedChartFileName,
        RhythmTimelineAsset timeline)
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                song.easyChartFileName = importedChartFileName;
                if (timeline != null) song.easyTimelineAsset = timeline;
                break;
            case Difficulty.Hard:
                song.hardChartFileName = importedChartFileName;
                if (timeline != null) song.hardTimelineAsset = timeline;
                break;
            default:
                song.normalChartFileName = importedChartFileName;
                if (timeline != null) song.normalTimelineAsset = timeline;
                break;
        }
    }

    private static SongData FindExistingSongData(string safeGroupId)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:SongData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SongData song = AssetDatabase.LoadAssetAtPath<SongData>(path);
            if (song == null)
                continue;

            string existingGroupId = !string.IsNullOrWhiteSpace(song.songGroupId)
                ? SongData.SanitizeForFileName(song.songGroupId)
                : SongData.SanitizeForFileName(BuildSongTitleFromSongData(song));

            if (existingGroupId == safeGroupId)
                return song;
        }

        return null;
    }

    private static SongData FindExistingSongData(OsuManiaBeatmapParser.ImportResult result)
    {
        return FindExistingSongData(SongData.SanitizeForFileName(BuildSongGroupId(result)));
    }

    private static Difficulty GuessDifficulty(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return Difficulty.Medium;

        string value = version.ToLowerInvariant();
        if (value.Contains("easy") || value.Contains("novice") || value.Contains("beginner") || value.Contains("basic"))
            return Difficulty.Easy;
        if (value.Contains("hard") || value.Contains("expert") || value.Contains("insane") || value.Contains("another"))
            return Difficulty.Hard;

        return Difficulty.Medium;
    }

    private static ChartDifficultyPreset ToGeneratorPreset(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => ChartDifficultyPreset.Easy,
            Difficulty.Hard => ChartDifficultyPreset.Hard,
            _ => ChartDifficultyPreset.Normal
        };
    }

    private float GetMp3Density(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => mp3EasyDensity,
            Difficulty.Hard => mp3HardDensity,
            _ => mp3NormalDensity
        };
    }

    private static string GetDifficultyFileSuffix(Difficulty difficulty)
    {
        return difficulty == Difficulty.Medium ? "normal" : difficulty.ToString().ToLowerInvariant();
    }

    private static string BuildSongTitle(OsuManiaBeatmapParser.ImportResult result)
    {
        return BuildSongTitle(result.Artist, result.Title);
    }

    private static string BuildSongTitle(string artist, string title)
    {
        string safeTitle = string.IsNullOrWhiteSpace(title) ? "Unknown Song" : title.Trim();
        if (string.IsNullOrWhiteSpace(artist))
            return safeTitle;

        return $"{artist.Trim()} - {safeTitle}";
    }

    private static string BuildSongGroupId(OsuManiaBeatmapParser.ImportResult result)
    {
        string artist = string.IsNullOrWhiteSpace(result.Artist) ? "unknown_artist" : result.Artist;
        string title = string.IsNullOrWhiteSpace(result.Title) ? result.Chart.songName : result.Title;
        return BuildSongGroupId(artist, title);
    }

    private static string BuildSongGroupId(string artist, string title)
    {
        string artistPart = string.IsNullOrWhiteSpace(artist) ? "unknown_artist" : artist;
        string titlePart = string.IsNullOrWhiteSpace(title) ? "unknown_song" : title;
        return $"{artistPart}_{titlePart}";
    }

    private static string BuildSongTitleFromSongData(SongData song)
    {
        return song != null && !string.IsNullOrWhiteSpace(song.SongTitle) ? song.SongTitle : song != null ? song.name : string.Empty;
    }

    private void SyncOpenScene(ChartData chart, AudioClip clip, string runtimeChartFileName, bool preview, bool prepareSpawner)
    {
        ChartGeneratorTool tool = FindFirstObjectByType<ChartGeneratorTool>();
        if (tool != null)
        {
            SerializedObject serializedTool = new SerializedObject(tool);
            serializedTool.FindProperty("saveFileName").stringValue = runtimeChartFileName;

            if (clip != null)
            {
                SerializedProperty musicSourceProperty = serializedTool.FindProperty("musicSource");
                AudioSource source = musicSourceProperty.objectReferenceValue as AudioSource;
                if (source != null)
                {
                    Undo.RecordObject(source, "Import Song Audio");
                    source.clip = clip;
                    EditorUtility.SetDirty(source);
                }
            }

            serializedTool.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tool);

            if (preview)
                tool.PreviewChart(chart);
        }

        if (prepareSpawner)
        {
            ChartNoteSpawner spawner = FindFirstObjectByType<ChartNoteSpawner>();
            if (spawner != null)
            {
                Undo.RecordObject(spawner, "Import Runtime Chart");
                spawner.SetChartFileName(runtimeChartFileName);
                EditorUtility.SetDirty(spawner);
            }
        }

        if (tool != null || prepareSpawner)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }
}
#endif
