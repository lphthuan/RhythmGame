using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Collects live note judgments and reveals the result overlay once the chart is finished.</summary>
public class GameplayResultController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NoteManager noteManager;
    [SerializeField] private ChartNoteSpawner chartSpawner;
    [SerializeField] private ComboManager comboManager;
    [SerializeField] private GameObject resultOverlay;
    [SerializeField] private ResultPro resultPro;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float showDelaySeconds = 2f;

    [Header("Navigation")]
    [SerializeField] private string songSelectSceneName = "MusicSelectionScene";
    [SerializeField] private bool wireResultButtons = true;

    private int _perfect;
    private int _great;
    private int _good;
    private int _miss;
    private bool _resultRequested;

    public void Configure(GameObject overlay)
    {
        resultOverlay = overlay;
        resultPro = overlay != null ? overlay.GetComponentInChildren<ResultPro>(true) : null;
    }

    private void Awake()
    {
        if (noteManager == null)
            noteManager = FindFirstObjectByType<NoteManager>();
        if (chartSpawner == null)
            chartSpawner = FindFirstObjectByType<ChartNoteSpawner>();
        if (comboManager == null)
            comboManager = FindFirstObjectByType<ComboManager>();
        if (resultPro == null && resultOverlay != null)
            resultPro = resultOverlay.GetComponentInChildren<ResultPro>(true);

        if (resultOverlay != null)
            resultOverlay.SetActive(false);

        WireResultButtons();
    }

    private void OnEnable()
    {
        if (noteManager != null)
            noteManager.OnNoteFinishedEvent += HandleNoteFinished;
        if (chartSpawner != null)
            chartSpawner.OnChartFinished += HandleChartFinished;
    }

    private void OnDisable()
    {
        if (noteManager != null)
            noteManager.OnNoteFinishedEvent -= HandleNoteFinished;
        if (chartSpawner != null)
            chartSpawner.OnChartFinished -= HandleChartFinished;
    }

    private void HandleNoteFinished(NoteBase note, NoteResult result)
    {
        HitJudgment judgment = result == NoteResult.Completed && note != null
            ? note.LastJudgment
            : HitJudgment.Miss;

        switch (judgment)
        {
            case HitJudgment.Perfect: _perfect++; break;
            case HitJudgment.Great: _great++; break;
            case HitJudgment.Good: _good++; break;
            default: _miss++; break;
        }
    }

    private void HandleChartFinished()
    {
        if (_resultRequested)
            return;

        _resultRequested = true;
        StartCoroutine(ShowResultAfterDelay());
    }

    private IEnumerator ShowResultAfterDelay()
    {
        yield return new WaitForSeconds(showDelaySeconds);

        if (resultOverlay == null || resultPro == null)
        {
            Debug.LogWarning("GameplayResultController: Result overlay is not assigned.");
            yield break;
        }

        resultOverlay.SetActive(true);
        WireResultButtons();
        ComboManager resolvedComboManager = ResolveComboManager();
        int maxCombo = resolvedComboManager != null ? resolvedComboManager.MaxCombo : _perfect + _great + _good;
        GameplayHudController hud = GameplayHudController.Instance;
        float health = hud != null ? hud.Health : 0f;
        bool passed = hud != null ? hud.IsPassed : _miss == 0;
        GameplayResultData result = new GameplayResultData(_perfect, _great, _good, _miss, maxCombo, health, passed);
        ResultSongBackdrop.Apply(resultOverlay, result);
        SongData selectedSong = SelectedSongManager.Instance != null ? SelectedSongManager.Instance.SelectedSong : null;
        SongPlayStats.Save(selectedSong, result);
        resultPro.Show(result);
    }

    public void RetryGame()
    {
        Time.timeScale = 1f;

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();

        SceneLoadUtility.ReloadActiveScene();
    }

    public void BackToSongSelect()
    {
        Time.timeScale = 1f;

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();

        SceneLoadUtility.LoadSceneByName(songSelectSceneName);
    }

    private void WireResultButtons()
    {
        if (!wireResultButtons || resultOverlay == null)
            return;

        Button retryButton = FindButton("Retry");
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(RetryGame);
            retryButton.onClick.AddListener(RetryGame);
        }

        Button backButton = FindButton("Back");
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(BackToSongSelect);
            backButton.onClick.AddListener(BackToSongSelect);
        }
    }

    private ComboManager ResolveComboManager()
    {
        GameObject runtimeComboObject = GameObject.Find("RG Runtime Combo Manager");
        ComboManager runtimeCombo = runtimeComboObject != null
            ? runtimeComboObject.GetComponent<ComboManager>()
            : null;

        if (runtimeCombo != null)
        {
            comboManager = runtimeCombo;
            return comboManager;
        }

        if (comboManager == null)
            comboManager = FindFirstObjectByType<ComboManager>();

        return comboManager;
    }

    private Button FindButton(string buttonName)
    {
        Button[] buttons = resultOverlay.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name == buttonName)
                return buttons[i];
        }

        return null;
    }
}
