using UnityEngine;

// TEST SCENE ONLY
// Chỉ dùng để log kết quả note trong scene test.
// Không đem qua scene chính.
public class TestNoteResultLogger : MonoBehaviour, INoteResultReceiver
{
    public void OnNoteFinished(NoteBase note, NoteResult result)
    {
        Debug.Log($"[TEST RESULT] {note.name} / {note.NoteType} / Result: {result}");

        Destroy(note.gameObject, 0.25f);
    }
}