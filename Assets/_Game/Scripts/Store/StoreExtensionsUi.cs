using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Creates the first skin and daily-mission cards in the existing StoreMenu without scene wiring.</summary>
public sealed class StoreExtensionsUi : MonoBehaviour
{
    private const string RootName = "RG Store Extensions";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded += (_, __) => TryInstall();
        TryInstall();
    }

    private static void TryInstall()
    {
        if (SceneManager.GetActiveScene().name != "StoreMenu" || Object.FindFirstObjectByType<StoreExtensionsUi>() != null)
            return;

        GameObject host = new GameObject(RootName);
        host.AddComponent<StoreExtensionsUi>();
    }

    private void Start()
    {
        CategoryTabController tabs = FindFirstObjectByType<CategoryTabController>();
        if (tabs == null)
            return;

        tabs.lockNote = false;
        BuildRechargeCard(tabs.themePanel);
        BuildSkinCard(tabs.notePanel);
        CloudSyncManager.GetOrCreate().RefreshWallet();
    }

    private static void BuildRechargeCard(GameObject panel)
    {
        Transform content = GetContent(panel);
        if (content == null || content.Find("RG Diamond Recharge Card") != null)
            return;

        RectTransform card = CreatePanel("RG Diamond Recharge Card", content, new Color(0.12f, 0.06f, 0.22f, 0.98f));
        card.SetAsFirstSibling();
        card.gameObject.AddComponent<LayoutElement>().preferredHeight = 190f;
        VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 16, 16);
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        CreateText("DIAMOND WALLET", card, 24, FontStyles.Bold, 34f);
        TMP_Text balance = CreateText(string.Empty, card, 20, FontStyles.Bold, 32f);
        CreateText("Nạp tiền để nhận Diamond dùng mở khóa bài hát và vật phẩm.", card, 15, FontStyles.Normal, 30f);

        RectTransform actions = CreateRect("Actions", card);
        actions.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
        HorizontalLayoutGroup actionLayout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
        actionLayout.childAlignment = TextAnchor.MiddleCenter;
        actionLayout.spacing = 12f;

        Button recharge = CreateButton(actions, "NAP DIAMOND");
        Button sync = CreateButton(actions, "SYNC");

        void RefreshBalance() => balance.text = $"Diamond: {PlayerWallet.Diamond}";
        recharge.onClick.AddListener(() =>
        {
            string playerId = SaveManager.Instance != null ? SaveManager.Instance.GetPlayerId() : null;
            if (string.IsNullOrEmpty(playerId) && AccountSession.IsSignedIn)
                playerId = AccountSession.CurrentUsername;
            if (string.IsNullOrEmpty(playerId)) playerId = "demo-player";
            Application.OpenURL("http://localhost:5173/?playerId=" + UnityEngine.Networking.UnityWebRequest.EscapeURL(playerId));
        });
        sync.onClick.AddListener(() => CloudSyncManager.GetOrCreate().RefreshWallet(_ => RefreshBalance()));
        PlayerWallet.Changed += RefreshBalance;
        RefreshBalance();
    }


    private static void BuildSkinCard(GameObject panel)
    {
        Transform content = GetContent(panel);
        if (content == null || content.Find("RG Arcaea Skin Card") != null)
            return;

        RectTransform card = CreatePanel("RG Arcaea Skin Card", content, new Color(0.075f, 0.06f, 0.14f, 0.98f));
        card.gameObject.AddComponent<LayoutElement>().preferredHeight = 280f;
        VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 16, 16);
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        CreateText("Arcaea ConflictSide", card, 28, FontStyles.Bold, 40f);
        CreateText("4K note + Hold head/body/tail + lane theme", card, 17, FontStyles.Normal, 30f);

        RectTransform previewRow = CreateRect("Preview", card);
        previewRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 90f;
        HorizontalLayoutGroup previewLayout = previewRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        previewLayout.childAlignment = TextAnchor.MiddleCenter;
        previewLayout.spacing = 24f;
        AddPreview(previewRow, "Tap", "NoteSkins/ArcaeaConflictSide/tap");
        AddPreview(previewRow, "Hold", "NoteSkins/ArcaeaConflictSide/hold-head");
        AddPreview(previewRow, "Lane", "NoteSkins/ArcaeaConflictSide/stage-bottom");

        RectTransform buttons = CreateRect("Actions", card);
        buttons.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
        HorizontalLayoutGroup buttonLayout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.spacing = 14f;
        Button buy = CreateButton(buttons, "BUY 500 MONEY");
        Button apply = CreateButton(buttons, "APPLY");
        TMP_Text state = CreateText(string.Empty, card, 17, FontStyles.Bold, 28f);

        void Refresh()
        {
            if (apply == null) return;
            bool owned = NoteSkinService.IsOwned(NoteSkinService.ArcaeaConflictSideId);
            bool equipped = NoteSkinService.IsEquipped(NoteSkinService.ArcaeaConflictSideId);
            buy.gameObject.SetActive(!owned);
            apply.gameObject.SetActive(owned);
            apply.interactable = !equipped;
            SetButtonText(apply, equipped ? "EQUIPPED" : "APPLY");
            state.text = equipped
                ? "EQUIPPED"
                : owned ? "OWNED — press APPLY" : "Unlock with Money";
        }

        buy.onClick.AddListener(() =>
        {
            NoteSkinService.TryPurchaseArcaea(out string message);
            state.text = message;
            Refresh();
        });
        apply.onClick.AddListener(() =>
        {
            NoteSkinService.TryEquip(NoteSkinService.ArcaeaConflictSideId, out string message);
            state.text = message;
            Refresh();
        });
        Refresh();
        NoteSkinService.Changed += Refresh;
    }

    private static void BuildDailyCard(GameObject panel)
    {
        Transform content = GetContent(panel);
        if (content == null || content.Find("RG Daily Missions") != null)
            return;

        RectTransform card = CreatePanel("RG Daily Missions", content, new Color(0.06f, 0.12f, 0.16f, 0.98f));
        card.gameObject.AddComponent<LayoutElement>().preferredHeight = 290f;
        VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 16, 16);
        layout.spacing = 8;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        CreateText("NHIỆM VỤ NGÀY  •  reset 00:00 Việt Nam", card, 23, FontStyles.Bold, 38f);

        foreach (DailyQuestService.QuestId id in System.Enum.GetValues(typeof(DailyQuestService.QuestId)))
        {
            RectTransform row = CreateRect(id.ToString(), card);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;
            HorizontalLayoutGroup rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 10;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            TMP_Text label = CreateText(string.Empty, row, 16, FontStyles.Normal, 58f);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            Button claim = CreateButton(row, "CLAIM");
            claim.gameObject.AddComponent<LayoutElement>().preferredWidth = 110f;

            void RefreshRow()
            {
                DailyQuestService.QuestState state = null;
                foreach (DailyQuestService.QuestState item in DailyQuestService.GetQuests())
                    if (item.id == id.ToString()) state = item;
                int progress = state != null ? state.progress : 0;
                bool claimed = state != null && state.claimed;
                label.text = $"{DailyQuestService.GetTitle(id)}  {progress}/{DailyQuestService.GetTarget(id)}  •  {DailyQuestService.GetReward(id)} Money";
                claim.gameObject.SetActive(!claimed);
                claim.interactable = progress >= DailyQuestService.GetTarget(id);
                claim.GetComponentInChildren<TMP_Text>().text = claimed ? "CLAIMED" : "CLAIM";
            }

            claim.onClick.AddListener(() =>
            {
                DailyQuestService.TryClaim(id, out _);
                RefreshRow();
            });
            RefreshRow();
        }
    }

    private static Transform GetContent(GameObject panel)
    {
        if (panel == null) return null;
        ScrollRect scroll = panel.GetComponentInChildren<ScrollRect>(true);
        return scroll != null && scroll.content != null ? scroll.content : panel.transform;
    }

    private static void AddPreview(Transform parent, string label, string resourcePath)
    {
        RectTransform root = CreateRect(label, parent);
        root.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        Image image = root.gameObject.AddComponent<Image>();
        image.sprite = Resources.Load<Sprite>(resourcePath);
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private static Button CreateButton(Transform parent, string label)
    {
        RectTransform root = CreatePanel(label, parent, new Color(0.35f, 0.18f, 0.68f, 1f));
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = root.GetComponent<Image>();
        GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(root, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = label;
        text.fontSize = 15;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return button;
    }

    private static void SetButtonText(Button button, string value)
    {
        TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
        if (text != null) text.text = value;
    }

    private static TextMeshProUGUI CreateText(string value, Transform parent, float size, FontStyles style, float preferredHeight)
    {
        RectTransform root = CreateRect("Text", parent);
        root.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
        TextMeshProUGUI text = root.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.gameObject.AddComponent<Image>().color = color;
        return rect;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }
}
