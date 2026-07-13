using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn trên các ô sản phẩm (Theme / Note / Effect / Avatar) trong Store.
/// Bấm nút mua -> hiện popup xác nhận -> bấm "Xác nhận" mới thực sự mua.
/// (Ô bài hát dùng SongPreviewButton vì có thêm nút nghe thử.)
/// </summary>
public class ShopItemBuyButton : MonoBehaviour
{
    [Header("Tên sản phẩm hiển thị trong popup")]
    public TMP_Text itemTitleText;

    [Header("Nút mua (API sẽ tích hợp sau)")]
    public Button diamondBuyButton;
    public Button coinBuyButton;

    [Header("Popup xác nhận mua hàng")]
    public PurchaseConfirmPopup purchasePopupPrefab;

    [Header("Trạng thái sở hữu")]
    [Tooltip("Mã sản phẩm. Để trống = tự tạo từ tên object + tên sản phẩm")]
    public string itemId;
    [Tooltip("Lớp phủ khóa - hiện khi CHƯA mua")]
    public GameObject lockedOverlay;
    [Tooltip("Hàng nút mua - ẩn khi ĐÃ mua")]
    public GameObject priceRow;

    private void Awake()
    {
        if (diamondBuyButton != null) diamondBuyButton.onClick.AddListener(BuyWithDiamond);
        if (coinBuyButton != null) coinBuyButton.onClick.AddListener(BuyWithCoin);
        RefreshOwnedState();
    }

    public void BuyWithDiamond()
    {
        ShowBuyConfirm("Diamond");
    }

    public void BuyWithCoin()
    {
        ShowBuyConfirm("Coin");
    }

    private void ShowBuyConfirm(string currency)
    {
        string itemName = itemTitleText != null ? itemTitleText.text : gameObject.name;

        PurchaseConfirmPopup popup = PurchaseConfirmPopup.GetOrCreate(purchasePopupPrefab, this);
        if (popup == null)
        {
            Debug.LogWarning("[Store] Chưa gán Purchase Popup Prefab cho " + gameObject.name);
            return;
        }

        if (OwnedItems.IsOwned(ItemId))
        {
            popup.ShowMessage("Bạn đã sở hữu \"" + itemName + "\" rồi!");
            return;
        }

        popup.Show(itemName, () => ConfirmPurchase(itemName, currency));
    }

    private void ConfirmPurchase(string itemName, string currency)
    {
        int price = GetPrice(currency == "Diamond" ? diamondBuyButton : coinBuyButton);

        // TODO API: khi có API mua hàng, thay đoạn trừ tiền local dưới đây bằng gọi server
        bool paid = currency == "Diamond"
            ? CurrencyManager.TrySpendDiamonds(price)
            : CurrencyManager.TrySpendCoins(price);

        PurchaseConfirmPopup popup = PurchaseConfirmPopup.GetOrCreate(purchasePopupPrefab, this);
        if (!paid)
        {
            if (popup != null) popup.ShowMessage("Không đủ " + currency + " để mua \"" + itemName + "\"!");
            return;
        }

        OwnedItems.SetOwned(ItemId);
        RefreshOwnedState();
        Debug.Log("[Store] Mua thành công \"" + itemName + "\" với giá " + price + " " + currency);
    }

    // Lấy giá từ chữ số ghi trên nút mua (vd "50", "500")
    private int GetPrice(Button buyButton)
    {
        if (buyButton != null)
        {
            TMP_Text label = buyButton.GetComponentInChildren<TMP_Text>();
            int price;
            if (label != null && int.TryParse(label.text.Trim(), out price)) return price;
        }
        return 0;
    }

    private string ItemId
    {
        get
        {
            if (!string.IsNullOrEmpty(itemId)) return itemId;
            string title = itemTitleText != null ? itemTitleText.text : "";
            return gameObject.name.Replace("(Clone)", "").Trim() + "_" + title;
        }
    }

    private void RefreshOwnedState()
    {
        bool owned = OwnedItems.IsOwned(ItemId);
        if (lockedOverlay != null) lockedOverlay.SetActive(!owned);
        if (priceRow != null) priceRow.SetActive(!owned);
    }
}
