using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategoryTabController : MonoBehaviour
{
    [Header("Tab Buttons")]
    public Button themeBtn;
    public Button songBtn;
    public Button noteBtn;
    public Button effectBtn;
    public Button avatarBtn;

    [Header("Content Panels")]
    public GameObject themePanel;  // ThemeScrollView
    public GameObject songPanel;   // SongScrollView
    public GameObject notePanel;
    public GameObject effectPanel;
    public GameObject avatarPanel;

    [Header("Tab Active Colors")]
    public Color activeColor = new Color(0.49f, 0.23f, 0.93f); // #7C3AED
    public Color inactiveColor = new Color(0.93f, 0.91f, 0.99f); // #EDE9FE

    [Header("Khóa danh mục chưa làm xong (bấm vào sẽ hiện Coming Soon)")]
    public bool lockNote = true;
    public bool lockEffect = true;
    public bool lockAvatar = true;
    public string comingSoonMessage = "COMING SOON!";
    [Tooltip("Tự ẩn bảng Coming Soon sau bao nhiêu giây (0 = chỉ đóng khi chạm)")]
    public float comingSoonDuration = 1.2f;

    private Button[] allBtns;
    private GameObject[] allPanels;

    private GameObject comingSoonPanel;
    private Coroutine hideRoutine;

    private void Start()
    {
        allBtns = new Button[] { themeBtn, songBtn, noteBtn, effectBtn, avatarBtn };
        allPanels = new GameObject[] { themePanel, songPanel, notePanel, effectPanel, avatarPanel };

        // G?n event cho t?ng tab
        themeBtn.onClick.AddListener(() => SwitchTab(0));
        songBtn.onClick.AddListener(() => SwitchTab(1));
        noteBtn.onClick.AddListener(() => SwitchTab(2));
        effectBtn.onClick.AddListener(() => SwitchTab(3));
        avatarBtn.onClick.AddListener(() => SwitchTab(4));

        // M? tab ??u ti�n m?c ??nh
        SwitchTab(0);
    }

    private bool IsLocked(int index)
    {
        if (index == 2) return lockNote;
        if (index == 3) return lockEffect;
        if (index == 4) return lockAvatar;
        return false;
    }

    private void SwitchTab(int index)
    {
        // Tab bị khóa: giữ nguyên tab hiện tại, chỉ hiện bảng Coming Soon
        if (IsLocked(index))
        {
            ShowComingSoon();
            return;
        }

        // ?n t?t c? panel, reset m�u t?t c? tab
        for (int i = 0; i < allPanels.Length; i++)
        {
            allPanels[i].SetActive(false);
            allBtns[i].GetComponent<Image>().color = inactiveColor;
        }

        // Hi?n panel ???c ch?n, ??i m�u tab active
        allPanels[index].SetActive(true);
        allBtns[index].GetComponent<Image>().color = activeColor;
    }

    // ================== BẢNG COMING SOON ==================

    private void ShowComingSoon()
    {
        if (comingSoonPanel == null)
        {
            comingSoonPanel = BuildComingSoonPanel();
        }

        comingSoonPanel.SetActive(true);
        comingSoonPanel.transform.SetAsLastSibling(); // nổi lên trên cùng

        if (hideRoutine != null) StopCoroutine(hideRoutine);
        if (comingSoonDuration > 0f)
        {
            hideRoutine = StartCoroutine(AutoHideComingSoon());
        }
    }

    private IEnumerator AutoHideComingSoon()
    {
        yield return new WaitForSecondsRealtime(comingSoonDuration);
        hideRoutine = null;
        if (comingSoonPanel != null) comingSoonPanel.SetActive(false);
    }

    // Tạo bảng Coming Soon bằng code, không cần prefab hay nối trong scene
    private GameObject BuildComingSoonPanel()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.rootCanvas.transform : transform;

        // Nền mờ che toàn màn hình, chạm vào đâu cũng đóng
        var root = new GameObject("ComingSoonPanel", typeof(RectTransform), typeof(Image), typeof(Button));
        var rootRt = (RectTransform)root.transform;
        rootRt.SetParent(parent, false);
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        root.GetComponent<Button>().onClick.AddListener(() => root.SetActive(false));

        // Hộp giữa màn hình
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        var panelRt = (RectTransform)panel.transform;
        panelRt.SetParent(rootRt, false);
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(420f, 140f);
        panel.GetComponent<Image>().color = new Color(0.49f, 0.23f, 0.93f, 0.95f); // tím theme

        // Chữ Coming Soon
        var textGo = new GameObject("Text", typeof(RectTransform));
        var textRt = (RectTransform)textGo.transform;
        textRt.SetParent(panelRt, false);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = comingSoonMessage;
        tmp.fontSize = 44f;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 20f;
        tmp.fontSizeMax = 48f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return root;
    }
}
