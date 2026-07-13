#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Dypsloom.RhythmTimeline.Core;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;

[CustomEditor(typeof(SongData))]
public class SongDataEditor : Editor
{
    private const float CoverPreviewHeight = 145f;
    private static readonly Color GoodColor = new(0.18f, 0.48f, 0.28f, 1f);
    private static readonly Color WarningColor = new(0.82f, 0.52f, 0.16f, 1f);
    private static readonly Color MissingColor = new(0.56f, 0.16f, 0.18f, 1f);

    private SerializedProperty _songTitle;
    private SerializedProperty _sceneName;
    private SerializedProperty _previewImage;
    private SerializedProperty _bpm;
    private SerializedProperty _legacyDifficulty;
    private SerializedProperty _songGroupId;
    private SerializedProperty _difficultyLevel;
    private SerializedProperty _unlockType;
    private SerializedProperty _chartFileName;
    private SerializedProperty _audioClip;
    private SerializedProperty _legacyTimeline;

    private void OnEnable()
    {
        _songTitle = serializedObject.FindProperty("_songTitle");
        _sceneName = serializedObject.FindProperty("_sceneName");
        _previewImage = serializedObject.FindProperty("_previewImage");
        _bpm = serializedObject.FindProperty("_bpm");
        _legacyDifficulty = serializedObject.FindProperty("_difficulty");
        _songGroupId = serializedObject.FindProperty("songGroupId");
        _difficultyLevel = serializedObject.FindProperty("difficultyLevel");
        _unlockType = serializedObject.FindProperty("unlockType");
        _chartFileName = serializedObject.FindProperty("chartFileName");
        _audioClip = serializedObject.FindProperty("audioClip");
        _legacyTimeline = serializedObject.FindProperty("timelineAsset");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SongData song = (SongData)target;

        DrawHeader(song);
        DrawPresentation(song);
        DrawIdentityTools(song);
        DrawDifficultyDashboard(song);
        DrawLegacyCompatibility();
        DrawValidationTools(song);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeader(SongData song)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("SongData Hub", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(song.SongTitle) ? song.name : song.SongTitle);

            ValidationReport report = Validate(song);
            MessageType messageType = report.HasErrors ? MessageType.Error : report.HasWarnings ? MessageType.Warning : MessageType.Info;
            EditorGUILayout.HelpBox(report.Summary, messageType);
        }
    }

    private void DrawPresentation(SongData song)
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Song Presentation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_songTitle, new GUIContent("Song Title"));
        EditorGUILayout.PropertyField(_sceneName, new GUIContent("Gameplay Scene"));
        EditorGUILayout.PropertyField(_previewImage, new GUIContent("Cover Image"));
        DrawCoverPreview(song);
        EditorGUILayout.PropertyField(_audioClip, new GUIContent("Audio Clip"));
        EditorGUILayout.PropertyField(_bpm, new GUIContent("BPM"));
    }

    private void DrawIdentityTools(SongData song)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Save Identity", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_songGroupId, new GUIContent("Song Group Id"));
        EditorGUILayout.PropertyField(_unlockType);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Normalize songGroupId"))
                NormalizeSongGroupId(song);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(song.songGroupId)))
            {
                if (GUILayout.Button("Copy Id"))
                    EditorGUIUtility.systemCopyBuffer = song.songGroupId;
            }
        }

        EditorGUILayout.HelpBox("songGroupId là khóa gom 3 difficulty và lưu best score. Nên dùng lowercase_underscore và giữ nguyên sau khi public bài.", MessageType.None);
    }

    private void DrawDifficultyDashboard(SongData song)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Difficulty Charts", EditorStyles.boldLabel);
        DrawDifficultyRow(song, Difficulty.Easy, "Easy", "easyChartFileName", "easyTimelineAsset");
        DrawDifficultyRow(song, Difficulty.Medium, "Normal", "normalChartFileName", "normalTimelineAsset");
        DrawDifficultyRow(song, Difficulty.Hard, "Hard", "hardChartFileName", "hardTimelineAsset");
    }

    private void DrawDifficultyRow(
        SongData song,
        Difficulty difficulty,
        string label,
        string chartPropertyName,
        string timelinePropertyName)
    {
        SerializedProperty chartProperty = serializedObject.FindProperty(chartPropertyName);
        SerializedProperty timelineProperty = serializedObject.FindProperty(timelinePropertyName);
        RhythmTimelineAsset explicitTimeline = timelineProperty.objectReferenceValue as RhythmTimelineAsset;
        RhythmTimelineAsset timeline = GetInspectorTimeline(song, difficulty, explicitTimeline);
        string explicitChart = chartProperty.stringValue;
        string resolvedChart = GetInspectorChartName(song, difficulty, explicitChart);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(78f));
                DrawStatusPill(GetStatusText(resolvedChart, timeline), GetStatusColor(resolvedChart, timeline), GUILayout.Width(120f));
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(timeline == null))
                {
                    if (GUILayout.Button("Open", GUILayout.Width(56f)))
                        OpenTimeline(timeline, song.audioClip);

                    if (GUILayout.Button("Sync JSON", GUILayout.Width(84f)))
                        SyncDifficultyFromTimeline(song, difficulty, label, chartProperty, timeline);

                    if (GUILayout.Button("Ping", GUILayout.Width(48f)))
                        PingObject(timeline);
                }

                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(resolvedChart)))
                {
                    if (GUILayout.Button("Build TL", GUILayout.Width(68f)))
                        BuildTimelineFromChart(song, difficulty, label, resolvedChart, timelineProperty);
                }
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(chartProperty, new GUIContent("Chart JSON"));
            EditorGUILayout.PropertyField(timelineProperty, new GUIContent("Timeline Asset"));
            EditorGUI.indentLevel--;

            DrawDifficultyHint(song, difficulty, explicitChart, resolvedChart, timeline);
        }
    }

    private void DrawLegacyCompatibility()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Legacy Compatibility", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_difficultyLevel, new GUIContent("Default Difficulty"));
        EditorGUILayout.PropertyField(_legacyDifficulty, new GUIContent("Legacy Difficulty Text"));
        EditorGUILayout.PropertyField(_chartFileName, new GUIContent("Legacy Chart JSON"));
        EditorGUILayout.PropertyField(_legacyTimeline, new GUIContent("Legacy Timeline"));
        EditorGUILayout.HelpBox("Các field legacy vẫn được giữ để không làm hỏng asset cũ. Flow mới nên gắn chart/timeline vào Easy/Normal/Hard ở trên.", MessageType.None);
    }

    private void DrawValidationTools(SongData song)
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Validate SongData"))
                ShowValidationDialog(song);

            if (GUILayout.Button("Normalize + Validate"))
            {
                NormalizeSongGroupId(song);
                ShowValidationDialog(song);
            }
        }
    }

    private static void DrawDifficultyHint(SongData song, Difficulty difficulty, string explicitChartName, string chartName, RhythmTimelineAsset timeline)
    {
        bool hasChart = !string.IsNullOrWhiteSpace(chartName);
        bool hasTimeline = timeline != null;

        if (hasChart && hasTimeline)
        {
            string fallbackNote = string.IsNullOrWhiteSpace(explicitChartName) ? " (using legacy fallback)" : string.Empty;
            EditorGUILayout.HelpBox($"{difficulty} ready: chart '{chartName}' + timeline '{timeline.name}'{fallbackNote}.", MessageType.None);
            return;
        }

        if (!hasChart && !hasTimeline)
        {
            EditorGUILayout.HelpBox($"{difficulty} chưa có chart hoặc timeline.", MessageType.Warning);
            return;
        }

        if (!hasChart)
            EditorGUILayout.HelpBox($"{difficulty} có timeline nhưng chưa có chart JSON. Gameplay runtime cần chart JSON để spawn note.", MessageType.Warning);
        else
        {
            string fallbackNote = string.IsNullOrWhiteSpace(explicitChartName) ? " Chart này đang lấy từ legacy fallback." : string.Empty;
            EditorGUILayout.HelpBox($"{difficulty} có chart JSON nhưng chưa có timeline để edit trực quan.{fallbackNote}", MessageType.Info);
        }
    }

    private static void DrawStatusPill(string text, Color color, params GUILayoutOption[] options)
    {
        GUIStyle style = new(EditorStyles.miniButton)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        Color oldColor = GUI.backgroundColor;
        GUI.backgroundColor = color;
        GUILayout.Label(text, style, options);
        GUI.backgroundColor = oldColor;
    }

    private static string GetStatusText(string chartName, RhythmTimelineAsset timeline)
    {
        bool hasChart = !string.IsNullOrWhiteSpace(chartName);
        bool hasTimeline = timeline != null;
        if (hasChart && hasTimeline) return "READY";
        if (hasChart || hasTimeline) return "PARTIAL";
        return "MISSING";
    }

    private static Color GetStatusColor(string chartName, RhythmTimelineAsset timeline)
    {
        bool hasChart = !string.IsNullOrWhiteSpace(chartName);
        bool hasTimeline = timeline != null;
        if (hasChart && hasTimeline) return GoodColor;
        if (hasChart || hasTimeline) return WarningColor;
        return MissingColor;
    }

    private static void DrawCoverPreview(SongData song)
    {
        if (song == null || song.PreviewImage == null || song.PreviewImage.texture == null)
            return;

        Rect previewRect = GUILayoutUtility.GetRect(0f, CoverPreviewHeight, GUILayout.ExpandWidth(true));
        GUI.Box(previewRect, GUIContent.none);

        Texture2D texture = song.PreviewImage.texture;
        Rect spriteRect = song.PreviewImage.textureRect;
        Rect uv = new(
            spriteRect.x / texture.width,
            spriteRect.y / texture.height,
            spriteRect.width / texture.width,
            spriteRect.height / texture.height);

        GUI.DrawTextureWithTexCoords(previewRect, texture, uv, true);
        EditorGUILayout.HelpBox("Cover chỉ bị crop/xoay trong giao diện chọn nhạc; file ảnh gốc không bị chỉnh sửa.", MessageType.None);
    }

    private static void NormalizeSongGroupId(SongData song)
    {
        if (song == null)
            return;

        string source = !string.IsNullOrWhiteSpace(song.songGroupId)
            ? song.songGroupId
            : !string.IsNullOrWhiteSpace(song.SongTitle)
                ? song.SongTitle
                : song.audioClip != null
                    ? song.audioClip.name
                    : song.name;

        string normalized = SongData.SanitizeForFileName(source);
        if (song.songGroupId == normalized)
            return;

        Undo.RecordObject(song, "Normalize Song Group Id");
        song.songGroupId = normalized;
        EditorUtility.SetDirty(song);
        AssetDatabase.SaveAssets();
    }

    private void SyncDifficultyFromTimeline(
        SongData song,
        Difficulty difficulty,
        string label,
        SerializedProperty chartProperty,
        RhythmTimelineAsset timeline)
    {
        if (song == null || timeline == null)
            return;

        serializedObject.ApplyModifiedProperties();

        string chartFileName = BuildDifficultyChartFileName(song, difficulty);
        string currentChartFileName = chartProperty.stringValue;

        if (!string.IsNullOrWhiteSpace(currentChartFileName) && currentChartFileName != chartFileName)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Replace Chart JSON Name?",
                $"{label} is currently linked to '{currentChartFileName}'.\n\nSync will export the selected timeline to '{chartFileName}.json' and update this difficulty to use it.",
                "Sync",
                "Cancel");

            if (!overwrite)
                return;
        }

        ChartData chart = ChartTimelineConverter.ExportTimelineToChart(timeline);
        if (chart == null)
        {
            EditorUtility.DisplayDialog("Sync Failed", "Could not export chart data from this timeline.", "OK");
            return;
        }

        chart.songName = string.IsNullOrWhiteSpace(song.SongTitle) ? timeline.name : song.SongTitle;
        if (chart.bpm <= 0f)
            chart.bpm = song._bpm > 0f ? song._bpm : 120f;

        ChartSaveLoad.Save(chart, chartFileName);

        Undo.RecordObject(song, "Sync Difficulty Chart From Timeline");
        serializedObject.Update();
        chartProperty.stringValue = chartFileName;

        if (_bpm != null && chart.bpm > 0f)
            _bpm.floatValue = chart.bpm;

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(song);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Sync Complete",
            $"{label} now uses '{chartFileName}.json'.\n\nExported {chart.notes.Count} note(s), {chart.laneCount} lane(s), BPM {chart.bpm:0.###}.",
            "OK");
    }

    private static string BuildDifficultyChartFileName(SongData song, Difficulty difficulty)
    {
        string source = !string.IsNullOrWhiteSpace(song.songGroupId)
            ? song.songGroupId
            : !string.IsNullOrWhiteSpace(song.SongTitle)
                ? song.SongTitle
                : song.audioClip != null
                    ? song.audioClip.name
                    : song.name;

        string groupId = SongData.SanitizeForFileName(source);
        string suffix = difficulty == Difficulty.Medium ? "normal" : difficulty.ToString().ToLowerInvariant();
        return $"chart_{groupId}_{suffix}";
    }

    private void BuildTimelineFromChart(
        SongData song,
        Difficulty difficulty,
        string label,
        string chartFileName,
        SerializedProperty timelineProperty)
    {
        ChartData chart = ChartSaveLoad.Load(chartFileName);
        if (chart == null)
        {
            EditorUtility.DisplayDialog("Build Timeline Failed", $"Could not load '{chartFileName}.json' from persistentDataPath.", "OK");
            return;
        }

        if (timelineProperty.objectReferenceValue != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Replace Timeline Asset?",
                $"{label} already has a timeline assigned.\n\nBuild TL will create a new timeline from '{chartFileName}.json' and assign it to this difficulty.",
                "Build",
                "Cancel");

            if (!replace)
                return;
        }

        string safeGroupId = SongData.SanitizeForFileName(!string.IsNullOrWhiteSpace(song.songGroupId) ? song.songGroupId : song.name);
        string suffix = difficulty == Difficulty.Medium ? "normal" : difficulty.ToString().ToLowerInvariant();
        string folder = "Assets/_Game/Data/Timelines/OsuImported";
        EnsureFolder(folder);
        string path = $"{folder}/{safeGroupId}_{suffix}_timeline.asset";
        RhythmTimelineAsset timeline = ChartTimelineConverter.CreateTimelineFromChart(chart, path, song.audioClip);

        if (timeline == null)
        {
            EditorUtility.DisplayDialog("Build Timeline Failed", "Timeline converter returned null.", "OK");
            return;
        }

        Undo.RecordObject(song, "Build Timeline From Chart");
        serializedObject.Update();
        timelineProperty.objectReferenceValue = timeline;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(song);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Timeline Built", $"{label} now uses timeline:\n{AssetDatabase.GetAssetPath(timeline)}", "OK");
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

    private static void ShowValidationDialog(SongData song)
    {
        ValidationReport report = Validate(song);
        string body = report.ToDialogText();
        EditorUtility.DisplayDialog(report.HasErrors ? "SongData Has Errors" : report.HasWarnings ? "SongData Has Warnings" : "SongData Ready", body, "OK");
    }

    private static ValidationReport Validate(SongData song)
    {
        ValidationReport report = new();
        if (song == null)
        {
            report.Errors.Add("SongData is null.");
            return report;
        }

        if (string.IsNullOrWhiteSpace(song.SongTitle))
            report.Errors.Add("Missing Song Title.");
        if (string.IsNullOrWhiteSpace(song.SceneName))
            report.Warnings.Add("Gameplay Scene is empty.");
        if (song.audioClip == null)
            report.Errors.Add("Missing Audio Clip.");
        if (song.PreviewImage == null)
            report.Warnings.Add("Missing Cover Image.");
        if (string.IsNullOrWhiteSpace(song.songGroupId))
            report.Errors.Add("Missing songGroupId.");
        else if (song.songGroupId != SongData.SanitizeForFileName(song.songGroupId))
            report.Warnings.Add("songGroupId is not normalized. Use lowercase_underscore.");

        ValidateDifficulty(song, Difficulty.Easy, "Easy", report);
        ValidateDifficulty(song, Difficulty.Medium, "Normal", report);
        ValidateDifficulty(song, Difficulty.Hard, "Hard", report);

        if (!HasAnyDifficulty(song))
            report.Errors.Add("No difficulty has chart/timeline data.");

        return report;
    }

    private static void ValidateDifficulty(SongData song, Difficulty difficulty, string label, ValidationReport report)
    {
        string chartName = GetInspectorChartName(song, difficulty);
        RhythmTimelineAsset timeline = GetInspectorTimeline(song, difficulty);
        bool hasChart = !string.IsNullOrWhiteSpace(chartName);
        bool hasTimeline = timeline != null;

        if (!hasChart && !hasTimeline)
            return;

        if (!hasChart)
            report.Errors.Add($"{label}: missing chart JSON.");
        if (!hasTimeline)
            report.Warnings.Add($"{label}: missing timeline asset.");
        if (hasChart && !ChartJsonMayExist(chartName))
            report.Warnings.Add($"{label}: chart JSON '{chartName}.json' was not found in persistentDataPath. This may be fine if it is generated later.");
    }

    private static bool HasAnyDifficulty(SongData song)
    {
        return HasDifficulty(song, Difficulty.Easy) || HasDifficulty(song, Difficulty.Medium) || HasDifficulty(song, Difficulty.Hard);
    }

    private static bool HasDifficulty(SongData song, Difficulty difficulty)
    {
        return !string.IsNullOrWhiteSpace(GetInspectorChartName(song, difficulty)) || GetInspectorTimeline(song, difficulty) != null;
    }

    private static string GetInspectorChartName(SongData song, Difficulty difficulty, string explicitChart = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitChart))
            return explicitChart;

        string difficultyChart = difficulty switch
        {
            Difficulty.Easy => song.easyChartFileName,
            Difficulty.Medium => song.normalChartFileName,
            Difficulty.Hard => song.hardChartFileName,
            _ => string.Empty
        };

        if (!string.IsNullOrWhiteSpace(difficultyChart))
            return difficultyChart;

        bool isLegacyDefaultDifficulty = difficulty == song.difficultyLevel || difficulty == Difficulty.Medium;
        if (isLegacyDefaultDifficulty && !string.IsNullOrWhiteSpace(song.chartFileName))
            return song.chartFileName;

        return string.Empty;
    }

    private static RhythmTimelineAsset GetInspectorTimeline(SongData song, Difficulty difficulty, RhythmTimelineAsset explicitTimeline = null)
    {
        if (explicitTimeline != null)
            return explicitTimeline;

        RhythmTimelineAsset difficultyTimeline = difficulty switch
        {
            Difficulty.Easy => song.easyTimelineAsset,
            Difficulty.Medium => song.normalTimelineAsset,
            Difficulty.Hard => song.hardTimelineAsset,
            _ => null
        };

        if (difficultyTimeline != null)
            return difficultyTimeline;

        bool isLegacyDefaultDifficulty = difficulty == song.difficultyLevel || difficulty == Difficulty.Medium;
        return isLegacyDefaultDifficulty ? song.timelineAsset : null;
    }

    private static bool ChartJsonMayExist(string chartName)
    {
        if (string.IsNullOrWhiteSpace(chartName))
            return false;

        string path = Path.Combine(Application.persistentDataPath, chartName + ".json");
        return File.Exists(path);
    }

    private static void OpenTimeline(RhythmTimelineAsset timeline, AudioClip audioClip)
    {
        if (timeline == null)
            return;

        ChartTimelineAudioPreviewSetup.Setup(timeline, audioClip, showDialog: false);
        Selection.activeObject = timeline;
        EditorGUIUtility.PingObject(timeline);
        TimelineEditor.Refresh(RefreshReason.ContentsModified | RefreshReason.WindowNeedsRedraw);
    }

    private static void PingObject(Object targetObject)
    {
        if (targetObject == null)
            return;

        Selection.activeObject = targetObject;
        EditorGUIUtility.PingObject(targetObject);
    }

    private sealed class ValidationReport
    {
        public readonly List<string> Errors = new();
        public readonly List<string> Warnings = new();

        public bool HasErrors => Errors.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;

        public string Summary
        {
            get
            {
                if (HasErrors) return $"{Errors.Count} error(s), {Warnings.Count} warning(s). Fix errors before using this song.";
                if (HasWarnings) return $"{Warnings.Count} warning(s). Song can work, but should be reviewed.";
                return "Ready. Presentation, identity, and at least one difficulty look valid.";
            }
        }

        public string ToDialogText()
        {
            List<string> lines = new();

            if (Errors.Count > 0)
            {
                lines.Add("Errors:");
                foreach (string error in Errors)
                    lines.Add("- " + error);
            }

            if (Warnings.Count > 0)
            {
                if (lines.Count > 0) lines.Add(string.Empty);
                lines.Add("Warnings:");
                foreach (string warning in Warnings)
                    lines.Add("- " + warning);
            }

            if (lines.Count == 0)
                lines.Add("SongData is ready.");

            return string.Join("\n", lines);
        }
    }
}
#endif
