using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Popup xác nhận mua hàng trong Store.
/// Gọi Show(tênSảnPhẩm, callbackKhiXácNhận) để hiện popup.
/// Bấm "Xác nhận" -> chạy callback rồi đóng. Bấm "Từ chối" -> chỉ đóng.
/// </summary>
public class PurchaseConfirmPopup : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text messageText;
    public Button confirmButton;
    public Button cancelButton;

    [Header("Nội dung popup ({0} sẽ được thay bằng tên sản phẩm)")]
    [TextArea]
    public string messageFormat = "Bạn có muốn mua \"{0}\" không?";

    private Action onConfirm;

    // Popup dùng chung cho cả shop, chỉ tạo 1 lần trên Canvas
    private static PurchaseConfirmPopup sharedInstance;

    /// <summary>Tạo (nếu chưa có) và trả về popup dùng chung trên Canvas chứa caller.</summary>
    public static PurchaseConfirmPopup GetOrCreate(PurchaseConfirmPopup prefab, Component caller)
    {
        if (sharedInstance == null)
        {
            if (prefab == null || caller == null) return null;

            Canvas canvas = caller.GetComponentInParent<Canvas>();
            if (canvas == null) return null;

            sharedInstance = Instantiate(prefab, canvas.rootCanvas.transform, false);
            sharedInstance.gameObject.SetActive(false);
        }
        return sharedInstance;
    }

    private void Awake()
    {
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(Hide);
    }

    public void Show(string itemName, Action confirmCallback)
    {
        onConfirm = confirmCallback;
        if (messageText != null)
        {
            messageText.text = string.Format(messageFormat, itemName);
        }
        if (confirmButton != null) confirmButton.gameObject.SetActive(true);
        gameObject.SetActive(true);
        transform.SetAsLastSibling(); // luôn nổi lên trên cùng
    }

    /// <summary>Hiện popup chỉ để thông báo (ẩn nút Xác nhận, chỉ còn nút đóng).</summary>
    public void ShowMessage(string message)
    {
        onConfirm = null;
        if (messageText != null)
        {
            messageText.text = message;
        }
        if (confirmButton != null) confirmButton.gameObject.SetActive(false);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    private void OnConfirmClicked()
    {
        Action callback = onConfirm;
        Hide();
        callback?.Invoke();
    }

    public void Hide()
    {
        onConfirm = null;
        gameObject.SetActive(false);
    }
}
