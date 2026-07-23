using System;

/// <summary>One gameplay-wide authority for temporarily refusing new note input.</summary>
public static class GameplayInputGate
{
    private static bool _isBlocked;
    public static bool IsBlocked => _isBlocked;
    public static event Action<bool> Changed;

    public static void SetBlocked(bool blocked)
    {
        if (_isBlocked == blocked)
            return;

        _isBlocked = blocked;
        Changed?.Invoke(blocked);
    }
}
