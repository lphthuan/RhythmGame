using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Local skin catalogue and equipped-skin state.  Ownership uses the same
/// inventory/wallet seam as songs, so a server can later become authoritative.
/// </summary>
public static class NoteSkinService
{
    public const string NightSkyId = "skin:night-sky";
    public const string ArcaeaConflictSideId = "skin:arcaea-conflict-side";
    public const int ArcaeaConflictSidePrice = 500;
    private const string EquippedSkinKey = "NoteSkin.Equipped";
    private const string ResourceRoot = "NoteSkins/ArcaeaConflictSide/";

    private static SkinSprites _arcaea;
    public static event System.Action Changed;

    static NoteSkinService()
    {
        AccountSession.Changed += NotifyAccountChanged;
    }

    public static string EquippedSkinId => PlayerPrefs.GetString(AccountSession.ScopedKey(EquippedSkinKey), string.Empty);
    public static bool IsOwned(string skinId) => skinId == NightSkyId || PlayerInventory.IsOwned(skinId);
    public static bool IsEquipped(string skinId) => skinId == NightSkyId
        ? string.IsNullOrWhiteSpace(EquippedSkinId)
        : EquippedSkinId == skinId;

    public static bool TryPurchaseArcaea(out string message)
        => TryPurchase(ArcaeaConflictSideId, ArcaeaConflictSidePrice, "Arcaea ConflictSide", out message);

    private static bool TryPurchase(string skinId, int price, string title, out string message)
    {
        if (IsOwned(skinId))
        {
            message = "Skin này đã sở hữu.";
            return true;
        }

        if (!PlayerWallet.TrySpend(CurrencyType.Money, price))
        {
            message = $"Không đủ Money. Cần {price}.";
            return false;
        }

        PlayerInventory.SetOwned(skinId);
        WalletTransactionJournal.Record("note_skin_purchase", CurrencyType.Money, price, skinId);
        message = $"Đã mở khóa {title}.";
        return true;
    }

    public static bool TryEquip(string skinId, out string message)
    {
        if (skinId == NightSkyId)
        {
            PlayerPrefs.DeleteKey(AccountSession.ScopedKey(EquippedSkinKey));
            PlayerPrefs.Save();
            Changed?.Invoke();
            message = "Đã áp dụng Night Sky.";
            return true;
        }

        if (skinId != ArcaeaConflictSideId || !IsOwned(skinId))
        {
            message = "Bạn chưa sở hữu skin này.";
            return false;
        }

        PlayerPrefs.SetString(AccountSession.ScopedKey(EquippedSkinKey), skinId);
        PlayerPrefs.Save();
        Changed?.Invoke();
        ApplyToCurrentScene();
        message = "Đã áp dụng skin.";
        return true;
    }

    public static void ApplyTo(NoteBase note)
    {
        if (note == null)
            return;

        SkinSprites sprites;
        if (IsEquipped(ArcaeaConflictSideId))
            sprites = GetArcaeaSprites();
        else
            return;

        if (note is HoldNote hold)
            hold.ApplySkinSprites(sprites.holdHead, sprites.holdBody, sprites.holdTail);
        else
            note.ApplySkinSprite(sprites.tap);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded += (_, __) => ApplyToCurrentScene();
        ApplyToCurrentScene();
    }

    public static void ApplyToCurrentScene()
    {
        SkinSprites sprites;
        if (IsEquipped(ArcaeaConflictSideId))
        {
            sprites = GetArcaeaSprites();
        }
        else
            return;

        foreach (GameplayLaneLayout layout in Object.FindObjectsByType<GameplayLaneLayout>(FindObjectsSortMode.None))
            layout.ApplySkinSprites(sprites.stageLeft, sprites.stageRight, sprites.stageHint,
                sprites.stageBottom, sprites.stageLight);
    }

    private static SkinSprites GetArcaeaSprites()
    {
        if (_arcaea.loaded)
            return _arcaea;

        _arcaea = new SkinSprites
        {
            loaded = true,
            tap = Resources.Load<Sprite>(ResourceRoot + "tap"),
            holdHead = Resources.Load<Sprite>(ResourceRoot + "hold-head"),
            holdBody = Resources.Load<Sprite>(ResourceRoot + "hold-body"),
            holdTail = Resources.Load<Sprite>(ResourceRoot + "hold-tail"),
            stageLeft = Resources.Load<Sprite>(ResourceRoot + "stage-left"),
            stageRight = Resources.Load<Sprite>(ResourceRoot + "stage-right"),
            stageHint = Resources.Load<Sprite>(ResourceRoot + "stage-hint"),
            stageBottom = Resources.Load<Sprite>(ResourceRoot + "stage-bottom"),
            stageLight = Resources.Load<Sprite>(ResourceRoot + "stage-light")
        };
        return _arcaea;
    }

    private struct SkinSprites
    {
        public bool loaded;
        public Sprite tap, holdHead, holdBody, holdTail, stageLeft, stageRight, stageHint, stageBottom, stageLight;
    }

    private static void NotifyAccountChanged()
    {
        Changed?.Invoke();
        ApplyToCurrentScene();
    }
}
