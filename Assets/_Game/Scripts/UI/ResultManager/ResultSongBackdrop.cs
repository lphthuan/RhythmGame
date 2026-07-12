using UnityEngine;
using TMPro;
using UnityEngine.UI;

public static class ResultSongBackdrop
{
    public static void Apply(GameObject overlay)
    {
        SongData song = SelectedSongManager.Instance != null ? SelectedSongManager.Instance.SelectedSong : null;
        if (overlay == null)
            return;

        RepairLayout(overlay.transform);
        ApplySongInfo(overlay.transform, song);

        if (song == null || song.PreviewImage == null)
            return;

        Image target = FindBackground(overlay.transform);
        if (target != null)
        {
            target.sprite = song.PreviewImage;
            target.type = Image.Type.Simple;
            target.preserveAspect = true;
            target.color = new Color(0.60f, 0.60f, 0.68f, 1f);
        }
    }

    private static void RepairLayout(Transform overlay)
    {
        RectTransform leftSongInfo = FindRect(overlay, "Left_SongInfo");
        if (leftSongInfo != null)
        {
            SetRect(leftSongInfo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(112f, 0f), new Vector2(230f, 255f));
            leftSongInfo.localScale = Vector3.one;
        }

        RectTransform rightScoreBoard = FindRect(overlay, "Right_ScoreBoard");
        if (rightScoreBoard != null)
        {
            SetRect(rightScoreBoard, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-82f, 8f), new Vector2(130f, 105f));
            rightScoreBoard.localScale = Vector3.one;
            RepairComboBadgeChildren(rightScoreBoard);
        }

        RectTransform maxCombo = FindRect(overlay, "MaxComboText");
        if (maxCombo != null)
        {
            SetRect(maxCombo, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-92f, 0f), new Vector2(116f, 46f));
            TextMeshProUGUI comboText = maxCombo.GetComponent<TextMeshProUGUI>();
            if (comboText != null)
            {
                comboText.fontSize = 32f;
                comboText.enableAutoSizing = true;
                comboText.fontSizeMin = 18f;
                comboText.fontSizeMax = 32f;
                comboText.alignment = TextAlignmentOptions.Center;
                comboText.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        SetBottomButton(overlay, "Back", new Vector2(38f, 24f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        SetBottomButton(overlay, "Retry", new Vector2(0f, 24f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        SetBottomButton(overlay, "Share", new Vector2(-38f, 24f), new Vector2(1f, 0f), new Vector2(1f, 0f));
    }

    private static void ApplySongInfo(Transform overlay, SongData song)
    {
        RectTransform leftSongInfo = FindRect(overlay, "Left_SongInfo");
        if (leftSongInfo == null)
            return;

        TextMeshProUGUI title = FindSongTitleText(leftSongInfo);
        if (title != null)
        {
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -6f), new Vector2(218f, 44f));
            title.text = song != null && !string.IsNullOrWhiteSpace(song.SongTitle) ? song.SongTitle : "Unknown Song";
            title.fontSize = 24f;
            title.enableAutoSizing = true;
            title.fontSizeMin = 14f;
            title.fontSizeMax = 24f;
            title.alignment = TextAlignmentOptions.Left;
            title.overflowMode = TextOverflowModes.Ellipsis;
        }

        Image cover = FindSongCoverImage(leftSongInfo);
        if (cover != null)
        {
            SetRect(cover.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -58f), new Vector2(150f, 150f));
            cover.sprite = song != null ? song.PreviewImage : null;
            cover.type = Image.Type.Simple;
            cover.preserveAspect = true;
            cover.color = cover.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.12f);
        }
    }

    private static void RepairComboBadgeChildren(RectTransform rightScoreBoard)
    {
        Image[] images = rightScoreBoard.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image == null)
                continue;

            SetRect(image.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(122f, 76f));
            image.preserveAspect = true;
        }

        TextMeshProUGUI[] labels = rightScoreBoard.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI label in labels)
        {
            if (label == null)
                continue;

            if (label.text == "COMBO")
            {
                SetRect(label.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 22f), new Vector2(88f, 26f));
                label.fontSize = 17f;
            }
        }
    }

    private static Image FindBackground(Transform parent)
    {
        Image exact = FindImage(parent, "BackgroundImage");
        if (exact != null)
            return exact;

        Image[] images = parent.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            string name = image.name.ToLowerInvariant();
            if (name.Contains("background") || name.Contains("back ground"))
                return image;
        }

        return null;
    }

    private static TextMeshProUGUI FindSongTitleText(Transform leftSongInfo)
    {
        TextMeshProUGUI[] texts = leftSongInfo.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            if (text.text == "New Text")
                return text;
        }

        return texts.Length > 0 ? texts[0] : null;
    }

    private static Image FindSongCoverImage(Transform leftSongInfo)
    {
        Image[] images = leftSongInfo.GetComponentsInChildren<Image>(true);
        Image best = null;
        float bestArea = -1f;
        foreach (Image image in images)
        {
            if (image == null)
                continue;

            RectTransform rect = image.rectTransform;
            float area = Mathf.Abs(rect.rect.width * rect.rect.height);
            if (area > bestArea)
            {
                best = image;
                bestArea = area;
            }
        }

        return best;
    }

    private static Image FindImage(Transform parent, string objectName)
    {
        RectTransform rect = FindRect(parent, objectName);
        return rect != null ? rect.GetComponent<Image>() : null;
    }

    private static RectTransform FindRect(Transform parent, string objectName)
    {
        if (parent == null)
            return null;

        if (parent.name == objectName)
            return parent as RectTransform;

        for (int i = 0; i < parent.childCount; i++)
        {
            RectTransform found = FindRect(parent.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void SetBottomButton(Transform overlay, string name, Vector2 position, Vector2 anchor, Vector2 pivot)
    {
        RectTransform rect = FindRect(overlay, name);
        if (rect == null)
            return;

        SetRect(rect, anchor, anchor, pivot, position, new Vector2(122f, 42f));
        rect.localScale = Vector3.one;
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
