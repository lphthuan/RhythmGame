using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Local transaction history used until the account service becomes authoritative.
/// Server sync can later submit these IDs idempotently instead of re-creating UI rules.
/// </summary>
public static class WalletTransactionJournal
{
    private const string StorageKey = "Wallet.TransactionJournal";
    private const int MaximumEntries = 100;

    [Serializable]
    public class Entry
    {
        public string transactionId;
        public string kind;
        public string currency;
        public int amount;
        public string itemId;
        public long createdAtUnixSeconds;
    }

    [Serializable]
    private class EntryCollection
    {
        public List<Entry> entries = new();
    }

    public static void Record(string kind, CurrencyType currency, int amount, string itemId = "")
    {
        if (amount <= 0)
            return;

        EntryCollection collection = Load();
        collection.entries.Add(new Entry
        {
            transactionId = Guid.NewGuid().ToString("N"),
            kind = kind,
            currency = currency.ToString(),
            amount = amount,
            itemId = itemId ?? string.Empty,
            createdAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });

        int excess = collection.entries.Count - MaximumEntries;
        if (excess > 0)
            collection.entries.RemoveRange(0, excess);

        PlayerPrefs.SetString(AccountSession.ScopedKey(StorageKey), JsonUtility.ToJson(collection));
        PlayerPrefs.Save();
    }

    public static IReadOnlyList<Entry> GetEntries()
    {
        return Load().entries;
    }

    private static EntryCollection Load()
    {
        string json = PlayerPrefs.GetString(AccountSession.ScopedKey(StorageKey), string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return new EntryCollection();

        EntryCollection collection = JsonUtility.FromJson<EntryCollection>(json);
        return collection ?? new EntryCollection();
    }
}
