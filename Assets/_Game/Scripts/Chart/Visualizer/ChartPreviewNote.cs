using UnityEngine;

public class ChartPreviewNote : MonoBehaviour
{
    public int NoteIndex { get; private set; }
    public NoteData NoteData { get; private set; }

    private ChartVisualizer visualizer;

    public void Initialize(int noteIndex, NoteData noteData, ChartVisualizer owner)
    {
        NoteIndex = noteIndex;
        NoteData = noteData;
        visualizer = owner;
    }

#if UNITY_EDITOR
    private void OnMouseDrag()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        transform.position = mouseWorld;

        visualizer.UpdateNoteFromPreview(this);
    }
#endif
}
