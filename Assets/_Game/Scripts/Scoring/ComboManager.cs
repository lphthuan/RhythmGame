using System;
using UnityEngine;

/// <summary>
/// Quản lý combo trong gameplay.
/// Subscribe vào NoteManager.OnNoteFinishedEvent, phát sự kiện cho UI/Audio.
/// Không sửa đổi NoteManager hay NoteBase — giao tiếp 100% qua event.
/// </summary>
public class ComboManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NoteManager _noteManager;

    [Header("Config")]
    [SerializeField] private ComboConfig _config;

    private int _currentCombo;
    private int _maxCombo;

    /// <summary>Combo hiện tại.</summary>
    public int CurrentCombo => _currentCombo;

    /// <summary>Combo cao nhất trong lượt chơi.</summary>
    public int MaxCombo => _maxCombo;

    // ──────────────────────────────────────
    // Events — UI, Audio, Effects subscribe vào đây
    // ──────────────────────────────────────

    /// <summary>
    /// Phát mỗi khi combo thay đổi (tăng hoặc reset).
    /// UI dùng để cập nhật số combo hiển thị.
    /// </summary>
    public event Action<ComboData> OnComboChanged;

    /// <summary>
    /// Phát khi combo đạt mốc milestone (50, 100, 200, ...).
    /// Dùng để trigger hiệu ứng đặc biệt, SFX, animation.
    /// </summary>
    public event Action<int> OnComboMilestone;

    /// <summary>
    /// Phát khi combo bị reset về 0 sau khi đang có combo > 0.
    /// Dùng để trigger hiệu ứng combo break.
    /// </summary>
    public event Action<int> OnComboBreak;

    private void OnEnable()
    {
        if (_noteManager != null)
        {
            _noteManager.OnNoteFinishedEvent += HandleNoteResult;
        }
    }

    private void OnDisable()
    {
        if (_noteManager != null)
        {
            _noteManager.OnNoteFinishedEvent -= HandleNoteResult;
        }
    }

    /// <summary>
    /// Reset combo về trạng thái ban đầu (đầu bài mới).
    /// </summary>
    public void ResetAll()
    {
        _currentCombo = 0;
        _maxCombo = 0;
    }

    private void HandleNoteResult(NoteBase note, NoteResult result)
    {
        if (_config == null)
        {
            HandleWithoutConfig(result);
            return;
        }

        if (_config.IsComboBreak(result))
        {
            BreakCombo();
        }
        else
        {
            IncrementCombo();
        }
    }

    /// <summary>
    /// Fallback khi không gán ComboConfig — mặc định Completed = tăng, còn lại = reset.
    /// </summary>
    private void HandleWithoutConfig(NoteResult result)
    {
        if (result == NoteResult.Completed)
            IncrementCombo();
        else
            BreakCombo();
    }

    private void IncrementCombo()
    {
        _currentCombo++;

        if (_currentCombo > _maxCombo)
            _maxCombo = _currentCombo;

        bool isMilestone = _config != null && _config.IsMilestone(_currentCombo);

        ComboData data = new ComboData(_currentCombo, _maxCombo, isMilestone, false);
        OnComboChanged?.Invoke(data);

        if (isMilestone)
        {
            OnComboMilestone?.Invoke(_currentCombo);
        }
    }

    private void BreakCombo()
    {
        int previousCombo = _currentCombo;
        _currentCombo = 0;

        ComboData data = new ComboData(0, _maxCombo, false, true);
        OnComboChanged?.Invoke(data);

        if (previousCombo > 0)
        {
            OnComboBreak?.Invoke(previousCombo);
        }
    }
}
