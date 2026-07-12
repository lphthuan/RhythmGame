#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RhythmUiThemeInstaller
{
    private const string SongSelectScenePath = "Assets/_Game/Scenes/Sandbox/ttpCuong/SongSelect.unity";
    private const string StartMenuScenePath = "Assets/_Game/Scenes/Sandbox/HaoThongThinh/StartMenu.unity";
    private const string SongSelectBackgroundPath = "Assets/_Game/Sprites/UI/New/Background_SongChoice.png";
    private const string MainMenuBackgroundPath = "Assets/_Game/Sprites/UI/New/BackGroundMainMenu.png";

    [MenuItem("Tools/RhythmGame/UI/Apply New Menu Theme")]
    public static void ApplyTheme()
    {
        Sprite songSelectBackground = ImportAsSprite(SongSelectBackgroundPath);
        Sprite mainMenuBackground = ImportAsSprite(MainMenuBackgroundPath);

        if (songSelectBackground == null || mainMenuBackground == null)
        {
            Debug.LogError("RhythmUiThemeInstaller: missing a UI/New background image.");
            return;
        }

        ApplySongSelect(songSelectBackground);
        ApplyStartMenu(mainMenuBackground);
        InstallSongSelectSettingsCanvas();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("RhythmUiThemeInstaller: applied new SongSelect and StartMenu presentation.");
    }

    private static void ApplySongSelect(Sprite background)
    {
        Scene scene = SceneManager.GetSceneByPath(SongSelectScenePath);
        bool closeScene = !scene.isLoaded;
        if (closeScene)
            scene = EditorSceneManager.OpenScene(SongSelectScenePath, OpenSceneMode.Additive);
        SongListManager manager = FindInScene<SongListManager>(scene);
        if (manager == null)
        {
            Debug.LogError("RhythmUiThemeInstaller: SongListManager was not found in SongSelect.");
        }
        else
        {
            SerializedObject serialized = new SerializedObject(manager);
            serialized.FindProperty("selectScreenBackground").objectReferenceValue = background;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            DisableLegacySongSelectPreview(scene);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.SaveScene(scene);
        }
        if (closeScene)
            EditorSceneManager.CloseScene(scene, true);
    }

    private static void ApplyStartMenu(Sprite background)
    {
        Scene scene = SceneManager.GetSceneByPath(StartMenuScenePath);
        bool closeScene = !scene.isLoaded;
        if (closeScene)
            scene = EditorSceneManager.OpenScene(StartMenuScenePath, OpenSceneMode.Additive);
        Canvas canvas = FindInScene<Canvas>(scene);
        if (canvas == null)
        {
            Debug.LogError("RhythmUiThemeInstaller: Canvas was not found in StartMenu.");
        }
        else
        {
            MainMenuPresentation presentation = canvas.GetComponent<MainMenuPresentation>();
            if (presentation == null)
                presentation = canvas.gameObject.AddComponent<MainMenuPresentation>();

            SerializedObject serialized = new SerializedObject(presentation);
            serialized.FindProperty("mainMenuBackground").objectReferenceValue = background;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presentation);
            EditorSceneManager.SaveScene(scene);
        }
        if (closeScene)
            EditorSceneManager.CloseScene(scene, true);
    }

    private static void InstallSongSelectSettingsCanvas()
    {
        Scene sourceScene = SceneManager.GetSceneByPath(StartMenuScenePath);
        bool closeSourceScene = !sourceScene.isLoaded;
        if (closeSourceScene)
            sourceScene = EditorSceneManager.OpenScene(StartMenuScenePath, OpenSceneMode.Additive);

        Scene targetScene = SceneManager.GetSceneByPath(SongSelectScenePath);
        bool closeTargetScene = !targetScene.isLoaded;
        if (closeTargetScene)
            targetScene = EditorSceneManager.OpenScene(SongSelectScenePath, OpenSceneMode.Additive);

        GameObject sourceCanvas = FindRootByName(sourceScene, "CanvasThai");
        if (sourceCanvas == null)
        {
            Debug.LogError("RhythmUiThemeInstaller: CanvasThai was not found in StartMenu.");
        }
        else
        {
            GameObject previousCanvas = FindRootByName(targetScene, "CanvasThai");
            if (previousCanvas != null)
                Object.DestroyImmediate(previousCanvas);

            GameObject clone = Object.Instantiate(sourceCanvas);
            clone.name = "CanvasThai";
            SceneManager.MoveGameObjectToScene(clone, targetScene);

            Canvas canvas = clone.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 100;
            }

            UIManager manager = clone.GetComponent<UIManager>();
            if (manager != null)
            {
                if (clone.GetComponent<SettingsCanvasBridge>() == null)
                    clone.AddComponent<SettingsCanvasBridge>();

                SerializedObject serialized = new SerializedObject(manager);
                serialized.FindProperty("settingsOverlayOnly").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                RepairSettingsButtons(clone, manager);
            }

            EditorSceneManager.MarkSceneDirty(targetScene);
            EditorSceneManager.SaveScene(targetScene);
        }

        if (closeTargetScene)
            EditorSceneManager.CloseScene(targetScene, true);
        if (closeSourceScene)
            EditorSceneManager.CloseScene(sourceScene, true);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }
        return null;
    }

    private static GameObject FindRootByName(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
                return root;
        }

        return null;
    }

    private static void RepairSettingsButtons(GameObject canvasThai, UIManager manager)
    {
        foreach (Button button in canvasThai.GetComponentsInChildren<Button>(true))
        {
            if (button == null || !button.name.Contains("Btn_Done"))
                continue;

            SerializedObject serializedButton = new SerializedObject(button);
            SerializedProperty calls = serializedButton.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            if (calls != null)
                calls.arraySize = 0;
            serializedButton.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void DisableLegacySongSelectPreview(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour != null && behaviour.GetType().FullName == "Dypsloom.RhythmTimeline.UI.SelectedSongPanel")
                    behaviour.enabled = false;
            }
        }
    }

    private static Sprite ImportAsSprite(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return null;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }
}
#endif
