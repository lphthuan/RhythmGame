using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUIController : MonoBehaviour
{
    [Header("--- QUẢN LÝ TAB ---")]
    public GameObject pageGameplay; // Trang Lối chơi
    public GameObject pageAudio;    // Trang Âm thanh
    public GameObject pageVisual;   // Trang Hình ảnh

    [Header("--- LỐI CHƠI (GAMEPLAY) ---")]
    public TextMeshProUGUI textTocDoNot;
    public TMP_Dropdown dropdownKieuTamDung;

    [Header("--- ÂM THANH (AUDIO) ---")]
    public TextMeshProUGUI textDoTre;

    // --- MỚI THÊM: Phần hiển thị cho Âm lượng Nhạc ---
    public Slider thanhTruotAmLuongNhac;
    public TextMeshProUGUI textGiaTriAmLuongNhac; // Hiển thị số % của Nhạc

    // --- MỚI THÊM: Phần hiển thị số % cho Âm lượng Nốt ---
    public Slider thanhTruotAmLuong; // (Đây là thanh nốt cũ)
    public TextMeshProUGUI textGiaTriAmLuong; // Hiển thị số % của Nốt

    public TMP_Dropdown dropdownCauHinhAmThanh;

    [Header("--- HÌNH ẢNH (VISUAL) ---")]
    public Toggle toggleDoHoaCao;

    private GameSettingsData currentData;

    private void Start()
    {
        // Kiểm tra an toàn: Nếu có SettingsManager thì lấy data, chưa có thì tạo data tạm để test UI không bị lỗi
        if (SettingsManager.Instance != null)
        {
            currentData = SettingsManager.Instance.CurrentSettings;
        }
        else
        {
            currentData = new GameSettingsData();
        }

        UpdateUIFromData();

        // Mặc định khi bảng Setting bật lên, tự động mở Tab số 0 (Lối chơi)
        OpenTab(0);
    }

    // ================= QUẢN LÝ GIAO DIỆN (TAB & PANEL) =================

    // Hàm chuyển Tab (Gọi khi bấm 3 nút tab ở trên cùng)
    public void OpenTab(int tabIndex)
    {
        // Tắt hết cả 3 trang trước (thêm kiểm tra != null để an toàn)
        if (pageGameplay != null) pageGameplay.SetActive(false);
        if (pageAudio != null) pageAudio.SetActive(false);
        if (pageVisual != null) pageVisual.SetActive(false);

        // Bật trang tương ứng
        if (tabIndex == 0 && pageGameplay != null) pageGameplay.SetActive(true);
        else if (tabIndex == 1 && pageAudio != null) pageAudio.SetActive(true);
        else if (tabIndex == 2 && pageVisual != null) pageVisual.SetActive(true);
    }

    // Hàm dùng cho nút "Done" (Hoàn tất) để tắt bảng cài đặt đi
    public void DongBangCaiDat()
    {
        gameObject.SetActive(false); // Ẩn cái Panel chứa script này đi
    }

    // ================= CHỨC NĂNG CÁC NÚT BẤM =================

    public void IncreaseNoteSpeed()
    {
        currentData.noteSpeed += 0.5f; // Tăng 0.5 mỗi lần bấm
        currentData.noteSpeed = Mathf.Clamp(currentData.noteSpeed, 1.0f, 6.5f);
        UpdateUIFromData();
        SaveData();
    }

    public void DecreaseNoteSpeed()
    {
        currentData.noteSpeed -= 0.5f; // Trừ 0.5 mỗi lần bấm
        currentData.noteSpeed = Mathf.Clamp(currentData.noteSpeed, 1.0f, 6.5f);
        UpdateUIFromData();
        SaveData();
    }

    public void IncreaseOffset()
    {
        currentData.offset += 5;
        UpdateUIFromData();
        SaveData();
    }

    public void DecreaseOffset()
    {
        currentData.offset -= 5;
        UpdateUIFromData();
        SaveData();
    }

    // --- MỚI CẬP NHẬT: Thêm hiển thị % khi kéo thanh trượt Nốt ---
    public void OnVolumeChanged(float value)
    {
        currentData.noteVolume = value;
        if (textGiaTriAmLuong != null)
            textGiaTriAmLuong.text = Mathf.RoundToInt(value * 100f) + "%";
        SaveData();
    }

    // --- MỚI THÊM: Hàm xử lý riêng cho khi kéo thanh trượt Nhạc ---
    public void OnMusicVolumeChanged(float value)
    {
        // Lưu ý: Đảm bảo trong class GameSettingsData của bạn đã có biến tên là 'musicVolume'
        currentData.musicVolume = value;
        if (textGiaTriAmLuongNhac != null)
            textGiaTriAmLuongNhac.text = Mathf.RoundToInt(value * 100f) + "%";
        SaveData();
    }

    public void OnPauseTypeChanged(int index)
    {
        currentData.pauseType = index;
        SaveData();
    }

    public void OnGraphicsToggleChanged(bool isHigh)
    {
        currentData.graphicsQuality = isHigh ? 1 : 0;
        QualitySettings.SetQualityLevel(currentData.graphicsQuality);
        SaveData();
    }

    // ================= HÀM HỖ TRỢ =================

    private void UpdateUIFromData()
    {
        if (textTocDoNot != null)
            textTocDoNot.text = currentData.noteSpeed.ToString("F1");

        if (textDoTre != null)
            textDoTre.text = currentData.offset.ToString() + " ms";

        // --- CẬP NHẬT: Gắn giá trị cho thanh trượt Nốt và số % Nốt lúc mới mở bảng ---
        if (thanhTruotAmLuong != null)
            thanhTruotAmLuong.value = currentData.noteVolume;
        if (textGiaTriAmLuong != null)
            textGiaTriAmLuong.text = Mathf.RoundToInt(currentData.noteVolume * 100f) + "%";

        // --- MỚI THÊM: Gắn giá trị cho thanh trượt Nhạc và số % Nhạc lúc mới mở bảng ---
        if (thanhTruotAmLuongNhac != null)
            thanhTruotAmLuongNhac.value = currentData.musicVolume;
        if (textGiaTriAmLuongNhac != null)
            textGiaTriAmLuongNhac.text = Mathf.RoundToInt(currentData.musicVolume * 100f) + "%";

        if (dropdownKieuTamDung != null)
            dropdownKieuTamDung.value = currentData.pauseType;

        if (toggleDoHoaCao != null)
            toggleDoHoaCao.isOn = (currentData.graphicsQuality == 1);
    }

    private void SaveData()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SaveSettings();
        }
    }
}