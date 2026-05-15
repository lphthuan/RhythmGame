using UnityEngine;

public class SelectedSongManager : MonoBehaviour
{
    public static SelectedSongManager Instance { get; private set; }

    [SerializeField] private SongData _selectedSong;
    public SongData SelectedSong => _selectedSong;

    private void Awake()
    {
        // Logic Singleton chuẩn bài
        if (Instance == null)
        {
            Instance = this;
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
    }
}