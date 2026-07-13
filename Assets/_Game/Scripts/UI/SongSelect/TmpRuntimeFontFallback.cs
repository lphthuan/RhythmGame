using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class TmpRuntimeFontFallback
{
    private const string ResourceFontPath = "Fonts/NotoSansCJKjp-Regular";
    private static TMP_FontAsset _fallbackFont;
    private static bool _triedLoadFallback;

    public static void Apply(TextMeshProUGUI label)
    {
        if (label == null)
            return;

        TMP_FontAsset fallback = FindProjectFallbackFont();
        if (fallback == null || label.font == null || label.font.fallbackFontAssetTable == null)
            return;

        if (!label.font.fallbackFontAssetTable.Contains(fallback))
            label.font.fallbackFontAssetTable.Add(fallback);
    }

    private static TMP_FontAsset FindProjectFallbackFont()
    {
        if (_fallbackFont != null)
            return _fallbackFont;

        if (!_triedLoadFallback)
        {
            _triedLoadFallback = true;
            _fallbackFont = CreateFallbackFromResource();
            if (_fallbackFont != null)
                return _fallbackFont;
        }

        if (TMP_Settings.fallbackFontAssets == null)
            return null;

        foreach (TMP_FontAsset fontAsset in TMP_Settings.fallbackFontAssets)
        {
            if (fontAsset != null)
                return fontAsset;
        }

        return null;
    }

    private static TMP_FontAsset CreateFallbackFromResource()
    {
        Font sourceFont = Resources.Load<Font>(ResourceFontPath);
        if (sourceFont == null)
            return null;

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            4096,
            4096,
            AtlasPopulationMode.Dynamic);

        if (fontAsset == null)
            return null;

        fontAsset.name = "NotoSansCJKjp Runtime TMP Fallback";
        if (TMP_Settings.fallbackFontAssets != null && !TMP_Settings.fallbackFontAssets.Contains(fontAsset))
            TMP_Settings.fallbackFontAssets.Add(fontAsset);

        return fontAsset;
    }
}
