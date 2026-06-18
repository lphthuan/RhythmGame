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

    private Button[] allBtns;
    private GameObject[] allPanels;

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

        // M? tab ??u tiên m?c ??nh
        SwitchTab(0);
    }

    private void SwitchTab(int index)
    {
        // ?n t?t c? panel, reset màu t?t c? tab
        for (int i = 0; i < allPanels.Length; i++)
        {
            allPanels[i].SetActive(false);
            allBtns[i].GetComponent<Image>().color = inactiveColor;
        }

        // Hi?n panel ???c ch?n, ??i màu tab active
        allPanels[index].SetActive(true);
        allBtns[index].GetComponent<Image>().color = activeColor;
    }
}