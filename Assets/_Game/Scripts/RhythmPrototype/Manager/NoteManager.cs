using System;
using System.Collections.Generic;
using UnityEngine;

public class NoteManager : MonoBehaviour
{
    [Header("Clock")]
    [SerializeField] private bool useInternalClock = true;
    [SerializeField] private float currentTime;

    [Header("Miss")]
    [SerializeField] private bool autoMiss = true;
    [SerializeField] private float missAfterHitTime = 0.45f;

    [Header("Receiver Optional")]
    [SerializeField] private MonoBehaviour resultReceiverBehaviour;

    [Header("Hitline Test")]
    [SerializeField] private bool requireNearHitline = true;
    [SerializeField] private float hitlineY = -330f;
    [SerializeField] private float hitlineJudgeDistance = 120f;

    private INoteResultReceiver resultReceiver;

    private readonly List<NoteBase> activeNotes = new List<NoteBase>();
    private readonly Dictionary<int, NoteBase> fingerToNote = new Dictionary<int, NoteBase>();

    private Vector2 lastMousePosition;

    public event Action<NoteBase, NoteResult> OnNoteFinishedEvent;

    public float CurrentTime => currentTime;

    private void Awake()
    {
        if (resultReceiverBehaviour != null)
        {
            resultReceiver = resultReceiverBehaviour as INoteResultReceiver;

            if (resultReceiver == null)
                Debug.LogWarning($"{resultReceiverBehaviour.name} does not implement INoteResultReceiver.");
        }
    }

    private void Update()
    {
        if (useInternalClock)
            currentTime += Time.deltaTime;

        TickNotes();
        CheckAutoMiss();
        HandleTouchInput();

#if UNITY_EDITOR
        HandleMouseInputForEditor();
#endif
    }

    public void SetExternalTime(float time)
    {
        useInternalClock = false;
        currentTime = time;
    }

    public void RegisterNote(NoteBase note)
    {
        if (note == null)
            return;

        if (activeNotes.Contains(note))
            return;

        activeNotes.Add(note);
        note.SetOwner(this);
    }

    public void UnregisterNote(NoteBase note)
    {
        if (note == null)
            return;

        activeNotes.Remove(note);
    }

    public void NotifyNoteFinished(NoteBase note, NoteResult result)
    {
        UnregisterNote(note);

        resultReceiver?.OnNoteFinished(note, result);
        OnNoteFinishedEvent?.Invoke(note, result);
    }

    private void TickNotes()
    {
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            if (activeNotes[i] == null)
            {
                activeNotes.RemoveAt(i);
                continue;
            }

            activeNotes[i].Tick(currentTime);
        }
    }

    private void CheckAutoMiss()
    {
        if (!autoMiss)
            return;

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            NoteBase note = activeNotes[i];

            if (note == null)
                continue;

            // Nếu note đang được giữ / đang được finger xử lý
            // thì không được auto miss.
            // Đặc biệt quan trọng với Hold và Slide.
            if (note.IsAssigned)
                continue;

            float missTime = note.GetAutoMissTime(missAfterHitTime);

            if (currentTime > missTime)
            {
                note.ForceMiss();
            }
        }
    }

    private void HandleTouchInput()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            NotePointer pointer = new NotePointer(
                touch.fingerId,
                touch.position,
                touch.deltaPosition,
                Time.unscaledTime
            );

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    PointerBegin(pointer);
                    break;

                case TouchPhase.Moved:
                    PointerMove(pointer);
                    break;

                case TouchPhase.Stationary:
                    PointerStationary(pointer);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    PointerEnd(pointer);
                    break;
            }
        }
    }

#if UNITY_EDITOR
    private void HandleMouseInputForEditor()
    {
        int mouseFingerId = -999;
        Vector2 mousePosition = Input.mousePosition;
        Vector2 delta = mousePosition - lastMousePosition;

        NotePointer pointer = new NotePointer(
            mouseFingerId,
            mousePosition,
            delta,
            Time.unscaledTime
        );

        if (Input.GetMouseButtonDown(0))
            PointerBegin(pointer);

        if (Input.GetMouseButton(0))
            PointerMove(pointer);

        if (Input.GetMouseButtonUp(0))
            PointerEnd(pointer);

        lastMousePosition = mousePosition;
    }
#endif

    private void PointerBegin(NotePointer pointer)
    {
        NoteBase note = FindBestNote(pointer.position);

        if (note == null)
            return;

        if (!note.CanReceivePointer())
            return;

        note.AssignFinger(pointer.fingerId);
        fingerToNote[pointer.fingerId] = note;

        note.OnPointerBegin(pointer);

        if (note.IsFinished)
            fingerToNote.Remove(pointer.fingerId);
    }

    private void PointerMove(NotePointer pointer)
    {
        if (!fingerToNote.TryGetValue(pointer.fingerId, out NoteBase note))
            return;

        if (note == null)
        {
            fingerToNote.Remove(pointer.fingerId);
            return;
        }

        if (!note.IsAssignedToFinger(pointer.fingerId))
            return;

        note.OnPointerMove(pointer);

        if (note.IsFinished)
            fingerToNote.Remove(pointer.fingerId);
    }

    private void PointerStationary(NotePointer pointer)
    {
        if (!fingerToNote.TryGetValue(pointer.fingerId, out NoteBase note))
            return;

        if (note == null)
        {
            fingerToNote.Remove(pointer.fingerId);
            return;
        }

        if (!note.IsAssignedToFinger(pointer.fingerId))
            return;

        note.OnPointerStationary(pointer);

        if (note.IsFinished)
            fingerToNote.Remove(pointer.fingerId);
    }

    private void PointerEnd(NotePointer pointer)
    {
        if (!fingerToNote.TryGetValue(pointer.fingerId, out NoteBase note))
            return;

        if (note != null && note.IsAssignedToFinger(pointer.fingerId))
        {
            note.OnPointerEnd(pointer);
            note.ReleaseFinger();
        }

        fingerToNote.Remove(pointer.fingerId);
    }

    private NoteBase FindBestNote(Vector2 screenPosition)
    {
        NoteBase bestNote = null;
        float bestDistance = float.MaxValue;

        foreach (NoteBase note in activeNotes)
        {
            if (note == null)
                continue;

            if (!note.CanReceivePointer())
                continue;

            if (requireNearHitline)
            {
                float distanceToHitline = Mathf.Abs(note.AnchoredPosition.y - hitlineY);

                if (distanceToHitline > hitlineJudgeDistance)
                    continue;
            }

            float distance = note.DistanceToPointer(screenPosition);

            if (distance <= note.TouchRadius && distance < bestDistance)
            {
                bestDistance = distance;
                bestNote = note;
            }
        }

        return bestNote;
    }
}