using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(500)]
public class StartMenuSettingsBinder : MonoBehaviour
{
    [SerializeField] private UIManager settingsManager;
    [SerializeField] private Button settingsButton;

    private void Awake()
    {
        Bind();
    }

    private void Start()
    {
        Bind();
    }

    private void OnEnable()
    {
        Bind();
    }

    private void Bind()
    {
        if (settingsManager == null)
            settingsManager = FindCanvasThaiManager();
        if (settingsManager == null)
            return;

        settingsManager.PrepareAsSettingsOverlay();

        if (settingsButton == null)
            settingsButton = FindSettingsButton();
        if (settingsButton == null)
            return;

        settingsButton.onClick = new Button.ButtonClickedEvent();
        settingsButton.onClick.AddListener(OpenSettings);

        StartMenuSettingsButton directClick = settingsButton.GetComponent<StartMenuSettingsButton>();
        if (directClick == null)
            directClick = settingsButton.gameObject.AddComponent<StartMenuSettingsButton>();
        directClick.Configure(settingsManager);
    }

    private void OpenSettings()
    {
        if (settingsManager == null)
            settingsManager = FindCanvasThaiManager();
        if (settingsManager == null)
            return;

        settingsManager.PrepareAsSettingsOverlay();
        settingsManager.OpenSettings();
    }

    private static UIManager FindCanvasThaiManager()
    {
        UIManager[] managers = FindObjectsByType<UIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (UIManager manager in managers)
        {
            if (manager != null && manager.gameObject.name == "CanvasThai")
                return manager;
        }

        return null;
    }

    private Button FindSettingsButton()
    {
        Button bestBySprite = null;
        Button bestByPosition = null;
        float bestPositionScore = float.PositiveInfinity;
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            Image image = button.GetComponent<Image>();
            string spriteName = image != null && image.sprite != null ? image.sprite.name.ToLowerInvariant() : string.Empty;
            if (spriteName.Contains("setting"))
                bestBySprite = button;

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null || button.transform.parent == null || button.transform.parent.name != "Panel")
                continue;

            float score = Mathf.Abs(rect.anchoredPosition.y);
            if (score < bestPositionScore)
            {
                bestPositionScore = score;
                bestByPosition = button;
            }
        }

        return bestBySprite != null ? bestBySprite : bestByPosition;
    }
}
