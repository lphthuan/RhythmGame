using UnityEngine;

public class NoteResultDispatcher : MonoBehaviour, INoteResultReceiver
{
    [Header("Receivers")]
    [SerializeField] private MonoBehaviour[] receiverBehaviours;

    private INoteResultReceiver[] receivers;

    private void Awake()
    {
        receivers = new INoteResultReceiver[receiverBehaviours.Length];

        for (int i = 0; i < receiverBehaviours.Length; i++)
        {
            receivers[i] = receiverBehaviours[i] as INoteResultReceiver;

            if (receiverBehaviours[i] != null && receivers[i] == null)
            {
                Debug.LogWarning($"{receiverBehaviours[i].name} does not implement INoteResultReceiver.");
            }
        }
    }

    public void OnNoteFinished(NoteBase note, NoteResult result)
    {
        for (int i = 0; i < receivers.Length; i++)
        {
            if (receivers[i] == null)
                continue;

            receivers[i].OnNoteFinished(note, result);
        }
    }
}