using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SongItemUI : MonoBehaviour
{
    public TextMeshProUGUI _titleText;
    public Image _previewImage;

    public SongData _data;

    public void Setup(SongData data)
    {
        _data = data;
        _titleText.text = data.SongTitle;
        if (_previewImage != null) _previewImage.sprite = data.PreviewImage;
    }

    public void OnSelect()
    {
        // Lưu data bài hát được chọn ngoài GameManager
        SelectedSongManager.Instance.SetSelectedSong(_data);

        // Đổi đúng cái ảnh preview ở giữa là đủ ăn tiền rồi
        if (SongListManager.Instance._centerPreviewImage != null)
        {
            SongListManager.Instance._centerPreviewImage.sprite = _data.PreviewImage;
        }

        // ĐÃ XÓA ĐOẠN ĐỔI CHỮ DIFFICULTY Ở ĐÂY CHO ĐỠ LOẠN! 🥳

        Debug.Log($"Selected: {_data.SongTitle}");
    }
}