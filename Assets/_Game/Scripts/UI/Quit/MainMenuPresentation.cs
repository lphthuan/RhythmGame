using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>Applies a readable landscape composition while preserving the existing main-menu buttons and their events.</summary>
public class MainMenuPresentation : MonoBehaviour
{
    [SerializeField] private Sprite mainMenuBackground;
    private UIManager _settingsManager;
    private Transform _menuCanvas;

    private void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        _menuCanvas = canvas.transform;
        if (canvas.GetComponent<StartMenuSettingsBinder>() == null)
            canvas.gameObject.AddComponent<StartMenuSettingsBinder>();

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        BuildBackground(canvas.transform);
        ArrangeExistingButtons(canvas.transform);
        BindSettingsButton(canvas.transform);
    }

    private void Start()
    {
        if (_menuCanvas == null)
            return;

        BindSettingsButton(_menuCanvas);
        if (_settingsManager != null)
            _settingsManager.PrepareAsSettingsOverlay();
    }

    private void BuildBackground(Transform canvas)
    {
        Transform old = canvas.Find("RG Main Menu Background");
        if (old != null)
            Destroy(old.gameObject);

        GameObject root = new("RG Main Menu Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(canvas, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.SetAsFirstSibling();
        Image image = root.GetComponent<Image>();
        image.sprite = mainMenuBackground;
        image.color = mainMenuBackground != null ? Color.white : new Color(0.10f, 0.07f, 0.18f, 1f);
        image.preserveAspect = false;
        image.raycastTarget = false;

        GameObject shade = new("Main Menu Shade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform shadeRect = shade.GetComponent<RectTransform>();
        shadeRect.SetParent(rect, false);
        shadeRect.anchorMin = Vector2.zero;
        shadeRect.anchorMax = Vector2.one;
        shadeRect.sizeDelta = Vector2.zero;
        Image shadeImage = shade.GetComponent<Image>();
        shadeImage.color = new Color(0.03f, 0.02f, 0.08f, 0.34f);
        shadeImage.raycastTarget = false;
    }

    private static void ArrangeExistingButtons(Transform root)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        int visualIndex = 0;
        foreach (Button button in buttons)
        {
            if (button == null || button.name.Contains("RG "))
                continue;

            string name = button.name.ToLowerInvariant();
            if (!name.Contains("start") && !name.Contains("setting") && !name.Contains("exit"))
                continue;

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null)
                continue;

            rect.anchorMin = rect.anchorMax = new Vector2(0.31f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 92f - visualIndex * 92f);
            rect.sizeDelta = new Vector2(300f, 64f);
            visualIndex++;
        }
    }

    private void BindSettingsButton(Transform menuCanvas)
    {
        UIManager[] managers = FindObjectsByType<UIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (UIManager manager in managers)
        {
            if (manager != null && manager.gameObject.name == "CanvasThai")
            {
                _settingsManager = manager;
                break;
            }
        }

        if (_settingsManager == null)
            return;

        List<Button> candidates = new();
        foreach (Button button in menuCanvas.GetComponentsInChildren<Button>(true))
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            string buttonName = button.name.ToLowerInvariant();
            string labelText = label != null ? label.text.ToLowerInvariant() : string.Empty;
            if (buttonName.Contains("setting") || labelText.Contains("setting"))
                candidates.Add(button);
        }

        if (candidates.Count == 0)
        {
            foreach (Button button in menuCanvas.GetComponentsInChildren<Button>(true))
            {
                if (button.transform.parent != null && button.transform.parent.name == "Panel")
                    candidates.Add(button);
            }
            candidates.Sort((left, right) => right.GetComponent<RectTransform>().anchoredPosition.y.CompareTo(left.GetComponent<RectTransform>().anchoredPosition.y));
            if (candidates.Count >= 3)
            {
                Button settingsButton = candidates[1];
                ReplaceWithSettingsAction(settingsButton);
            }
            return;
        }

        foreach (Button button in candidates)
        {
            ReplaceWithSettingsAction(button);
        }
    }

    private void ReplaceWithSettingsAction(Button button)
    {
        if (button == null)
            return;

        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(OpenSettings);

        StartMenuSettingsButton directClick = button.GetComponent<StartMenuSettingsButton>();
        if (directClick == null)
            directClick = button.gameObject.AddComponent<StartMenuSettingsButton>();
        directClick.Configure(_settingsManager);
    }

    private void OpenSettings()
    {
        if (_settingsManager == null && _menuCanvas != null)
            BindSettingsButton(_menuCanvas);

        if (_settingsManager == null)
            return;

        _settingsManager.PrepareAsSettingsOverlay();
        _settingsManager.OpenSettings();
    }
}
