#if UNITY_EDITOR
using Dypsloom.RhythmTimeline.Core;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;

[CustomEditor(typeof(SongData))]
public class SongDataEditor : Editor
{
    private const float CoverPreviewHeight = 145f;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Song Presentation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_songTitle"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_sceneName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_previewImage"), new GUIContent("Cover Image"));
        DrawCoverPreview((SongData)target);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_bpm"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_difficulty"));

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Timeline", EditorStyles.boldLabel);
        SerializedProperty timelineProperty = serializedObject.FindProperty("timelineAsset");
        EditorGUILayout.PropertyField(timelineProperty, new GUIContent("Timeline Asset"));

        using (new EditorGUI.DisabledScope(timelineProperty.objectReferenceValue == null))
        {
            if (GUILayout.Button("Open Timeline For This Song"))
                OpenTimeline((RhythmTimelineAsset)timelineProperty.objectReferenceValue, ((SongData)target).audioClip);
        }

        if (timelineProperty.objectReferenceValue == null)
            EditorGUILayout.HelpBox("Gán Timeline Asset để mở và edit chart trực tiếp từ SongData này.", MessageType.Info);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Difficulty Charts", EditorStyles.boldLabel);
        DrawDifficultyChart("Easy", "easyChartFileName", "easyTimelineAsset");
        DrawDifficultyChart("Normal", "normalChartFileName", "normalTimelineAsset");
        DrawDifficultyChart("Hard", "hardChartFileName", "hardTimelineAsset");

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Save & Unlock", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("songGroupId"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("difficultyLevel"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("unlockType"));

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Chart Integration", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("chartFileName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("audioClip"));

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawDifficultyChart(string label, string chartPropertyName, string timelinePropertyName)
    {
        SerializedProperty chartProperty = serializedObject.FindProperty(chartPropertyName);
        SerializedProperty timelineProperty = serializedObject.FindProperty(timelinePropertyName);

        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(chartProperty, new GUIContent("Chart File Name"));
        EditorGUILayout.PropertyField(timelineProperty, new GUIContent("Timeline Asset"));
        using (new EditorGUI.DisabledScope(timelineProperty.objectReferenceValue == null))
        {
            if (GUILayout.Button("Open " + label + " Timeline"))
                OpenTimeline((RhythmTimelineAsset)timelineProperty.objectReferenceValue, ((SongData)target).audioClip);
        }
        EditorGUI.indentLevel--;
    }

    private static void DrawCoverPreview(SongData song)
    {
        if (song == null || song.PreviewImage == null || song.PreviewImage.texture == null)
            return;

        Rect previewRect = GUILayoutUtility.GetRect(0f, CoverPreviewHeight, GUILayout.ExpandWidth(true));
        GUI.Box(previewRect, GUIContent.none);

        Texture2D texture = song.PreviewImage.texture;
        Rect spriteRect = song.PreviewImage.textureRect;
        Rect uv = new Rect(
            spriteRect.x / texture.width,
            spriteRect.y / texture.height,
            spriteRect.width / texture.width,
            spriteRect.height / texture.height);

        GUI.DrawTextureWithTexCoords(previewRect, texture, uv, true);
        EditorGUILayout.HelpBox("Cover chỉ bị crop/xoay trong giao diện chọn nhạc; file ảnh gốc không bị chỉnh sửa.", MessageType.None);
    }

    private static void OpenTimeline(RhythmTimelineAsset timeline, AudioClip audioClip)
    {
        if (timeline == null)
            return;

        ChartTimelineAudioPreviewSetup.Setup(timeline, audioClip, showDialog: false);
        Selection.activeObject = timeline;
        TimelineEditor.Refresh(RefreshReason.ContentsModified | RefreshReason.WindowNeedsRedraw);
    }
}
#endif
