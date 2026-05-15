using System.Collections.Generic;
using UnityEngine;

public class SongListManager : MonoBehaviour
{
    [SerializeField] private List<SongData> _songList;
    [SerializeField] private SongItemUI _itemPrefab;
    [SerializeField] private Transform _contentArea;

    private void Start()
    {
        PopulateList();
    }

    private void PopulateList()
    {
        foreach (var song in _songList)
        {
            var item = Instantiate(_itemPrefab, _contentArea);
            item.Setup(song);
        }
    }
}