using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("Đánh dấu true nếu UIManager này nằm trong scene Gameplay")]
    public bool isGameplayScene = false;
    [Tooltip("Chỉ dùng CanvasThai như popup Settings, không bật Lobby hoặc Gameplay panel khi scene mở.")]
    [SerializeField] private bool settingsOverlayOnly = false;

    [Header("Main Panels")]
    public GameObject lobbyPanel;
    public GameObject gameplayPanel;

    [Header("Popups")]
    public GameObject settingsPopup;
    public GameObject pausePopup;
    public TextMeshProUGUI countdownText;

    private bool isCountingDown = false;
    private const int SettingsOverlaySortingOrder = 1000;
    private RectTransform noteSpeedPreviewMarker;
    private TextMeshProUGUI noteSpeedPreviewValue;

    [Header("Settings Tabs Content")]
    public GameObject contentAudio;
    public GameObject contentVisual;
    public GameObject contentGameplay;

    [Header("Gameplay Tab UI")]
    public TextMeshProUGUI textSkillDisplay;
    public TextMeshProUGUI textNoteSpeed;
    public TextMeshProUGUI textPauseType;
    public TextMeshProUGUI textStaminaNotif;
    public TextMeshProUGUI textPureLateEarly;
    public TextMeshProUGUI textShowPotential;
    public TextMeshProUGUI textInviteNotif;

    [Header("Audio Tab UI")]
    public TextMeshProUGUI textNoteVolume;
    public TextMeshProUGUI textOffset;
    public TextMeshProUGUI textAudioPreset;

    [Header("Visual Tab UI")]
    public TextMeshProUGUI textGraphicsQuality;
    public TextMeshProUGUI textShowTouches;
    public TextMeshProUGUI textStoryTextSpeed;
    public TextMeshProUGUI textColorblindMode;
    public TextMeshProUGUI textFRPMIndicator;
    public TextMeshProUGUI textLateEarlyPosition;

    // Data Gameplay
    private bool isSkillDisplayEnabled = true;
    private float currentNoteSpeed = 1.0f;
    private int pauseTypeIndex = 0;
    private string[] pauseTypes = { "Single Tap", "Double Tap", "Disabled" };
    private bool isStaminaNotifEnabled, isPureLateEarlyEnabled = true, isShowPotentialEnabled = true, isInviteNotifEnabled = true;

    // Data Audio
    private int currentVolume = 100, currentOffset = 0;
    private bool isHeadphonesPreset = false;

    // Data Visual (6 Chức năng chuẩn Arcaea)
    private int qualityIndex = 1;
    private string[] qualityLevels = { "Low", "Standard / High" };

    private bool isShowTouchesEnabled = false;

    private int storySpeedIndex = 0;
    private string[] storySpeeds = { "Default", "Fast", "Slow" };

    private bool isColorblindModeEnabled = false;

    private int frpmIndex = 0;
    private string[] frpmPositions = { "Top", "Bottom" };

    private int lateEarlyPosIndex = 0;
    private string[] lateEarlyPositions = { "Middle", "Top", "Bottom" };

    private void Start()
    {
        LoadSettings();
        NormalizeOffsetTextLayout();
        BuildNoteSpeedPreview();
        if (settingsOverlayOnly)
        {
            TogglePanel(false, false, false, false);
            return;
        }

        if (isGameplayScene)
        {
            StartGame();
        }
        else
        {
            OpenLobby();
        }
    }

    private void Update()
    {
        UpdateNoteSpeedPreview();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (gameplayPanel != null && gameplayPanel.activeSelf)
            {
                if (pausePopup != null && pausePopup.activeSelf)
                {
                    if (!isCountingDown)
                        ResumeGame();
                }
                else
                {
                    PauseGame();
                }
            }
        }
    }

    // --- CHUYỂN ĐỔI STATE TRÒ CHƠI ---
    public void OpenLobby() { TogglePanel(true, false, false, false); Time.timeScale = 1f; }
    public void StartGame() { TogglePanel(false, true, false, false); }
    public void PrepareAsSettingsOverlay()
    {
        settingsOverlayOnly = true;
        TogglePanel(false, false, false, false);
        BringSettingsCanvasToFront();
        Time.timeScale = 1f;
    }

    public void OpenSettings()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        BringSettingsCanvasToFront();
        TogglePanel(false, false, true, false);
        NormalizeOffsetTextLayout();

        if (settingsPopup != null)
        {
            settingsPopup.SetActive(true);
            settingsPopup.transform.SetAsLastSibling();
        }
        OpenTabGameplay();
    }
    public void CloseSettingsAndSave() { SaveSettings(); settingsPopup.SetActive(false); }

    private void BringSettingsCanvasToFront()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        canvas.overrideSorting = true;
        if (canvas.sortingOrder < SettingsOverlaySortingOrder)
            canvas.sortingOrder = SettingsOverlaySortingOrder;
    }

    private void TogglePanel(bool lobby, bool gameplay, bool settings, bool pause)
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(lobby);
        if (gameplayPanel != null) gameplayPanel.SetActive(gameplay);
        if (settingsPopup != null) settingsPopup.SetActive(settings);
        if (pausePopup != null) pausePopup.SetActive(pause);
    }

    // --- CHUYỂN TÁB CÀI ĐẶT ---
    public void OpenTabAudio() { SwitchTab(true, false, false); }
    public void OpenTabVisual() { SwitchTab(false, true, false); }
    public void OpenTabGameplay() { SwitchTab(false, false, true); }
    private void SwitchTab(bool audio, bool visual, bool gameplay)
    {
        if (contentAudio != null) contentAudio.SetActive(audio);
        if (contentVisual != null) contentVisual.SetActive(visual);
        if (contentGameplay != null) contentGameplay.SetActive(gameplay);
    }

    // --- LOGIC GAMEPLAY ---
    public void ToggleSkillDisplay() { isSkillDisplayEnabled = !isSkillDisplayEnabled; textSkillDisplay.text = isSkillDisplayEnabled ? "Enabled" : "Disabled"; }
    public void ChangeNoteSpeed(float amount) { currentNoteSpeed = Mathf.Clamp(currentNoteSpeed + amount, RuntimeGameplaySettings.MinNoteSpeedSetting, RuntimeGameplaySettings.MaxNoteSpeedSetting); if (textNoteSpeed != null) textNoteSpeed.text = currentNoteSpeed.ToString("F1"); RuntimeGameplaySettings.NoteSpeedMultiplier = currentNoteSpeed; RuntimeGameplaySettings.Save(); UpdateNoteSpeedPreview(); }
    public void TogglePauseType() { pauseTypeIndex = (pauseTypeIndex + 1) % pauseTypes.Length; textPauseType.text = pauseTypes[pauseTypeIndex]; }
    public void ToggleStaminaNotif() { isStaminaNotifEnabled = !isStaminaNotifEnabled; textStaminaNotif.text = isStaminaNotifEnabled ? "Enabled" : "Disabled"; }
    public void TogglePureLateEarly() { isPureLateEarlyEnabled = !isPureLateEarlyEnabled; textPureLateEarly.text = isPureLateEarlyEnabled ? "Enabled" : "Disabled"; }
    public void ToggleShowPotential() { isShowPotentialEnabled = !isShowPotentialEnabled; textShowPotential.text = isShowPotentialEnabled ? "Enabled" : "Disabled"; }
    public void ToggleInviteNotif() { isInviteNotifEnabled = !isInviteNotifEnabled; textInviteNotif.text = isInviteNotifEnabled ? "Enabled" : "Disabled"; }

    // --- LOGIC AUDIO ---
    public void ChangeVolume(int amount) { currentVolume = Mathf.Clamp(currentVolume + amount, 0, 100); if (textNoteVolume != null) textNoteVolume.text = currentVolume + "%"; RuntimeGameplaySettings.MusicVolumePercent = currentVolume; RuntimeGameplaySettings.Save(); }
    public void ChangeAudioOffset(int amount) { currentOffset = Mathf.Clamp(currentOffset + amount, -500, 1000); if (textOffset != null) { textOffset.text = currentOffset.ToString(); NormalizeOffsetTextLayout(); } RuntimeGameplaySettings.AudioOffsetMs = currentOffset; RuntimeGameplaySettings.Save(); }
    public void ToggleAudioPreset() { isHeadphonesPreset = !isHeadphonesPreset; if (textAudioPreset != null) textAudioPreset.text = isHeadphonesPreset ? "Headphones" : "Speaker"; RuntimeGameplaySettings.HeadphonesPreset = isHeadphonesPreset; RuntimeGameplaySettings.Save(); }

    // --- LOGIC VISUAL ---
    public void ToggleGraphicsQuality() { qualityIndex = (qualityIndex + 1) % qualityLevels.Length; textGraphicsQuality.text = qualityLevels[qualityIndex]; }
    public void ToggleShowTouches() { isShowTouchesEnabled = !isShowTouchesEnabled; textShowTouches.text = isShowTouchesEnabled ? "Enabled" : "Disabled"; }
    public void ToggleStoryTextSpeed() { storySpeedIndex = (storySpeedIndex + 1) % storySpeeds.Length; textStoryTextSpeed.text = storySpeeds[storySpeedIndex]; }
    public void ToggleColorblindMode() { isColorblindModeEnabled = !isColorblindModeEnabled; textColorblindMode.text = isColorblindModeEnabled ? "Enabled" : "Disabled"; }
    public void ToggleFRPMIndicator() { frpmIndex = (frpmIndex + 1) % frpmPositions.Length; textFRPMIndicator.text = frpmPositions[frpmIndex]; }
    public void ToggleLateEarlyPosition() { lateEarlyPosIndex = (lateEarlyPosIndex + 1) % lateEarlyPositions.Length; textLateEarlyPosition.text = lateEarlyPositions[lateEarlyPosIndex]; }

    // --- LƯU VÀ TẢI DỮ LIỆU ---
    private void SaveSettings()
    {
        PlayerPrefs.SetInt("SkillDisplay", isSkillDisplayEnabled ? 1 : 0);
        RuntimeGameplaySettings.NoteSpeedMultiplier = currentNoteSpeed;
        PlayerPrefs.SetInt("PauseType", pauseTypeIndex);
        PlayerPrefs.SetInt("StaminaNotif", isStaminaNotifEnabled ? 1 : 0);
        PlayerPrefs.SetInt("PureLateEarly", isPureLateEarlyEnabled ? 1 : 0);
        PlayerPrefs.SetInt("ShowPotential", isShowPotentialEnabled ? 1 : 0);
        PlayerPrefs.SetInt("InviteNotif", isInviteNotifEnabled ? 1 : 0);
        RuntimeGameplaySettings.MusicVolumePercent = currentVolume;
        RuntimeGameplaySettings.AudioOffsetMs = currentOffset;
        RuntimeGameplaySettings.HeadphonesPreset = isHeadphonesPreset;
        // Lưu dữ liệu Visual
        PlayerPrefs.SetInt("VisualQuality", qualityIndex);
        PlayerPrefs.SetInt("VisualShowTouches", isShowTouchesEnabled ? 1 : 0);
        PlayerPrefs.SetInt("VisualStorySpeed", storySpeedIndex);
        PlayerPrefs.SetInt("VisualColorblind", isColorblindModeEnabled ? 1 : 0);
        PlayerPrefs.SetInt("VisualFRPM", frpmIndex);
        PlayerPrefs.SetInt("VisualLateEarlyPos", lateEarlyPosIndex);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        currentNoteSpeed = RuntimeGameplaySettings.NoteSpeedMultiplier; if (textNoteSpeed != null) textNoteSpeed.text = currentNoteSpeed.ToString("F1");
        isSkillDisplayEnabled = PlayerPrefs.GetInt("SkillDisplay", 1) == 1; if (textSkillDisplay != null) textSkillDisplay.text = isSkillDisplayEnabled ? "Enabled" : "Disabled";
        pauseTypeIndex = PlayerPrefs.GetInt("PauseType", 0); if (textPauseType != null) textPauseType.text = pauseTypes[pauseTypeIndex];
        isStaminaNotifEnabled = PlayerPrefs.GetInt("StaminaNotif", 0) == 1; if (textStaminaNotif != null) textStaminaNotif.text = isStaminaNotifEnabled ? "Enabled" : "Disabled";
        isPureLateEarlyEnabled = PlayerPrefs.GetInt("PureLateEarly", 1) == 1; if (textPureLateEarly != null) textPureLateEarly.text = isPureLateEarlyEnabled ? "Enabled" : "Disabled";
        isShowPotentialEnabled = PlayerPrefs.GetInt("ShowPotential", 1) == 1; if (textShowPotential != null) textShowPotential.text = isShowPotentialEnabled ? "Enabled" : "Disabled";
        isInviteNotifEnabled = PlayerPrefs.GetInt("InviteNotif", 1) == 1; if (textInviteNotif != null) textInviteNotif.text = isInviteNotifEnabled ? "Enabled" : "Disabled";
        currentVolume = RuntimeGameplaySettings.MusicVolumePercent; if (textNoteVolume != null) textNoteVolume.text = currentVolume + "%";
        currentOffset = RuntimeGameplaySettings.AudioOffsetMs; if (textOffset != null) textOffset.text = currentOffset.ToString();
        isHeadphonesPreset = RuntimeGameplaySettings.HeadphonesPreset; if (textAudioPreset != null) textAudioPreset.text = isHeadphonesPreset ? "Headphones" : "Speaker";
        // Tải dữ liệu Visual
        qualityIndex = PlayerPrefs.GetInt("VisualQuality", 1); if (textGraphicsQuality != null) textGraphicsQuality.text = qualityLevels[qualityIndex];
        isShowTouchesEnabled = PlayerPrefs.GetInt("VisualShowTouches", 0) == 1; if (textShowTouches != null) textShowTouches.text = isShowTouchesEnabled ? "Enabled" : "Disabled";
        storySpeedIndex = PlayerPrefs.GetInt("VisualStorySpeed", 0); if (textStoryTextSpeed != null) textStoryTextSpeed.text = storySpeeds[storySpeedIndex];
        isColorblindModeEnabled = PlayerPrefs.GetInt("VisualColorblind", 0) == 1; if (textColorblindMode != null) textColorblindMode.text = isColorblindModeEnabled ? "Enabled" : "Disabled";
        frpmIndex = PlayerPrefs.GetInt("VisualFRPM", 0); if (textFRPMIndicator != null) textFRPMIndicator.text = frpmPositions[frpmIndex];
        lateEarlyPosIndex = PlayerPrefs.GetInt("VisualLateEarlyPos", 0); if (textLateEarlyPosition != null) textLateEarlyPosition.text = lateEarlyPositions[lateEarlyPosIndex];
        NormalizeOffsetTextLayout();
    }

    private void NormalizeOffsetTextLayout()
    {
        if (textOffset == null)
            return;

        RectTransform rect = textOffset.rectTransform;
        if (rect != null && rect.sizeDelta.x < 92f)
            rect.sizeDelta = new Vector2(92f, Mathf.Max(rect.sizeDelta.y, 34f));

        textOffset.textWrappingMode = TextWrappingModes.NoWrap;
        textOffset.overflowMode = TextOverflowModes.Overflow;
        textOffset.alignment = TextAlignmentOptions.Center;
        textOffset.enableAutoSizing = true;
        textOffset.fontSizeMin = 16f;
        textOffset.fontSizeMax = Mathf.Max(textOffset.fontSize, 24f);
    }

    private void BuildNoteSpeedPreview()
    {
        if (contentGameplay == null)
            return;

        Transform old = contentGameplay.transform.Find("RG Note Speed Preview");
        if (old != null)
        {
            noteSpeedPreviewMarker = old.Find("Marker") as RectTransform;
            noteSpeedPreviewValue = old.GetComponentInChildren<TextMeshProUGUI>(true);
            return;
        }

        RectTransform root = CreateUiRect("RG Note Speed Preview", contentGameplay.transform);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(0f, -215f);
        root.sizeDelta = new Vector2(260f, 72f);

        Image bg = root.gameObject.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.04f, 0.10f, 0.60f);
        bg.raycastTarget = false;

        TextMeshProUGUI title = CreateUiText("NOTE SPEED PREVIEW", root, new Vector2(0f, 22f), new Vector2(230f, 22f), 13f, FontStyles.Bold);
        title.color = new Color(1f, 1f, 1f, 0.82f);

        RectTransform rail = CreateUiRect("Rail", root);
        rail.anchorMin = rail.anchorMax = new Vector2(0.5f, 0.5f);
        rail.pivot = new Vector2(0.5f, 0.5f);
        rail.anchoredPosition = new Vector2(0f, -3f);
        rail.sizeDelta = new Vector2(190f, 5f);
        Image railImage = rail.gameObject.AddComponent<Image>();
        railImage.color = new Color(1f, 1f, 1f, 0.18f);
        railImage.raycastTarget = false;

        noteSpeedPreviewMarker = CreateUiRect("Marker", rail);
        noteSpeedPreviewMarker.anchorMin = noteSpeedPreviewMarker.anchorMax = new Vector2(0.5f, 0.5f);
        noteSpeedPreviewMarker.pivot = new Vector2(0.5f, 0.5f);
        noteSpeedPreviewMarker.sizeDelta = new Vector2(14f, 14f);
        Image markerImage = noteSpeedPreviewMarker.gameObject.AddComponent<Image>();
        markerImage.color = new Color(0.70f, 1f, 1f, 0.95f);
        markerImage.raycastTarget = false;

        noteSpeedPreviewValue = CreateUiText("1.0x", root, new Vector2(0f, -26f), new Vector2(120f, 20f), 13f, FontStyles.Bold);
        noteSpeedPreviewValue.color = Color.white;
        UpdateNoteSpeedPreview();
    }

    private void UpdateNoteSpeedPreview()
    {
        if (noteSpeedPreviewValue != null)
            noteSpeedPreviewValue.text = RuntimeGameplaySettings.NoteSpeedMultiplier.ToString("0.0") + "x";

        if (noteSpeedPreviewMarker != null)
        {
            float phase = Mathf.Repeat(Time.unscaledTime * RuntimeGameplaySettings.NoteSpeedMultiplier * 0.55f, 1f);
            noteSpeedPreviewMarker.anchoredPosition = new Vector2(Mathf.Lerp(-92f, 92f, phase), 0f);
        }
    }

    private static RectTransform CreateUiRect(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static TextMeshProUGUI CreateUiText(string text, Transform parent, Vector2 position, Vector2 size, float fontSize, FontStyles fontStyle)
    {
        RectTransform rect = CreateUiRect("Text - " + text, parent);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        TmpRuntimeFontFallback.Apply(label);
        return label;
    }

    public void PauseGame()
    {
        if (isCountingDown) return;

        if (pausePopup != null)
        {
            pausePopup.SetActive(true);
            pausePopup.transform.SetAsLastSibling();
        }

        if (countdownText != null) countdownText.gameObject.SetActive(false);
        Time.timeScale = 0f;
        if (RhythmTimeManager.Instance != null) RhythmTimeManager.Instance.PauseGame();
    }

    public void ResumeGame()
    {
        if (!pausePopup.activeSelf || isCountingDown) return;
        StartCoroutine(ResumeCountdownCoroutine());
    }

    private IEnumerator ResumeCountdownCoroutine()
    {
        isCountingDown = true;

        // Ẩn menu Pause ngay lập tức
        if (pausePopup != null) pausePopup.SetActive(false);

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.transform.SetAsLastSibling(); // Đảm bảo số đếm ngược luôn đè lên trên cùng
            for (int i = 3; i > 0; i--)
            {
                countdownText.text = i.ToString();
                yield return new WaitForSecondsRealtime(1f);
            }
            countdownText.text = "GO!";
            yield return new WaitForSecondsRealtime(0.5f);
            countdownText.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSecondsRealtime(1f);
        }

        Time.timeScale = 1f;
        isCountingDown = false;

        if (RhythmTimeManager.Instance != null) RhythmTimeManager.Instance.ResumeGame();
    }

    public void RetryGame()
    {
        isCountingDown = false;
        StopAllCoroutines();

        if (GameManager.Instance != null)
            GameManager.Instance.RetryGame();
        else
            Debug.LogError("[UIManager] GameManager.Instance is null! Cannot retry.");
    }

    public void QuitToLobby()
    {
        isCountingDown = false;
        StopAllCoroutines();

        if (GameManager.Instance != null)
            GameManager.Instance.QuitToLobby();
        else
            Debug.LogError("[UIManager] GameManager.Instance is null! Cannot quit.");
    }
}
