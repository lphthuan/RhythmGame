using UnityEngine;

public class ChartVisualizer : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private Transform noteParent;

    [Header("Layout")]
    [SerializeField] private float laneSpacing = 1.2f;
    [SerializeField] private float timeSpacing = 1f;

    public void Draw(ChartData chart)
    {
        Clear();

        for (int i = 0; i < chart.notes.Count; i++)
        {
            NoteData note = chart.notes[i];

            Vector3 position = new Vector3(
                note.lane * laneSpacing,
                note.time * timeSpacing,
                0f
            );

            Instantiate(notePrefab, position, Quaternion.identity, noteParent);
        }
    }

    public void Clear()
    {
        if (noteParent == null) return;

        for (int i = noteParent.childCount - 1; i >= 0; i--)
        {
            Destroy(noteParent.GetChild(i).gameObject);
        }
    }
}