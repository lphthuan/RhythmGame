using UnityEngine;

public class SelectedSongManager : MonoBehaviour
{
    public static SelectedSongManager Instance { get; private set; }
    public const string LastSelectedSongGroupKey = "RhythmGame.LastSelectedSongGroup";
    public const string LastSelectedDifficultyKey = "RhythmGame.LastSelectedDifficulty";

    public SongData _selectedSong;
    public SongData SelectedSong => _selectedSong;
    public Difficulty SelectedDifficulty { get; private set; } = Difficulty.Medium;

    private void Awake()
    {
        // Logic Singleton chuẩn bài
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null)
                transform.SetParent(null);

            DontDestroyOnLoad(gameObject); // Giữ Object này không bị xóa khi đổi Scene
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetSelectedSong(SongData song)
    {
        _selectedSong = song;
        if (song != null)
            SelectedDifficulty = song.difficultyLevel;

        SaveSelection();
    }

    public void SetSelectedSong(SongData song, Difficulty difficulty)
    {
        _selectedSong = song;
        SelectedDifficulty = difficulty;
        SaveSelection();
    }

    private void SaveSelection()
    {
        if (_selectedSong == null)
            return;

        string groupId = !string.IsNullOrWhiteSpace(_selectedSong.songGroupId)
            ? _selectedSong.songGroupId
            : _selectedSong.name;

        PlayerPrefs.SetString(LastSelectedSongGroupKey, SongData.SanitizeForFileName(groupId));
        PlayerPrefs.SetInt(LastSelectedDifficultyKey, (int)SelectedDifficulty);
        PlayerPrefs.Save();
    }
}
