using UnityEngine;
using System;
using System.IO;

public class SettingsManager : MonoBehaviour
{
    // Cấu trúc Singleton để gọi ở bất cứ đâu (VD: SettingsManager.Instance.CurrentSettings.noteSpeed)
    public static SettingsManager Instance { get; private set; }

    public GameSettingsData CurrentSettings;

    // Action này cực kỳ quan trọng cho làm việc nhóm. 
    // Bất kỳ class nào (Audio, Spawner) cũng có thể lắng nghe sự kiện này để tự update.
    public static event Action OnSettingsChanged;

    private string savePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ nguyên khi chuyển Scene

            // Đường dẫn an toàn và chuẩn xác nhất cho cả Android và iOS
            savePath = Path.Combine(Application.persistentDataPath, "rhythm_settings.json");

            LoadSettings();
            ApplyCoreMobileSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Dev làm UI sẽ gọi hàm này mỗi khi bấm nút "Xác nhận" hoặc "X" thoát bảng Setting
    public void SaveSettings()
    {
        string json = JsonUtility.ToJson(CurrentSettings, true);
        File.WriteAllText(savePath, json);

        // Bắn tín hiệu cho toàn bộ game biết là setting đã thay đổi
        OnSettingsChanged?.Invoke();
    }

    private void LoadSettings()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            CurrentSettings = JsonUtility.FromJson<GameSettingsData>(json);
        }
        else
        {
            // Lần đầu vào game chưa có file save, tạo mới mặc định
            CurrentSettings = new GameSettingsData();
            SaveSettings();
        }
    }

    // Áp dụng ngay các tinh chỉnh cốt lõi cho Mobile khi vừa load game
    private void ApplyCoreMobileSettings()
    {
        // Rhythm game trên điện thoại BẮT BUỘC phải mở khóa FPS cao nhất để chạm nốt mượt
        Application.targetFrameRate = 120;

        // Cập nhật chất lượng đồ họa
        QualitySettings.SetQualityLevel(CurrentSettings.graphicsQuality);
    }
}