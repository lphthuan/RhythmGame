#if UNITY_EDITOR
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
    private int mp3LaneCount = 4;
    private bool mp3CreateTimelineAsset = true;
    private bool mp3CreateOrUpdateSongData = true;
    private bool mp3PreviewInOpenTool = true;
    private bool mp3PrepareRuntimeSpawner = true;

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
        mp3LaneCount = Mathf.Clamp(EditorGUILayout.IntField("Lane Count", mp3LaneCount), 1, 8);
        mp3CreateTimelineAsset = EditorGUILayout.Toggle("Create Timeline Asset", mp3CreateTimelineAsset);
        mp3CreateOrUpdateSongData = EditorGUILayout.Toggle("Create/Update SongData Hub", mp3CreateOrUpdateSongData);
        mp3PreviewInOpenTool = EditorGUILayout.Toggle("Preview In Open Tool", mp3PreviewInOpenTool);
        mp3PrepareRuntimeSpawner = EditorGUILayout.Toggle("Prepare Runtime Spawner", mp3PrepareRuntimeSpawner);

        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(mp3FilePath)))
        {
            if (GUILayout.Button("Generate From MP3", GUILayout.Height(30f)))
                ImportMp3AutoChart();
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

        AudioClip importedClip = copyAudioToProject ? ImportAudio(result) : null;
        Sprite importedCover = copyCoverToProject ? ImportCover(result) : null;
        Difficulty selectedDifficulty = autoDetectDifficulty ? GuessDifficulty(result.Version) : difficultySlot;
        RhythmTimelineAsset timeline = createTimelineAsset ? CreateTimeline(result.Chart, BuildSongGroupId(result), importedClip, selectedDifficulty) : null;

        SongData songData = null;
        if (createOrUpdateSongData)
        {
            songData = CreateOrUpdateSongData(
                BuildSongTitle(result),
                BuildSongGroupId(result),
                result.Chart.bpm,
                importedClip,
                importedCover,
                chartFileName,
                timeline,
                selectedDifficulty,
                targetSongData);
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

        AudioClip clip = ImportAudioFile(mp3FilePath);
        if (clip == null)
        {
            EditorUtility.DisplayDialog("MP3 Import Failed", "MP3 was copied but Unity could not load it as AudioClip.", "OK");
            return;
        }

        float bpm = mp3AutoDetectBpm ? BpmDetector.Detect(clip) : Mathf.Max(1f, mp3Bpm);
        mp3Bpm = bpm;

        string title = string.IsNullOrWhiteSpace(mp3SongTitle)
            ? Path.GetFileNameWithoutExtension(mp3FilePath)
            : mp3SongTitle.Trim();
        string groupId = BuildSongGroupId(mp3Artist, title);
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
            ChartData chart = SimpleChartGenerator.Generate(
                title,
                bpm,
                clip.length,
                mp3ChartOffsetSeconds,
                mp3LaneCount,
                preset);

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
                    songData != null ? songData : mp3TargetSongData);
            }

            lastChart = chart;
            lastChartName = generatedChartFileName;
            totalNotes += chart.notes.Count;
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

    private static string BuildChartFileName(OsuManiaBeatmapParser.ImportResult result)
    {
        string source = !string.IsNullOrWhiteSpace(result.AudioFileName)
            ? Path.GetFileNameWithoutExtension(result.AudioFileName)
            : result.Chart.songName;

        string difficulty = GetDifficultyFileSuffix(GuessDifficulty(result.Version));
        return "chart_" + SongData.SanitizeForFileName(source) + "_" + difficulty;
    }

    private static AudioClip ImportAudio(OsuManiaBeatmapParser.ImportResult result)
    {
        if (string.IsNullOrWhiteSpace(result.AudioFilePath) || !File.Exists(result.AudioFilePath))
        {
            Debug.LogWarning($"SongImportWindow: Audio file not found: {result.AudioFilePath}");
            return null;
        }

        return ImportAudioFile(result.AudioFilePath);
    }

    private static AudioClip ImportAudioFile(string sourcePath)
    {
        EnsureFolder(MusicFolder);

        string extension = Path.GetExtension(sourcePath);
        string safeName = SongData.SanitizeForFileName(Path.GetFileNameWithoutExtension(sourcePath));
        string targetPath = AssetDatabase.GenerateUniqueAssetPath($"{MusicFolder}/{safeName}{extension}");

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
        SongData explicitTarget)
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
        song.unlockType = SongUnlockType.Free;
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
