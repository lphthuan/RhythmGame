using UnityEngine;

[CreateAssetMenu(fileName = "NewSong", menuName = "Data/SongData")]
public class SongData : ScriptableObject
{
    [SerializeField] private string _songTitle;
    [SerializeField] private string _sceneName;
    [SerializeField] private Sprite _previewImage;
    [SerializeField] private float _bpm;

    public string SongTitle => _songTitle;
    public string SceneName => _sceneName;
    public Sprite PreviewImage => _previewImage;
}