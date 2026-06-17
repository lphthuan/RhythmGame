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

    [Header("Main Panels")]
    public GameObject lobbyPanel;
    public GameObject gameplayPanel;

    [Header("Popups")]
    public GameObject settingsPopup;
    public GameObject pausePopup;
    public TextMeshProUGUI countdownText;

    private bool isCountingDown = false;

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
    public void OpenSettings() 
    { 
        if (settingsPopup != null)
        {
            settingsPopup.SetActive(true); 
            settingsPopup.transform.SetAsLastSibling();
        }
        OpenTabGameplay(); 
    }
    public void CloseSettingsAndSave() { SaveSettings(); settingsPopup.SetActive(false); }

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
    public void ChangeNoteSpeed(float amount) { currentNoteSpeed = Mathf.Clamp(currentNoteSpeed + amount, 1.0f, 6.5f); textNoteSpeed.text = currentNoteSpeed.ToString("F1"); }
    public void TogglePauseType() { pauseTypeIndex = (pauseTypeIndex + 1) % pauseTypes.Length; textPauseType.text = pauseTypes[pauseTypeIndex]; }
    public void ToggleStaminaNotif() { isStaminaNotifEnabled = !isStaminaNotifEnabled; textStaminaNotif.text = isStaminaNotifEnabled ? "Enabled" : "Disabled"; }
    public void TogglePureLateEarly() { isPureLateEarlyEnabled = !isPureLateEarlyEnabled; textPureLateEarly.text = isPureLateEarlyEnabled ? "Enabled" : "Disabled"; }
    public void ToggleShowPotential() { isShowPotentialEnabled = !isShowPotentialEnabled; textShowPotential.text = isShowPotentialEnabled ? "Enabled" : "Disabled"; }
    public void ToggleInviteNotif() { isInviteNotifEnabled = !isInviteNotifEnabled; textInviteNotif.text = isInviteNotifEnabled ? "Enabled" : "Disabled"; }

    // --- LOGIC AUDIO ---
    public void ChangeVolume(int amount) { currentVolume = Mathf.Clamp(currentVolume + amount, 0, 100); textNoteVolume.text = currentVolume + "%"; }
    public void ChangeAudioOffset(int amount) { currentOffset = Mathf.Clamp(currentOffset + amount, -500, 1000); textOffset.text = currentOffset.ToString(); }
    public void ToggleAudioPreset() { isHeadphonesPreset = !isHeadphonesPreset; textAudioPreset.text = isHeadphonesPreset ? "🎧 Headphones" : "🔊 Speaker"; }

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
        PlayerPrefs.SetFloat("NoteSpeed", currentNoteSpeed);
        PlayerPrefs.SetInt("PauseType", pauseTypeIndex);
        PlayerPrefs.SetInt("StaminaNotif", isStaminaNotifEnabled ? 1 : 0);
        PlayerPrefs.SetInt("PureLateEarly", isPureLateEarlyEnabled ? 1 : 0);
        PlayerPrefs.SetInt("ShowPotential", isShowPotentialEnabled ? 1 : 0);
        PlayerPrefs.SetInt("InviteNotif", isInviteNotifEnabled ? 1 : 0);
        PlayerPrefs.SetInt("NoteVolume", currentVolume);
        PlayerPrefs.SetInt("AudioOffset", currentOffset);
        PlayerPrefs.SetInt("AudioPreset", isHeadphonesPreset ? 1 : 0);
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
        currentNoteSpeed = PlayerPrefs.GetFloat("NoteSpeed", 1.0f); textNoteSpeed.text = currentNoteSpeed.ToString("F1");
        isSkillDisplayEnabled = PlayerPrefs.GetInt("SkillDisplay", 1) == 1; textSkillDisplay.text = isSkillDisplayEnabled ? "Enabled" : "Disabled";
        pauseTypeIndex = PlayerPrefs.GetInt("PauseType", 0); textPauseType.text = pauseTypes[pauseTypeIndex];
        isStaminaNotifEnabled = PlayerPrefs.GetInt("StaminaNotif", 0) == 1; textStaminaNotif.text = isStaminaNotifEnabled ? "Enabled" : "Disabled";
        isPureLateEarlyEnabled = PlayerPrefs.GetInt("PureLateEarly", 1) == 1; textPureLateEarly.text = isPureLateEarlyEnabled ? "Enabled" : "Disabled";
        isShowPotentialEnabled = PlayerPrefs.GetInt("ShowPotential", 1) == 1; textShowPotential.text = isShowPotentialEnabled ? "Enabled" : "Disabled";
        isInviteNotifEnabled = PlayerPrefs.GetInt("InviteNotif", 1) == 1; textInviteNotif.text = isInviteNotifEnabled ? "Enabled" : "Disabled";
        currentVolume = PlayerPrefs.GetInt("NoteVolume", 100); textNoteVolume.text = currentVolume + "%";
        currentOffset = PlayerPrefs.GetInt("AudioOffset", 0); textOffset.text = currentOffset.ToString();
        isHeadphonesPreset = PlayerPrefs.GetInt("AudioPreset", 0) == 1; textAudioPreset.text = isHeadphonesPreset ? "🎧 Headphones" : "🔊 Speaker";
        // Tải dữ liệu Visual
        qualityIndex = PlayerPrefs.GetInt("VisualQuality", 1); textGraphicsQuality.text = qualityLevels[qualityIndex];
        isShowTouchesEnabled = PlayerPrefs.GetInt("VisualShowTouches", 0) == 1; textShowTouches.text = isShowTouchesEnabled ? "Enabled" : "Disabled";
        storySpeedIndex = PlayerPrefs.GetInt("VisualStorySpeed", 0); textStoryTextSpeed.text = storySpeeds[storySpeedIndex];
        isColorblindModeEnabled = PlayerPrefs.GetInt("VisualColorblind", 0) == 1; textColorblindMode.text = isColorblindModeEnabled ? "Enabled" : "Disabled";
        frpmIndex = PlayerPrefs.GetInt("VisualFRPM", 0); textFRPMIndicator.text = frpmPositions[frpmIndex];
        lateEarlyPosIndex = PlayerPrefs.GetInt("VisualLateEarlyPos", 0); textLateEarlyPosition.text = lateEarlyPositions[lateEarlyPosIndex];
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