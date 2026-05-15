using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SongItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private Image _previewImage;

    private SongData _data;

    public void Setup(SongData data)
    {
        _data = data;
        _titleText.text = data.SongTitle;
        if (_previewImage != null) _previewImage.sprite = data.PreviewImage;
    }

    public void OnSelect()
    {
        
        SelectedSongManager.Instance.SetSelectedSong(_data);
        Debug.Log($"Selected: {_data.SongTitle}");
    }
}