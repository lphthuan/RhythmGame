using UnityEngine;
using UnityEngine.UI;

/// <summary>Connects a copied CanvasThai settings popup to its local UIManager at runtime.</summary>
public class SettingsCanvasBridge : MonoBehaviour
{
    private void Awake()
    {
        UIManager manager = GetComponent<UIManager>();
        if (manager == null)
            return;

        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button == null || button.name != "Btn_Done")
                continue;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(manager.CloseSettingsAndSave);
        }
    }
}
