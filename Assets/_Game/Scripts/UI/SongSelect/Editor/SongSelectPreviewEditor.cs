using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SongSelectPreviewEditor
{
    private const string BuildMenu = "Tools/Rhythm Game/Song Select/Build Edit Mode Preview";
    private const string ClearMenu = "Tools/Rhythm Game/Song Select/Clear Edit Mode Preview";
    private const string SelectTopBarMenu = "Tools/Rhythm Game/Song Select/Select Top Bar";
    private const string SelectSettingsMenu = "Tools/Rhythm Game/Song Select/Select Settings Button";
    private const string SelectCardsMenu = "Tools/Rhythm Game/Song Select/Select Song Cards";
    private const string SelectFirstCardMenu = "Tools/Rhythm Game/Song Select/Select First Song Card Template";
    private const string SavePrefabMenu = "Tools/Rhythm Game/Song Select/Save Preview As Prefab";

    [MenuItem(BuildMenu)]
    public static void BuildPreview()
    {
        SongListManager manager = FindManager();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Song Select Preview", "Open the SongSelect scene first, then try again.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build Song Select Edit Mode Preview");
        manager.BuildEditModePreview();
        MarkSceneDirty(manager.gameObject);
        SelectChild(manager, "RG Generated Song Select");
    }

    [MenuItem(ClearMenu)]
    public static void ClearPreview()
    {
        SongListManager manager = FindManager();
        if (manager == null)
            return;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Clear Song Select Edit Mode Preview");
        manager.ClearEditModePreview();
        MarkSceneDirty(manager.gameObject);
    }

    [MenuItem(SelectTopBarMenu)]
    public static void SelectTopBar() => SelectGeneratedChild("Top Bar");

    [MenuItem(SelectSettingsMenu)]
    public static void SelectSettingsButton() => SelectGeneratedChild("Top Bar/Settings");

    [MenuItem(SelectCardsMenu)]
    public static void SelectSongCards() => SelectGeneratedChild("Song Carousel/Viewport/Content");

    [MenuItem(SelectFirstCardMenu)]
    public static void SelectFirstSongCard()
    {
        SongListManager manager = FindManager();
        if (manager == null)
            return;

        Canvas canvas = manager.GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        Transform content = canvas != null ? canvas.transform.Find("RG Generated Song Select/Song Carousel/Viewport/Content") : null;
        if (content == null || content.childCount == 0)
        {
            EditorUtility.DisplayDialog("Song Select Preview", "Build the preview first, then try again.", "OK");
            return;
        }

        Transform target = content.Find("Song Card Template") ?? content.GetChild(0);
        Selection.activeGameObject = target.gameObject;
        EditorGUIUtility.PingObject(target.gameObject);
    }

    [MenuItem(SavePrefabMenu)]
    public static void SavePreviewAsPrefab()
    {
        SongListManager manager = FindManager();
        if (manager == null)
            return;

        Canvas canvas = manager.GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        Transform root = canvas != null ? canvas.transform.Find("RG Generated Song Select") : null;
        if (root == null)
        {
            EditorUtility.DisplayDialog("Song Select Preview", "Build the preview before saving it as a prefab.", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Song Select Preview Prefab",
            "SongSelectEditableLayout",
            "prefab",
            "Choose where to save the editable Song Select layout prefab.",
            "Assets/_Game/Prefabs/UI");
        if (string.IsNullOrEmpty(path))
            return;

        PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Song Select Preview", "Saved editable layout prefab:\n" + path, "OK");
    }

    private static SongListManager FindManager()
    {
        if (Selection.activeGameObject != null)
        {
            SongListManager selected = Selection.activeGameObject.GetComponentInParent<SongListManager>();
            if (selected != null)
                return selected;
        }

        return Object.FindFirstObjectByType<SongListManager>(FindObjectsInactive.Include);
    }

    private static void SelectGeneratedChild(string relativePath)
    {
        SongListManager manager = FindManager();
        if (manager == null)
            return;

        SelectChild(manager, "RG Generated Song Select/" + relativePath);
    }

    private static void SelectChild(SongListManager manager, string path)
    {
        Canvas canvas = manager.GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
            return;

        Transform target = canvas.transform.Find(path);
        if (target == null)
        {
            EditorUtility.DisplayDialog("Song Select Preview", $"Could not find '{path}'. Build the preview first.", "OK");
            return;
        }

        Selection.activeGameObject = target.gameObject;
        EditorGUIUtility.PingObject(target.gameObject);
    }

    private static void MarkSceneDirty(GameObject sceneObject)
    {
        if (sceneObject == null)
            return;

        EditorSceneManager.MarkSceneDirty(sceneObject.scene);
    }
}
