using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn trên SongItemSlot trong Store.
/// Bấm nút Play để nghe thử 1 đoạn bài hát, hết đoạn tự dừng.
/// Ô previewClip để trống - bạn tự kéo file nhạc vào để test.
/// </summary>
public class SongPreviewButton : MonoBehaviour
{
    [Header("Âm thanh nghe thử (ĐỂ TRỐNG - bạn tự kéo bài hát vào)")]
    public AudioClip previewClip;

    [Header("Cài đặt nghe thử")]
    [Tooltip("Bắt đầu phát từ giây thứ mấy của bài hát")]
    public float previewStartTime = 0f;
    [Tooltip("Phát thử trong bao nhiêu giây rồi tự dừng")]
    public float previewDuration = 10f;
    [Range(0f, 1f)]
    public float previewVolume = 1f;

    [Header("UI")]
    public Button playButton;
    public TMP_Text playIcon; // hiện ">" khi đang dừng, "II" khi đang phát

    [Header("Nút mua (API sẽ tích hợp sau)")]
    public Button diamondBuyButton;
    public Button coinBuyButton;

    [Header("Xác nhận mua hàng")]
    [Tooltip("Kéo prefab PurchaseConfirmPopup vào đây")]
    public PurchaseConfirmPopup purchasePopupPrefab;
    [Tooltip("Tên sản phẩm hiển thị trong popup (kéo SongTitle vào)")]
    public TMP_Text songTitleText;

    [Header("Trạng thái sở hữu")]
    [Tooltip("Mã sản phẩm. Để trống = tự tạo từ tên object + tên bài hát")]
    public string itemId;
    [Tooltip("Lớp phủ khóa - hiện khi CHƯA mua")]
    public GameObject lockedOverlay;
    [Tooltip("Hàng nút mua - ẩn khi ĐÃ mua")]
    public GameObject priceRow;

    private AudioSource audioSource;
    private Coroutine previewRoutine;

    // Đảm bảo chỉ 1 bài được phát thử tại 1 thời điểm
    private static SongPreviewButton currentPlaying;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        if (playButton != null) playButton.onClick.AddListener(TogglePreview);
        if (diamondBuyButton != null) diamondBuyButton.onClick.AddListener(BuyWithDiamond);
        if (coinBuyButton != null) coinBuyButton.onClick.AddListener(BuyWithCoin);

        UpdateIcon(false);
        RefreshOwnedState();
    }

    public void TogglePreview()
    {
        if (currentPlaying == this && audioSource.isPlaying)
        {
            StopPreview();
        }
        else
        {
            StartPreview();
        }
    }

    public void StartPreview()
    {
        if (previewClip == null)
        {
            Debug.LogWarning("[Store] Chưa gán previewClip cho " + gameObject.name +
                             " - hãy kéo 1 file nhạc vào ô Preview Clip để test.");
            return;
        }

        // Dừng bài khác nếu đang phát
        if (currentPlaying != null && currentPlaying != this)
        {
            currentPlaying.StopPreview();
        }
        currentPlaying = this;

        audioSource.clip = previewClip;
        audioSource.volume = previewVolume;
        audioSource.time = Mathf.Clamp(previewStartTime, 0f, Mathf.Max(0f, previewClip.length - 0.1f));
        audioSource.Play();

        if (previewRoutine != null) StopCoroutine(previewRoutine);
        previewRoutine = StartCoroutine(StopAfterDuration());

        UpdateIcon(true);
    }

    public void StopPreview()
    {
        if (previewRoutine != null)
        {
            StopCoroutine(previewRoutine);
            previewRoutine = null;
        }

        audioSource.Stop();
        if (currentPlaying == this) currentPlaying = null;

        UpdateIcon(false);
    }

    private IEnumerator StopAfterDuration()
    {
        float timer = 0f;
        while (timer < previewDuration && audioSource.isPlaying)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        previewRoutine = null;
        StopPreview();
    }

    private void UpdateIcon(bool isPlaying)
    {
        if (playIcon != null)
        {
            playIcon.text = isPlaying ? "II" : ">";
        }
    }

    private void OnDisable()
    {
        StopPreview();
    }

    // ================== PHẦN MUA HÀNG ==================
    // Bấm nút mua -> hiện popup xác nhận -> bấm "Xác nhận" mới thực sự mua.

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
        string itemName = songTitleText != null ? songTitleText.text : gameObject.name;

        PurchaseConfirmPopup popup = GetPopup();
        if (popup == null)
        {
            Debug.LogWarning("[Store] Chưa gán Purchase Popup Prefab cho " + gameObject.name);
            return;
        }

        StopPreview(); // đang nghe thử thì dừng lại

        if (OwnedItems.IsOwned(ItemId))
        {
            popup.ShowMessage("Bạn đã sở hữu \"" + itemName + "\" rồi!");
            return;
        }

        popup.Show(itemName, () => ConfirmPurchase(itemName, currency));
    }

    private PurchaseConfirmPopup GetPopup()
    {
        return PurchaseConfirmPopup.GetOrCreate(purchasePopupPrefab, this);
    }

    private void ConfirmPurchase(string itemName, string currency)
    {
        int price = GetPrice(currency == "Diamond" ? diamondBuyButton : coinBuyButton);

        // TODO API: khi có API mua hàng, thay đoạn trừ tiền local dưới đây bằng gọi server
        bool paid = currency == "Diamond"
            ? CurrencyManager.TrySpendDiamonds(price)
            : CurrencyManager.TrySpendCoins(price);

        PurchaseConfirmPopup popup = GetPopup();
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
            string title = songTitleText != null ? songTitleText.text : "";
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
