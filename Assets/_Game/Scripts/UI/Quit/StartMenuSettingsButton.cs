using UnityEngine;
using UnityEngine.EventSystems;

public class StartMenuSettingsButton : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    private UIManager settingsManager;

    public void Configure(UIManager manager)
    {
        settingsManager = manager;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OpenSettings();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        OpenSettings();
    }

    private void OpenSettings()
    {
        if (settingsManager == null)
            settingsManager = FindCanvasThaiManager();

        if (settingsManager == null)
        {
            Debug.LogWarning("StartMenuSettingsButton: CanvasThai UIManager was not found.");
            return;
        }

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
}
