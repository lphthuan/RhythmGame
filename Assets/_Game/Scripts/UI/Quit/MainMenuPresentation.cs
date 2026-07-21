using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Builds the polished Gothic StartMenu at runtime while preserving existing scene navigation.</summary>
public class MainMenuPresentation : MonoBehaviour
{
    [SerializeField] private Sprite mainMenuBackground;
    private UIManager settingsManager;
    private Transform menuCanvas;
    private GameObject legacyPanel;
    private static Sprite whiteSprite;

    private void Awake()
    {
        Canvas canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>() ?? RuntimeCanvasUtility.FindSceneCanvas();
        if (canvas == null) return;
        menuCanvas = canvas.transform;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        BuildBackground(menuCanvas);
        FindSettingsManager();
    }

    private void Start()
    {
        if (menuCanvas == null) return;
        DisableLegacyMenu(menuCanvas);
        BuildModernMenu(menuCanvas);
        StartCoroutine(PlayMenuMusic());
        if (settingsManager != null) settingsManager.PrepareAsSettingsOverlay();
    }

    private IEnumerator PlayMenuMusic()
    {
        AudioClip clip = Resources.Load<AudioClip>("Audio/Beneath_the_Stained_Glass");
        if (clip == null)
        {
            Debug.LogWarning("MainMenuPresentation: Beneath_the_Stained_Glass audio clip was not found in Resources/Audio.");
            yield break;
        }

        Transform existing = transform.Find("RG Menu Music");
        GameObject musicObject = existing != null ? existing.gameObject : new GameObject("RG Menu Music", typeof(AudioSource));
        if (existing == null) musicObject.transform.SetParent(transform, false);
        AudioSource source = musicObject.GetComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.Play();

        const float targetVolume = 0.55f;
        const float fadeDuration = 1.8f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(0f, targetVolume, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }
        source.volume = targetVolume;
    }
    private void FindSettingsManager()
    {
        foreach (UIManager manager in FindObjectsByType<UIManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (manager != null && manager.gameObject.name == "CanvasThai") { settingsManager = manager; break; }
    }

    private void BuildBackground(Transform canvas)
    {
        Transform old = canvas.Find("RG Main Menu Background");
        if (old != null) Destroy(old.gameObject);
        RectTransform root = CreateRect("RG Main Menu Background", canvas, Vector2.zero, Vector2.one);
        root.SetAsFirstSibling();
        Image background = root.gameObject.AddComponent<Image>();
        background.sprite = mainMenuBackground;
        background.color = mainMenuBackground != null ? Color.white : new Color(0.025f, 0.02f, 0.06f, 1f);
        background.preserveAspect = false; background.raycastTarget = false;
        root.gameObject.AddComponent<GothicBackgroundMotion>();
        BuildMusicNotes(root);

        RectTransform shade = CreateRect("Left Menu Shade", root, Vector2.zero, new Vector2(0.54f, 1f));
        Image shadeImage = shade.gameObject.AddComponent<Image>();
        shadeImage.sprite = GetWhiteSprite(); shadeImage.color = new Color(0.005f, 0.005f, 0.025f, 0.33f); shadeImage.raycastTarget = false;
    }

    private static void BuildMusicNotes(RectTransform backgroundRoot)
    {
        RectTransform layer = CreateRect("Flying Music Notes", backgroundRoot, Vector2.zero, Vector2.one);
        layer.SetAsLastSibling();
        for (int i = 0; i < 14; i++)
        {
            RectTransform note = CreateRect("Music Note " + (i + 1), layer, new Vector2(0.08f + (i * 0.137f) % 0.82f, 0.08f + (i * 0.193f) % 0.72f), new Vector2(0.08f + (i * 0.137f) % 0.82f, 0.08f + (i * 0.193f) % 0.72f));
            note.sizeDelta = new Vector2(28f, 38f);
            Color color = i % 3 == 0 ? new Color(0.82f, 0.38f, 1f, 0.32f) : new Color(0.38f, 0.78f, 1f, 0.28f);

            RectTransform head = CreateRect("Head", note, new Vector2(0.42f, 0.2f), new Vector2(0.42f, 0.2f));
            head.sizeDelta = new Vector2(11f, 8f); head.localRotation = Quaternion.Euler(0f, 0f, -22f);
            Image headImage = head.gameObject.AddComponent<Image>(); headImage.sprite = GetWhiteSprite(); headImage.color = color; headImage.raycastTarget = false;

            RectTransform stem = CreateRect("Stem", note, new Vector2(0.58f, 0.53f), new Vector2(0.58f, 0.53f));
            stem.sizeDelta = new Vector2(2f, 24f);
            Image stemImage = stem.gameObject.AddComponent<Image>(); stemImage.sprite = GetWhiteSprite(); stemImage.color = color; stemImage.raycastTarget = false;

            RectTransform flag = CreateRect("Flag", note, new Vector2(0.72f, 0.79f), new Vector2(0.72f, 0.79f));
            flag.sizeDelta = new Vector2(12f, 3f); flag.localRotation = Quaternion.Euler(0f, 0f, -24f);
            Image flagImage = flag.gameObject.AddComponent<Image>(); flagImage.sprite = GetWhiteSprite(); flagImage.color = color; flagImage.raycastTarget = false;

            note.gameObject.AddComponent<FlyingMusicNoteFX>().Configure(i, new[] { headImage, stemImage, flagImage });
        }
    }
    private void DisableLegacyMenu(Transform canvas)
    {
        Transform panel = canvas.Find("Panel");
        if (panel != null) { legacyPanel = panel.gameObject; panel.gameObject.SetActive(false); }
        Transform oldRuntime = canvas.Find("RG Gothic Menu");
        if (oldRuntime != null) Destroy(oldRuntime.gameObject);
    }

    private void LateUpdate()
    {
        if (legacyPanel != null && legacyPanel.activeSelf) legacyPanel.SetActive(false);
    }

    private void BuildModernMenu(Transform canvas)
    {
        RectTransform root = CreateRect("RG Gothic Menu", canvas, new Vector2(0.055f, 0.12f), new Vector2(0.47f, 0.88f));
        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1f;

        CreateTitle(root);
        CreateRhythmSignature(root);
        string[] labels = { "START", "SETTING", "SHOP", "EXIT" };
        UnityEngine.Events.UnityAction[] actions = { StartGame, OpenSettings, OpenShop, QuitGame };
        float[] y = { 105f, 15f, -75f, -165f };
        for (int i = 0; i < labels.Length; i++)
            CreateGothicButton(root, labels[i], y[i], i == 0, actions[i], i * 0.09f, i + 1);
    }

    private static void CreateTitle(RectTransform root)
    {
        RectTransform line = CreateRect("Menu Header Line", root, new Vector2(0.16f, 0.83f), new Vector2(0.84f, 0.835f));
        Image image = line.gameObject.AddComponent<Image>(); image.sprite = GetWhiteSprite(); image.color = new Color(0.45f, 0.72f, 1f, 0.35f); image.raycastTarget = false;
        TextMeshProUGUI title = CreateText("MAIN MENU", root, new Vector2(0.5f, 0.89f), new Vector2(300f, 38f), 18f);
        title.color = new Color(0.82f, 0.88f, 1f, 0.72f); title.characterSpacing = 10f;
    }

    private static void CreateRhythmSignature(RectTransform root)
    {
        Texture2D logoTexture = Resources.Load<Texture2D>("StartMenuLogo/RhythmGameLogo");
        if (logoTexture != null)
        {
            RectTransform logoRect = CreateRect("Rhythm Game Logo", root, new Vector2(0.5f, 0.965f), new Vector2(0.5f, 0.965f));
            logoRect.sizeDelta = new Vector2(430f, 92f);
            Image logo = logoRect.gameObject.AddComponent<Image>();
            logo.sprite = Sprite.Create(logoTexture, new Rect(0f, 0f, logoTexture.width, logoTexture.height), new Vector2(0.5f, 0.5f), 100f);
            logo.preserveAspect = true; logo.color = Color.white; logo.raycastTarget = false;
        }
        else
        {
            TextMeshProUGUI brand = CreateText("RHYTHM GAME", root, new Vector2(0.5f, 0.965f), new Vector2(430f, 58f), 32f);
            brand.color = new Color(0.96f, 0.93f, 1f, 0.98f); brand.characterSpacing = 11f; brand.fontStyle = FontStyles.Bold;
        }
        TextMeshProUGUI subtitle = CreateText("ENTER THE CATHEDRAL OF SOUND", root, new Vector2(0.5f, 0.865f), new Vector2(380f, 18f), 9f);
        subtitle.color = new Color(0.48f, 0.72f, 1f, 0.58f); subtitle.characterSpacing = 5f;
        TextMeshProUGUI footer = CreateText("AUDIO LINK  //  READY", root, new Vector2(0.5f, 0.055f), new Vector2(300f, 22f), 10f);
        footer.color = new Color(0.66f, 0.52f, 0.88f, 0.48f); footer.characterSpacing = 4f;

        RectTransform pulse = CreateRect("Rhythm Pulse", root, new Vector2(0.5f, 0.105f), new Vector2(0.5f, 0.105f));
        pulse.sizeDelta = new Vector2(160f, 28f);
        List<RectTransform> bars = new();
        for (int i = 0; i < 11; i++)
        {
            RectTransform bar = CreateRect("Pulse " + i, pulse, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            bar.sizeDelta = new Vector2(3f, 8f); bar.anchoredPosition = new Vector2((i - 5) * 12f, 0f);
            Image image = bar.gameObject.AddComponent<Image>(); image.sprite = GetWhiteSprite(); image.color = new Color(0.43f, 0.76f, 1f, 0.42f); image.raycastTarget = false;
            bars.Add(bar);
        }
        pulse.gameObject.AddComponent<MenuRhythmPulse>().Configure(bars);
    }
    private static void CreateGothicButton(RectTransform root, string label, float y, bool primary, UnityEngine.Events.UnityAction action, float delay, int index)
    {
        GameObject go = new("RG " + label + " Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CanvasGroup));
        RectTransform rect = go.GetComponent<RectTransform>(); rect.SetParent(root, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y); rect.sizeDelta = primary ? new Vector2(390f, 82f) : new Vector2(350f, 70f);

        Image bg = go.GetComponent<Image>(); bg.sprite = GetWhiteSprite(); bg.color = new Color(0.025f, 0.018f, 0.075f, 0.92f);
        Button button = go.GetComponent<Button>(); button.targetGraphic = bg; button.transition = Selectable.Transition.None; button.onClick.AddListener(action);

        AddBorder(rect, new Color(0.55f, 0.82f, 1f, 0.82f), 2f, 0f);
        AddBorder(rect, new Color(0.86f, 0.38f, 1f, 0.58f), 1f, -6f);
        AddDiamond(rect, new Vector2(-0.5f, 0f)); AddDiamond(rect, new Vector2(0.5f, 0f));
        TextMeshProUGUI text = CreateText(label, rect, new Vector2(0.5f, 0.5f), rect.sizeDelta, primary ? 35f : 25f);
        text.color = new Color(0.96f, 0.91f, 1f, 1f); text.characterSpacing = primary ? 8f : 6f; text.fontStyle = FontStyles.Bold;

        TextMeshProUGUI number = CreateText("0" + index, rect, new Vector2(0.08f, 0.5f), new Vector2(42f, 24f), 11f);
        number.color = new Color(0.55f, 0.78f, 1f, 0.48f); number.characterSpacing = 2f;
        RectTransform markerRect = CreateRect("Selection Marker", rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        markerRect.sizeDelta = new Vector2(12f, 12f); markerRect.anchoredPosition = new Vector2(-30f, 0f); markerRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image marker = markerRect.gameObject.AddComponent<Image>(); marker.sprite = GetWhiteSprite(); marker.color = new Color(0.45f, 0.85f, 1f, 0.08f); marker.raycastTarget = false;
        RectTransform sweepRect = CreateRect("Light Sweep", rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
        sweepRect.sizeDelta = new Vector2(3f, rect.sizeDelta.y * 0.72f); sweepRect.anchoredPosition = new Vector2(-rect.sizeDelta.x * 0.46f, 0f);
        Image sweep = sweepRect.gameObject.AddComponent<Image>(); sweep.sprite = GetWhiteSprite(); sweep.color = new Color(0.65f, 0.92f, 1f, 0f); sweep.raycastTarget = false;
        GothicMenuButtonFX fx = go.AddComponent<GothicMenuButtonFX>();
        fx.Configure(primary, delay, bg, text, marker, sweepRect);
    }

    private static void AddBorder(RectTransform parent, Color color, float thickness, float inset)
    {
        CreateLine(parent, "Top", new Vector2(0.5f, 1f), new Vector2(inset * 2f, thickness), new Vector2(0f, inset), color);
        CreateLine(parent, "Bottom", new Vector2(0.5f, 0f), new Vector2(inset * 2f, thickness), new Vector2(0f, -inset), color);
        CreateLine(parent, "Left", new Vector2(0f, 0.5f), new Vector2(thickness, inset * 2f), new Vector2(inset, 0f), color, true);
        CreateLine(parent, "Right", new Vector2(1f, 0.5f), new Vector2(thickness, inset * 2f), new Vector2(-inset, 0f), color, true);
    }

    private static void CreateLine(RectTransform parent, string name, Vector2 anchor, Vector2 adjustment, Vector2 position, Color color, bool vertical = false)
    {
        RectTransform line = CreateRect(name, parent, anchor, anchor);
        line.anchorMin = line.anchorMax = anchor; line.pivot = new Vector2(0.5f, 0.5f); line.anchoredPosition = position;
        line.sizeDelta = vertical ? new Vector2(adjustment.x, parent.sizeDelta.y + adjustment.y) : new Vector2(parent.sizeDelta.x + adjustment.x, adjustment.y);
        Image image = line.gameObject.AddComponent<Image>(); image.sprite = GetWhiteSprite(); image.color = color; image.raycastTarget = false;
    }

    private static void AddDiamond(RectTransform parent, Vector2 side)
    {
        RectTransform diamond = CreateRect("Gothic Accent", parent, new Vector2(side.x < 0 ? 0f : 1f, 0.5f), new Vector2(side.x < 0 ? 0f : 1f, 0.5f));
        diamond.sizeDelta = new Vector2(20f, 20f); diamond.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image image = diamond.gameObject.AddComponent<Image>(); image.sprite = GetWhiteSprite(); image.color = new Color(0.55f, 0.82f, 1f, 0.65f); image.raycastTarget = false;
        RectTransform inner = CreateRect("Inner", diamond, new Vector2(0.23f, 0.23f), new Vector2(0.77f, 0.77f));
        Image innerImage = inner.gameObject.AddComponent<Image>(); innerImage.sprite = GetWhiteSprite(); innerImage.color = new Color(0.04f, 0.02f, 0.10f, 1f); innerImage.raycastTarget = false;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        GameObject go = new(name, typeof(RectTransform)); RectTransform rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; return rect;
    }

    private static TextMeshProUGUI CreateText(string value, Transform parent, Vector2 anchor, Vector2 size, float fontSize)
    {
        RectTransform rect = CreateRect("Text " + value, parent, anchor, anchor); rect.sizeDelta = size;
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap; text.raycastTarget = false; TmpRuntimeFontFallback.Apply(text); return text;
    }

    internal static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        Texture2D texture = new(2, 2, TextureFormat.RGBA32, false); Color[] pixels = { Color.white, Color.white, Color.white, Color.white };
        texture.SetPixels(pixels); texture.Apply(); whiteSprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f); return whiteSprite;
    }

    private static void StartGame() => SceneLoadUtility.LoadSceneByName("SongSelect");
    private void OpenSettings() { if (settingsManager == null) FindSettingsManager(); if (settingsManager != null) { settingsManager.PrepareAsSettingsOverlay(); settingsManager.OpenSettings(); } }
    private static void OpenShop() => SceneLoadUtility.LoadSceneByName("StoreMenu");
    private static void QuitGame()
    {
        Debug.Log("Thanks for playing!");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

internal sealed class GothicMenuButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private bool primary, hovered, pressed; private float delay; private Image background, marker; private TMP_Text label; private RectTransform sweep; private Vector3 baseScale; private CanvasGroup group; private float sweepPhase;
    public void Configure(bool isPrimary, float entranceDelay, Image bg, TMP_Text text, Image selectionMarker, RectTransform lightSweep) { primary = isPrimary; delay = entranceDelay; background = bg; label = text; marker = selectionMarker; sweep = lightSweep; }
    private void Awake() { baseScale = transform.localScale; group = GetComponent<CanvasGroup>(); group.alpha = 0f; transform.localScale = baseScale * 0.86f; }
    private IEnumerator Start() { yield return new WaitForSecondsRealtime(delay + 0.08f); float t = 0f; while (t < 1f) { t += Time.unscaledDeltaTime * 5f; float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f); group.alpha = e; transform.localScale = Vector3.Lerp(baseScale * 0.86f, baseScale, e); yield return null; } }
    private void Update()
    {
        float scale = pressed ? 0.965f : hovered ? 1.045f : 1f;
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * scale, Time.unscaledDeltaTime * 15f);
        if (background != null) background.color = Color.Lerp(background.color, hovered ? (primary ? new Color(.03f,.18f,.28f,.96f) : new Color(.16f,.04f,.25f,.96f)) : new Color(.025f,.018f,.075f,.92f), Time.unscaledDeltaTime * 10f);
        if (label != null) label.color = Color.Lerp(label.color, hovered ? Color.white : new Color(.96f,.91f,1f,1f), Time.unscaledDeltaTime * 10f);
        if (marker != null) { Color c = marker.color; c.a = Mathf.Lerp(c.a, hovered ? .95f : .08f, Time.unscaledDeltaTime * 12f); marker.color = c; marker.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 5f) * .12f); }
        if (sweep != null)
        {
            sweepPhase = hovered ? Mathf.Repeat(sweepPhase + Time.unscaledDeltaTime * 1.25f, 1f) : 0f;
            sweep.anchoredPosition = new Vector2(Mathf.Lerp(-transform.GetComponent<RectTransform>().sizeDelta.x * .46f, transform.GetComponent<RectTransform>().sizeDelta.x * .46f, sweepPhase), 0f);
            Image image = sweep.GetComponent<Image>(); Color c = image.color; c.a = hovered ? Mathf.Sin(sweepPhase * Mathf.PI) * .48f : 0f; image.color = c;
        }
    }
    public void OnPointerEnter(PointerEventData e) { hovered = true; } public void OnPointerExit(PointerEventData e) { hovered = false; pressed = false; } public void OnPointerDown(PointerEventData e) { pressed = true; } public void OnPointerUp(PointerEventData e) { pressed = false; }
}




