#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChartGeneratorTool))]
public class ChartGeneratorToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ChartGeneratorTool tool = (ChartGeneratorTool)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Auto Detect BPM"))
        {
            tool.AutoDetectBpm();
        }

        GUILayout.Space(4);

        if (GUILayout.Button("Generate And Save Chart"))
        {
            tool.GenerateAndSave();
        }
        if (GUILayout.Button("Load Preview Chart"))
        {
            tool.LoadAndPreview();
        }
        if (GUILayout.Button("Save Edited Chart"))
        {
            tool.SaveEditedChart();
        }
    }
}
#endif