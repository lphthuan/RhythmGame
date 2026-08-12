using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn trên Header: hiển thị số dư Coin / Diamond và chạy hiệu ứng đếm số khi thay đổi.
/// </summary>
public class CurrencyDisplay : MonoBehaviour
{
    public TMP_Text coinText;
    public TMP_Text diamondText;

    [Tooltip("Thời gian hiệu ứng số chạy (giây)")]
    public float countDuration = 0.4f;

    private int shownCoins;
    private int shownDiamonds;
    private Coroutine coinRoutine;
    private Coroutine diamondRoutine;
    private Coroutine walletRoutine;

    private void OnEnable()
    {
        CreateRechargeButton(coinText, "RC");
        CreateRechargeButton(diamondText, "DIAMOND");
        shownCoins = PlayerWallet.Money;
        shownDiamonds = PlayerWallet.Diamond;
        SetLabel(coinText, shownCoins);
        SetLabel(diamondText, shownDiamonds);
        PlayerWallet.Changed += Refresh;
        walletRoutine = StartCoroutine(RefreshWalletLoop());
    }

    private void OnDisable()
    {
        PlayerWallet.Changed -= Refresh;
        if (walletRoutine != null)
        {
            StopCoroutine(walletRoutine);
            walletRoutine = null;
        }
    }

    private void Refresh()
    {
        if (coinRoutine != null) StopCoroutine(coinRoutine);
        if (diamondRoutine != null) StopCoroutine(diamondRoutine);
        coinRoutine = StartCoroutine(CountTo(coinText, shownCoins, PlayerWallet.Money, true));
        diamondRoutine = StartCoroutine(CountTo(diamondText, shownDiamonds, PlayerWallet.Diamond, false));
    }

    private IEnumerator CountTo(TMP_Text label, int from, int to, bool isCoin)
    {
        float t = 0f;
        float duration = Mathf.Max(0.01f, countDuration);
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            int value = Mathf.RoundToInt(Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
            SetLabel(label, value);
            if (isCoin) shownCoins = value; else shownDiamonds = value;
            yield return null;
        }
        SetLabel(label, to);
        if (isCoin) { shownCoins = to; coinRoutine = null; }
        else { shownDiamonds = to; diamondRoutine = null; }
    }

    private static void SetLabel(TMP_Text label, int value)
    {
        if (label != null) label.text = value.ToString();
    }

    private System.Collections.IEnumerator RefreshWalletLoop()
    {
        while (isActiveAndEnabled)
        {
            CloudSyncManager.GetOrCreate().RefreshWallet();

            // Do not hammer the local API, but retry automatically if Unity
            // entered Play mode before the backend finished starting.
            yield return new WaitForSecondsRealtime(3f);
        }
    }

    private static void CreateRechargeButton(TMP_Text currencyText, string currency)
    {
        if (currencyText == null || currencyText.transform.parent == null)
            return;

        string buttonName = "Recharge " + currency + " Button";
        if (currencyText.transform.parent.Find(buttonName) != null)
            return;

        GameObject buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(currencyText.transform.parent, false);
        RectTransform labelRect = currencyText.rectTransform;
        rect.anchorMin = rect.anchorMax = labelRect.anchorMax;
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(30f, 30f);
        rect.anchoredPosition = labelRect.anchoredPosition + new Vector2(labelRect.sizeDelta.x * 0.5f + 8f, 0f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.3f, 0.15f, 0.55f, 0.9f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() =>
        {
            string playerId = SaveManager.Instance != null ? SaveManager.Instance.GetPlayerId() : null;
            if (string.IsNullOrEmpty(playerId) && AccountSession.IsSignedIn)
                playerId = AccountSession.CurrentUsername;
            if (string.IsNullOrEmpty(playerId)) playerId = "demo-player";
            Application.OpenURL("http://localhost:5173/?playerId=" + UnityEngine.Networking.UnityWebRequest.EscapeURL(playerId));
        });

        GameObject textObject = new GameObject("Plus", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        TextMeshProUGUI plus = textObject.GetComponent<TextMeshProUGUI>();
        plus.text = "+";
        plus.font = TMP_Settings.defaultFontAsset;
        plus.fontSize = 20f;
        plus.alignment = TextAlignmentOptions.Center;
        plus.color = Color.white;
        plus.raycastTarget = false;
    }
}
