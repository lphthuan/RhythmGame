using UnityEngine;

// TEST SCENE ONLY
// Chỉ dùng để log kết quả note trong scene test.
// Không đem qua scene chính.
public class TestNoteResultLogger : MonoBehaviour, INoteResultReceiver
{
    [Header("TEST ONLY")]
    [SerializeField] private bool destroyNoteAfterResult = true;
    [SerializeField] private float destroyDelay = 0.75f;

    public void OnNoteFinished(NoteBase note, NoteResult result)
    {
        if (note == null)
            return;

        Debug.Log(
            $"[TEST RESULT] {note.name} / {note.NoteType} / Result: {result} / Judgment: {note.LastJudgment} / Delta: {note.LastDeltaMs:0.0}ms"
        );

        if (destroyNoteAfterResult)
        {
            Destroy(note.gameObject, destroyDelay);
        }
    }
}