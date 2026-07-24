using System;
using UnityEngine;

/// <summary>
/// Local ownership storage. Server inventory can replace this later without changing UI flow.
/// </summary>
public static class PlayerInventory
{
    private const string OwnedPrefix = "Inventory.Owned.";

    public static event Action Changed;

    static PlayerInventory()
    {
        AccountSession.Changed += NotifyChanged;
    }

    public static bool IsOwned(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        return PlayerPrefs.GetInt(AccountSession.ScopedKey(OwnedPrefix + itemId), 0) == 1;
    }

    public static void SetOwned(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return;

        PlayerPrefs.SetInt(AccountSession.ScopedKey(OwnedPrefix + itemId), 1);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static void RemoveForTesting(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return;

        PlayerPrefs.DeleteKey(AccountSession.ScopedKey(OwnedPrefix + itemId));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    private static void NotifyChanged() => Changed?.Invoke();
}
