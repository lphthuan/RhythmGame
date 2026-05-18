using System;
using UnityEngine;

public enum AccuracyCalculateMode
{
    /// <summary>
    /// (Chuẩn Cytus / Deemo): Bắt đầu bài là 100%. 
    /// Giả định tất cả nốt chưa đánh đều là Perfect. Mỗi khi đánh hụt hoặc sai, % sẽ tụt dần.
    /// </summary>
    DropFrom100,

    /// <summary>
    /// (Chuẩn osu! / truyền thống): Chỉ tính trung bình dựa trên những nốt ĐÃ ĐÁNH. 
    /// Ví dụ nốt đầu tiên đánh Miss thì sẽ hiển thị 0%, đánh nốt 2 Perfect thì kéo lên lại 50%.
    /// </summary>
    AverageCurrent
}

/// <summary>
/// Quản lý phần trăm (Accuracy) của người chơi trong màn chơi.
/// Lắng nghe dữ liệu từ ScoreManager (sau này) và phát Event cho UI hiển thị.
/// </summary>
public class AccuracyManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Cách tính % Accuracy")]
    [SerializeField] private AccuracyCalculateMode _calculateMode = AccuracyCalculateMode.DropFrom100;
    
    [Tooltip("Tổng số nốt nhạc của bài hát. Dùng để test khi chưa có Beatmap.")]
    [SerializeField] private int _totalNotes = 100;

    private float _totalEarnedWeights = 0f;
    private int _notesProcessed = 0;

    /// <summary>
    /// Phần trăm hiện tại (từ 0.0 đến 1.0)
    /// </summary>
    public float CurrentAccuracy { get; private set; }

    /// <summary>
    /// Event phát ra mỗi khi accuracy thay đổi, truyền ra giá trị từ 0.0 -> 1.0.
    /// UI dùng cái này để cập nhật thanh Slider hoặc % chữ.
    /// </summary>
    public event Action<float> OnAccuracyChanged;

    private void Start()
    {
        // Khởi tạo mặc định
        ResetAccuracy(_totalNotes);
    }

    /// <summary>
    /// Hàm này để reset lại vào đầu bài hát
    /// </summary>
    public void ResetAccuracy(int totalNotesOfSong)
    {
        _totalNotes = totalNotesOfSong;
        _totalEarnedWeights = 0f;
        _notesProcessed = 0;
        
        CurrentAccuracy = (_calculateMode == AccuracyCalculateMode.DropFrom100) ? 1f : 0f;
        
        OnAccuracyChanged?.Invoke(CurrentAccuracy);
    }

    /// <summary>
    /// ScoreManager (sau này) hoặc Test script sẽ gọi hàm này mỗi khi ăn/hụt 1 nốt.
    /// </summary>
    public void AddJudgment(HitJudgment judgment)
    {
        _notesProcessed++;

        // Tính trọng số dựa theo công thức bạn đưa ra
        float weight = 0f;
        switch (judgment)
        {
            case HitJudgment.Perfect:
                weight = 1.0f;  // 100%
                break;
            case HitJudgment.Great:
                weight = 0.75f;  // 75%
                break;
            case HitJudgment.Good:
                weight = 0.5f;  // 50%
                break;
            case HitJudgment.Miss:
                weight = 0.0f;  // 0%
                break;
        }

        _totalEarnedWeights += weight;

        // Tính % hiện tại tùy theo chế độ
        if (_calculateMode == AccuracyCalculateMode.DropFrom100)
        {
            if (_totalNotes > 0)
            {
                // Giả định nốt còn lại đều Perfect (+1 cho mỗi nốt)
                float remainingNotes = _totalNotes - _notesProcessed;
                float maxPossibleWeights = _totalEarnedWeights + remainingNotes; 
                CurrentAccuracy = maxPossibleWeights / _totalNotes;
            }
        }
        else if (_calculateMode == AccuracyCalculateMode.AverageCurrent)
        {
            if (_notesProcessed > 0)
            {
                // Chỉ chia trung bình trên những nốt đã xử lý
                CurrentAccuracy = _totalEarnedWeights / _notesProcessed;
            }
        }

        // Đảm bảo % không lọt ra ngoài 0-1
        CurrentAccuracy = Mathf.Clamp01(CurrentAccuracy);

        // Báo cho UI biết để cập nhật
        OnAccuracyChanged?.Invoke(CurrentAccuracy);
    }
}
