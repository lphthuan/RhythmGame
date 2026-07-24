using System;
using UnityEngine;

/// <summary>
/// Local wallet used while account/server work is not merged yet.
/// Later this class can become a cache over server wallet responses.
/// </summary>
public static class PlayerWallet
{
    private const string MoneyKey = "Wallet.Money";
    private const string DiamondKey = "Wallet.Diamond";

    public static event Action Changed;

    static PlayerWallet()
    {
        AccountSession.Changed += NotifyChanged;
    }

    public static int Money => PlayerPrefs.GetInt(AccountSession.ScopedKey(MoneyKey), 0);
    public static int Diamond => PlayerPrefs.GetInt(AccountSession.ScopedKey(DiamondKey), 0);

    public static bool CanSpend(CurrencyType currency, int amount)
    {
        if (amount < 0) return false;
        return GetBalance(currency) >= amount;
    }

    public static bool TrySpend(CurrencyType currency, int amount)
    {
        if (!CanSpend(currency, amount))
            return false;

        SetBalance(currency, GetBalance(currency) - amount);
        return true;
    }

    public static void Add(CurrencyType currency, int amount)
    {
        if (amount <= 0)
            return;

        SetBalance(currency, GetBalance(currency) + amount);
    }

    public static void ResetForTesting()
    {
        PlayerPrefs.DeleteKey(AccountSession.ScopedKey(MoneyKey));
        PlayerPrefs.DeleteKey(AccountSession.ScopedKey(DiamondKey));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static int GetBalance(CurrencyType currency)
    {
        return currency == CurrencyType.Diamond ? Diamond : Money;
    }

    private static void SetBalance(CurrencyType currency, int value)
    {
        string key = currency == CurrencyType.Diamond ? DiamondKey : MoneyKey;
        PlayerPrefs.SetInt(AccountSession.ScopedKey(key), Mathf.Max(0, value));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    private static void NotifyChanged() => Changed?.Invoke();
}
