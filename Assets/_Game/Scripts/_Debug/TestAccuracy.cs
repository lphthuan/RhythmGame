using UnityEngine;

/// <summary>
/// Script test giả lập việc nhận Judgment cho AccuracyManager.
/// KHÔNG DÙNG TRONG BẢN BUILD (Nằm trong _Debug).
/// </summary>
public class TestAccuracy : MonoBehaviour
{
    [SerializeField] private AccuracyManager _accuracyManager;
    [SerializeField] private AllPerfectManager _allPerfectManager;

    private void Start()
    {
        if (_accuracyManager != null)
        {
            // Đăng ký nhận thông báo để in ra màn hình Console
            _accuracyManager.OnAccuracyChanged += OnAccuracyUpdated;
        }
        else
        {
            Debug.LogWarning("[TestAccuracy] Chưa kéo AccuracyManager vào Inspector!");
        }

        if (_allPerfectManager == null)
        {
            Debug.LogWarning("[TestAccuracy] Chưa kéo AllPerfectManager vào Inspector!");
        }
    }

    private void OnDestroy()
    {
        if (_accuracyManager != null)
        {
            _accuracyManager.OnAccuracyChanged -= OnAccuracyUpdated;
        }
    }

    private void OnAccuracyUpdated(float currentVal)
    {
        // currentVal là số thực từ 0.0 -> 1.0. Ta nhân 100 để in ra cho dễ đọc.
        Debug.Log($"<color=cyan>[TestAccuracy]</color> % Hiện tại: <b>{currentVal * 100:F2}%</b>");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            SendPerfect();
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            SendGreat();
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            SendGood();
        }
        else if (Input.GetKeyDown(KeyCode.V))
        {
            SendMiss();
        }
    }

    [ContextMenu("Reset / Bắt đầu bài hát mới")]
    public void TestReset()
    {
        Debug.Log("--- BẮT ĐẦU BÀI HÁT MỚI (10 Nốt để test All Perfect) ---");
        if (_accuracyManager != null) _accuracyManager.ResetAccuracy(10);
        if (_allPerfectManager != null) _allPerfectManager.ResetState(10);
    }

    [ContextMenu("Gửi 1 PERFECT (100%)")]
    public void SendPerfect()
    {
        if (_accuracyManager != null) _accuracyManager.AddJudgment(HitJudgment.Perfect);
        if (_allPerfectManager != null) _allPerfectManager.AddJudgment(HitJudgment.Perfect);
    }

    [ContextMenu("Gửi 1 GREAT (75%)")]
    public void SendGreat()
    {
        if (_accuracyManager != null) _accuracyManager.AddJudgment(HitJudgment.Great);
        if (_allPerfectManager != null) _allPerfectManager.AddJudgment(HitJudgment.Great);
    }

    [ContextMenu("Gửi 1 GOOD (50%)")]
    public void SendGood()
    {
        if (_accuracyManager != null) _accuracyManager.AddJudgment(HitJudgment.Good);
        if (_allPerfectManager != null) _allPerfectManager.AddJudgment(HitJudgment.Good);
    }

    [ContextMenu("Gửi 1 MISS (0%)")]
    public void SendMiss()
    {
        if (_accuracyManager != null) _accuracyManager.AddJudgment(HitJudgment.Miss);
        if (_allPerfectManager != null) _allPerfectManager.AddJudgment(HitJudgment.Miss);
    }
}