internal sealed class MenuRhythmPulse : MonoBehaviour
{
    private List<RectTransform> bars;
    public void Configure(List<RectTransform> value) { bars = value; }
    private void Update()
    {
        if (bars == null) return;
        float beat = Time.unscaledTime * 2.15f;
        for (int i = 0; i < bars.Count; i++)
        {
            float wave = Mathf.Abs(Mathf.Sin(beat + i * 0.62f));
            Vector2 size = bars[i].sizeDelta; size.y = 5f + wave * (i == 5 ? 21f : 13f); bars[i].sizeDelta = size;
        }
    }
}

internal sealed class GothicBackgroundMotion : MonoBehaviour
{
    private RectTransform rect;
    private Image image;
    private void Awake() { rect = GetComponent<RectTransform>(); image = GetComponent<Image>(); }
    private void Update()
    {
        float breathe = (Mathf.Sin(Time.unscaledTime * 0.28f) + 1f) * 0.5f;
        rect.localScale = Vector3.one * Mathf.Lerp(1.006f, 1.018f, breathe);
        rect.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 0.13f) * 4f, Mathf.Cos(Time.unscaledTime * 0.17f) * 2f);
        if (image != null) image.color = Color.Lerp(new Color(.91f,.93f,1f,1f), Color.white, breathe);
    }
}









