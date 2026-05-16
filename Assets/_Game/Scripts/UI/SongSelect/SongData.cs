using UnityEngine;

[CreateAssetMenu(fileName = "NewSong", menuName = "Data/SongData")]
public class SongData : ScriptableObject
{
    public string _songTitle;
    public string _sceneName;
    public Sprite _previewImage;
    public float _bpm;

    // Thêm dòng này để tha hồ gõ chữ Easy/Normal/Hard ngoài Inspector
    public string _difficulty = "Normal";

    public string SongTitle => _songTitle;
    public string SceneName => _sceneName;
    public Sprite PreviewImage => _previewImage;
}