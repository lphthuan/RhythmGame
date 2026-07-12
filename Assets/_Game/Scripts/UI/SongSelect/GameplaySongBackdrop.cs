using UnityEngine;
using UnityEngine.UI;

/// <summary>Shows the selected song cover behind the gameplay lanes without affecting note input.</summary>
public class GameplaySongBackdrop : MonoBehaviour
{
    private void Awake()
    {
        Apply();
    }

    public static void Apply()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        SongData song = SelectedSongManager.Instance != null ? SelectedSongManager.Instance.SelectedSong : null;
        if (canvas == null || song == null || song.PreviewImage == null)
            return;

        Transform old = canvas.transform.Find("RG Song Gameplay Backdrop");
        if (old != null)
            Destroy(old.gameObject);

        RectTransform root = CreateRect("RG Song Gameplay Backdrop", canvas.transform);
        Stretch(root);
        root.SetAsFirstSibling();

        Image baseColor = root.gameObject.AddComponent<Image>();
        baseColor.color = new Color(0.025f, 0.035f, 0.09f, 1f);
        baseColor.raycastTarget = false;

        Image art = CreateImage("Song Art", root, song.PreviewImage, new Color(1f, 1f, 1f, 0.26f));
        Stretch(art.rectTransform);
        art.preserveAspect = true;
        art.raycastTarget = false;

        Image shade = CreateImage("Dark Overlay", root, null, new Color(0.02f, 0.025f, 0.08f, 0.62f));
        Stretch(shade.rectTransform);
        shade.raycastTarget = false;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject obj = new(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }
}
