using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý trạng thái All Perfect của bài hát.
/// Đếm số nốt và kiểm tra nếu tất cả đều là Perfect thì sau 1 khoảng thời gian sẽ phát sự kiện.
/// </summary>
public class AllPerfectManager : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Thời gian chờ (giây) sau khi đánh nốt cuối cùng để hiện All Perfect")]
    [SerializeField] private float _delayTime = 3f;
    
    [Tooltip("Tổng số nốt nhạc của bài. Dùng để test.")]
    [SerializeField] private int _totalNotes = 100;

    private bool _isAllPerfect = true;
    private int _notesProcessed = 0;
    private bool _songFinished = false;

    /// <summary>
    /// Sự kiện phát ra khi người chơi đạt All Perfect và đã hết thời gian đếm lùi.
    /// </summary>
    public event Action OnAllPerfectAchieved;

    /// <summary>
    /// Gọi hàm này khi bắt đầu bài hát mới.
    /// </summary>
    public void ResetState(int totalNotesOfSong)
    {
        _totalNotes = totalNotesOfSong;
        _isAllPerfect = true;
        _notesProcessed = 0;
        _songFinished = false;
        
        StopAllCoroutines(); // Dừng các bộ đếm giờ cũ nếu có
    }

    /// <summary>
    /// Nhận tín hiệu khi người chơi đánh 1 nốt.
    /// </summary>
    public void AddJudgment(HitJudgment judgment)
    {
        if (_songFinished) return; // Nếu bài hát đã kết thúc thì không nhận thêm nốt

        _notesProcessed++;

        // Nếu có bất kỳ nốt nào không phải Perfect, gãy All Perfect
        if (judgment != HitJudgment.Perfect)
        {
            _isAllPerfect = false;
        }

        // Kiểm tra xem đã đánh hết nốt chưa
        if (_notesProcessed >= _totalNotes)
        {
            _songFinished = true;
            CheckAllPerfectCondition();
        }
    }

    private void CheckAllPerfectCondition()
    {
        if (_isAllPerfect)
        {
            // Bắt đầu đếm lùi 3 giây rồi mới gọi UI
            StartCoroutine(WaitAndTriggerAllPerfect());
        }
        else
        {
            Debug.Log("<color=orange>[AllPerfectManager]</color> Completed");
        }
    }

    private IEnumerator WaitAndTriggerAllPerfect()
    {
        Debug.Log($"<color=green>[AllPerfectManager]</color> Đã đạt All Perfect! Chờ {_delayTime} giây...");
        
        yield return new WaitForSeconds(_delayTime);

        // Đếm xong, phát sự kiện cho UI hiện lên
        OnAllPerfectAchieved?.Invoke();
    }
}
