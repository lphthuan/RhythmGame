using System.Collections;
using TMPro;
using UnityEngine;

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

    private void OnEnable()
    {
        shownCoins = CurrencyManager.Coins;
        shownDiamonds = CurrencyManager.Diamonds;
        SetLabel(coinText, shownCoins);
        SetLabel(diamondText, shownDiamonds);
        CurrencyManager.OnChanged += Refresh;
    }

    private void OnDisable()
    {
        CurrencyManager.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        if (coinRoutine != null) StopCoroutine(coinRoutine);
        if (diamondRoutine != null) StopCoroutine(diamondRoutine);
        coinRoutine = StartCoroutine(CountTo(coinText, shownCoins, CurrencyManager.Coins, true));
        diamondRoutine = StartCoroutine(CountTo(diamondText, shownDiamonds, CurrencyManager.Diamonds, false));
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
}
