using UnityEngine;
using UnityEngine.UI;
using TMPro; // Sử dụng TextMeshPro cho Text UI

/// <summary>
/// Hiển thị phần trăm Accuracy lên giao diện HUD.
/// Tuyệt đối không chứa logic tính toán, chỉ lắng nghe dữ liệu từ AccuracyManager.
/// </summary>
public class AccuracyUI : MonoBehaviour
{
    [Header("References (Kéo thả từ Hierarchy)")]
    [Tooltip("Kéo script AccuracyManager vào đây")]
    [SerializeField] private AccuracyManager _accuracyManager;
    
    [Header("UI Elements (Kéo thả UI của bạn vào đây)")]
    [Tooltip("Text để hiển thị % (Chỉ nhận TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI _accuracyText;
    
    [Tooltip("Thanh Slider phần trăm (Nếu có)")]
    [SerializeField] private Slider _accuracySlider;

    private void OnEnable()
    {
        if (_accuracyManager != null)
        {
            // Bắt đầu lắng nghe sự thay đổi
            _accuracyManager.OnAccuracyChanged += UpdateUI;
            
            // Cập nhật giá trị hiện tại ngay lập tức lúc màn hình vừa bật lên
            UpdateUI(_accuracyManager.CurrentAccuracy);
        }
    }

    private void OnDisable()
    {
        if (_accuracyManager != null)
        {
            // Ngừng lắng nghe để tránh lỗi tràn bộ nhớ (Memory Leak)
            _accuracyManager.OnAccuracyChanged -= UpdateUI;
        }
    }

    /// <summary>
    /// Hàm này sẽ tự động được gọi mỗi khi AccuracyManager báo cáo có % mới
    /// </summary>
    private void UpdateUI(float accuracyValue)
    {
        // accuracyValue đang là số từ 0.0 đến 1.0
        
        if (_accuracyText != null)
        {
            // Nhân 100 và ép kiểu hiển thị 2 chữ số thập phân. VD: 0.995 -> "99.50%"
            _accuracyText.text = $"{accuracyValue * 100f:F2}%";
        }

        if (_accuracySlider != null)
        {
            // Cập nhật giá trị cho thanh trượt
            _accuracySlider.value = accuracyValue;
        }
    }
}
