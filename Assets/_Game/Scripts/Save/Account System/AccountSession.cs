using System;
using UnityEngine;

/// <summary>
/// Tracks the account currently active on this device. Game data is scoped by
/// this session so one member cannot see another member's local progress.
/// Signing out only clears the active session; it never deletes that member's
/// saved profile on the device.
/// </summary>
public static class AccountSession
{
    private const string SessionUsernameKey = "RhythmGame.Auth.SessionUsername";
    private const string LegacyUsernameKey = "CurrentUsername";
    private const string LegacyModeKey = "UserMode";

    public static event Action Changed;

    public static string CurrentUsername
    {
        get
        {
            string current = PlayerPrefs.GetString(SessionUsernameKey, string.Empty).Trim();
            // Preserve the session of users who logged in before account-scoped
            // storage was introduced.
            return string.IsNullOrEmpty(current)
                ? PlayerPrefs.GetString(LegacyUsernameKey, string.Empty).Trim()
                : current;
        }
    }

    public static bool IsSignedIn => !string.IsNullOrEmpty(CurrentUsername);

    public static string StorageId => IsSignedIn
        ? "member." + Uri.EscapeDataString(CurrentUsername.ToLowerInvariant())
        : "guest";

    public static string ScopedKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Storage key is required.", nameof(key));

        return "RhythmGame.Account." + StorageId + "." + key;
    }

    public static void SignIn(string username)
    {
        username = username?.Trim();
        if (string.IsNullOrEmpty(username))
            return;

        string previous = CurrentUsername;
        PlayerPrefs.SetString(SessionUsernameKey, username);
        PlayerPrefs.SetString(LegacyUsernameKey, username);
        PlayerPrefs.SetString(LegacyModeKey, "Member");
        PlayerPrefs.Save();

        if (!string.Equals(previous, username, StringComparison.OrdinalIgnoreCase))
            Changed?.Invoke();
    }

    public static void SignOut()
    {
        if (!IsSignedIn)
            return;

        PlayerPrefs.DeleteKey(SessionUsernameKey);
        PlayerPrefs.DeleteKey(LegacyUsernameKey);
        PlayerPrefs.DeleteKey(LegacyModeKey);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
