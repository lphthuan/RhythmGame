using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Thêm cái này để xài Image
using TMPro; // Thêm cái này để xài TextMP

public class SongListManager : MonoBehaviour
{
    // Tạo một Instance nhanh để gọi từ bất cứ đâu
    public static SongListManager Instance;

    public List<SongData> _songList;
    public SongItemUI _itemPrefab;
    public Transform _contentArea;

    // Kéo thả Image cột 2 và Text cột 3 vào đây ngoài Inspector
    public Image _centerPreviewImage;
    public TextMeshProUGUI _rightBpmText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        PopulateList();
    }

    public void PopulateList()
    {
        foreach (var song in _songList)
        {
            var item = Instantiate(_itemPrefab, _contentArea);
            item.Setup(song);
        }
    }
}