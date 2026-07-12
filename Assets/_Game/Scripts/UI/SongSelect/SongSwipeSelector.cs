using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Lets the SongSelect ScrollRect commit the card closest to the middle after a vertical swipe.</summary>
public class SongSwipeSelector : MonoBehaviour, IEndDragHandler
{
    [SerializeField] private SongListManager songListManager;

    public void Configure(SongListManager manager)
    {
        songListManager = manager;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (songListManager != null)
            songListManager.SelectClosestScrolledSong();
    }
}
