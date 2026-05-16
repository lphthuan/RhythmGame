using UnityEngine;

/// <summary>
/// Cấu hình combo — chỉnh từ Inspector, không cần sửa code.
/// Tạo asset: Right-click > Create > ProjectBeat > ComboConfig
/// </summary>
[CreateAssetMenu(fileName = "ComboConfig", menuName = "ProjectBeat/ComboConfig")]
public class ComboConfig : ScriptableObject
{
    [Header("Milestone")]
    [Tooltip("Các mốc combo sẽ phát sự kiện OnComboMilestone (ví dụ: hiệu ứng đặc biệt).")]
    [SerializeField] private int[] _milestones = { 10, 25, 50, 100, 200, 500, 1000 };

    [Header("Combo Break Rules")]
    [Tooltip("Các NoteResult khiến combo reset về 0.")]
    [SerializeField] private NoteResult[] _breakResults = 
    { 
        NoteResult.Failed, 
        NoteResult.Missed, 
        NoteResult.ReleasedEarly 
    };

    /// <summary>Danh sách mốc milestone.</summary>
    public int[] Milestones => _milestones;

    /// <summary>
    /// Kiểm tra NoteResult này có reset combo không.
    /// </summary>
    public bool IsComboBreak(NoteResult result)
    {
        for (int i = 0; i < _breakResults.Length; i++)
        {
            if (_breakResults[i] == result)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra combo hiện tại có phải mốc milestone không.
    /// </summary>
    public bool IsMilestone(int combo)
    {
        for (int i = 0; i < _milestones.Length; i++)
        {
            if (_milestones[i] == combo)
                return true;
        }

        return false;
    }
}