internal sealed class FlyingMusicNoteFX : MonoBehaviour
{
    private RectTransform rect;
    private Image[] images;
    private Vector2 origin;
    private float phase;
    private float speed;
    private float drift;
    private float baseAlpha;

    public void Configure(int index, Image[] noteImages)
    {
        images = noteImages;
        phase = index * 0.071f;
        speed = 0.045f + (index % 5) * 0.009f;
        drift = 9f + (index % 4) * 5f;
        baseAlpha = index % 3 == 0 ? 0.32f : 0.28f;
    }

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        origin = rect.anchoredPosition;
    }

    private void Update()
    {
        phase = Mathf.Repeat(phase + Time.unscaledDeltaTime * speed, 1f);
        float rise = Mathf.Lerp(-45f, 170f, phase);
        float sway = Mathf.Sin(Time.unscaledTime * 0.75f + transform.GetSiblingIndex() * 1.37f) * drift;
        rect.anchoredPosition = origin + new Vector2(sway, rise);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 0.55f + transform.GetSiblingIndex()) * 8f);
        float fade = Mathf.Sin(phase * Mathf.PI) * baseAlpha;
        if (images == null) return;
        foreach (Image image in images)
        {
            if (image == null) continue;
            Color color = image.color; color.a = fade; image.color = color;
        }
    }
}
