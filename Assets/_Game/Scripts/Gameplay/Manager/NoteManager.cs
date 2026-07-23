using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using Keyboard = UnityEngine.InputSystem.Keyboard;
#endif

public class NoteManager : MonoBehaviour
{
    [Header("Clock")]
    [SerializeField] private bool useInternalClock = true;
    [SerializeField] private float currentTime;

    [Header("Miss")]
    [SerializeField] private bool autoMiss = true;

    [Tooltip("Sau khi hết Good Window, chờ thêm bao nhiêu giây rồi mới tính MISS.")]
    [SerializeField] private float autoMissExtraDelay = 0.16f;

    [Header("Receiver Optional")]
    [SerializeField] private MonoBehaviour resultReceiverBehaviour;

    [Header("Judgment")]
    [SerializeField] private JudgmentWindow judgmentWindow = new JudgmentWindow();

    [Header("Anti-Spam")]
    [Tooltip("Nếu bấm cùng lane trước Good window nhưng vẫn còn trong Miss Window, note sẽ bị MISS để chống spam.")]
    [SerializeField] private bool punishTooEarlyInput = true;

    [Header("Hitline Position TEST")]
    [SerializeField] private bool requireNearHitline = true;
    [SerializeField] private float hitlineY = -330f;
    [SerializeField] private float hitlineJudgeDistance = 150f;
    [SerializeField] private bool useLaneLayoutHitbox = true;
    [SerializeField, Range(0.25f, 1.25f)] private float laneHitboxWidthRatio = 1.08f;
    [SerializeField] private bool recoverUnboundTouchDuringHold = true;

    [Header("PC Test Input")]
    [Tooltip("Cho phép giả lập 4 lane bằng bàn phím trong Play Mode.")]
    [SerializeField] private bool enableKeyboardLaneInput = true;
    [SerializeField] private bool ignoreEditorMouseWhenTouchActive = true;
    [SerializeField] private GameplayLaneLayout laneLayout;
    [SerializeField] private KeyCode lane0Key = KeyCode.A;
    [SerializeField] private KeyCode lane1Key = KeyCode.S;
    [SerializeField] private KeyCode lane2Key = KeyCode.Semicolon;
    [SerializeField] private KeyCode lane3Key = KeyCode.Quote;

    private INoteResultReceiver resultReceiver;

    private readonly List<NoteBase> activeNotes = new List<NoteBase>();
    private readonly Dictionary<int, NoteBase> fingerToNote = new Dictionary<int, NoteBase>();
    // Tracks physical fingers for their whole screen-contact lifetime. This is
    // intentionally separate from fingerToNote: a finger can remain on screen
    // after its Hold or Tap has finished.
    private readonly HashSet<int> activeTouchFingerIds = new HashSet<int>();
    // Fingers that first touched the screen while paused must be lifted before
    // they can judge a note. This prevents a paused tap becoming a free hit.
    private readonly HashSet<int> blockedTouchFingerIds = new HashSet<int>();

    private Vector2 lastMousePosition;
    private const int KeyboardFingerBaseId = -2000;

    public event Action<NoteBase, NoteResult> OnNoteFinishedEvent;
    public event Action<NoteBase, HitJudgment, float> OnNoteJudgedEvent;
    public event Action<NoteBase> OnNoteSustainEvent;

    public float CurrentTime => currentTime;

    public void ApplyHitlineLayout(float newHitlineY, float newHitlineJudgeDistance = -1f)
    {
        hitlineY = newHitlineY;

        if (newHitlineJudgeDistance > 0f)
            hitlineJudgeDistance = newHitlineJudgeDistance;
    }

    private void Awake()
    {
        judgmentWindow?.Normalize();
        ResolveResultReceiver();

        if (laneLayout == null)
            laneLayout = FindFirstObjectByType<GameplayLaneLayout>();
    }

    private void OnValidate()
    {
        judgmentWindow?.Normalize();
        autoMissExtraDelay = Mathf.Max(0f, autoMissExtraDelay);
    }

    private void Update()
    {
        if (useInternalClock)
        {
            currentTime += Time.deltaTime;
        }

        TickNotes();

        if (GameplayInputGate.IsBlocked)
        {
            RememberTouchesStartedWhileBlocked();
            return;
        }

        // Xử lý input trước AutoMiss để tránh trường hợp:
        // note vừa qua window → bị miss trước → người chơi bấm cùng frame nhưng không ăn.
        HandleTouchInput();

#if UNITY_EDITOR
        HandleMouseInputForEditor();
#endif

        HandleKeyboardLaneInput();

        CheckAutoMiss();
    }

    private void ResolveResultReceiver()
    {
        if (resultReceiverBehaviour == null)
            return;

        resultReceiver = resultReceiverBehaviour as INoteResultReceiver;

        if (resultReceiver == null)
        {
            Debug.LogWarning($"{resultReceiverBehaviour.name} does not implement INoteResultReceiver.");
        }
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
        RemoveFingerBindingOfNote(note);
    }

    public void NotifyNoteFinished(NoteBase note, NoteResult result)
    {
        if (note == null)
            return;

        UnregisterNote(note);

        resultReceiver?.OnNoteFinished(note, result);
        OnNoteFinishedEvent?.Invoke(note, result);

        HitEffectSpriteReceiver receiver = HitEffectSpriteReceiver.ActiveReceiver;
        if (receiver != null && !receiver.IsSubscribed &&
            (result == NoteResult.Missed ||
             result == NoteResult.Failed ||
             result == NoteResult.ReleasedEarly))
        {
            receiver.ShowMissEffect(note);
        }
    }
    public void NotifyJudgmentEffect(NoteBase note)
    {
        if (note == null)
            return;

        if (note.LastJudgment == HitJudgment.None ||
            note.LastJudgment == HitJudgment.Miss)
            return;

        OnNoteJudgedEvent?.Invoke(
            note,
            note.LastJudgment,
            note.LastDeltaMs
        );

        HitEffectSpriteReceiver receiver = HitEffectSpriteReceiver.ActiveReceiver;
        if (receiver != null && !receiver.IsSubscribed)
            receiver.ShowJudgmentEffect(note, note.LastJudgment);
    }

    public void NotifySustainEffect(NoteBase note)
    {
        if (note == null)
            return;

        OnNoteSustainEvent?.Invoke(note);
    }

    private void TickNotes()
    {
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            NoteBase note = activeNotes[i];

            if (note == null)
            {
                activeNotes.RemoveAt(i);
                continue;
            }

            note.Tick(currentTime);
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

            // Hold / Slide đang được giữ thì để chính note đó tự xử lý complete/fail.
            if (note.IsAssigned)
                continue;

            float missTime = note.HitTime + judgmentWindow.missWindow + autoMissExtraDelay;

            if (currentTime > missTime)
            {
                note.SetJudgment(HitJudgment.Miss, 0f);
                note.ForceMiss();
            }
        }
    }

    private void HandleTouchInput()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (blockedTouchFingerIds.Contains(touch.fingerId))
            {
                if (touch.phase == UnityEngine.TouchPhase.Ended || touch.phase == UnityEngine.TouchPhase.Canceled)
                    blockedTouchFingerIds.Remove(touch.fingerId);
                continue;
            }

            NotePointer pointer = new NotePointer(
                touch.fingerId,
                touch.position,
                touch.deltaPosition,
                Time.unscaledTime
            );

            // A missed Began can be recovered once, when the finger is first
            // observed. Never recover an already-observed unbound finger:
            // after completing a note, that would incorrectly consume later
            // notes while the player is still holding the screen.
            bool firstObservation = activeTouchFingerIds.Add(touch.fingerId);

            switch (touch.phase)
            {
                case UnityEngine.TouchPhase.Began:
                    if (firstObservation)
                        PointerBegin(pointer);
                    break;

                case UnityEngine.TouchPhase.Moved:
                    if (firstObservation)
                        TryRecoverMissedTouchBegin(pointer);
                    PointerMove(pointer);
                    break;

                case UnityEngine.TouchPhase.Stationary:
                    if (firstObservation)
                        TryRecoverMissedTouchBegin(pointer);
                    PointerStationary(pointer);
                    break;

                case UnityEngine.TouchPhase.Ended:
                case UnityEngine.TouchPhase.Canceled:
                    PointerEnd(pointer);
                    activeTouchFingerIds.Remove(touch.fingerId);
                    break;
            }
        }
    }

    private void RememberTouchesStartedWhileBlocked()
    {
        for (int i = 0; i < Input.touchCount; i++)
            blockedTouchFingerIds.Add(Input.GetTouch(i).fingerId);
    }

    /// <summary>
    /// Android can occasionally report a newly-added finger first as Moved or
    /// Stationary while other fingers are already holding the screen. Recover
    /// that unbound finger without applying an early-input penalty, because the
    /// original Began event was not delivered.
    /// </summary>
    private void TryRecoverMissedTouchBegin(NotePointer pointer)
    {
        if (!recoverUnboundTouchDuringHold || fingerToNote.ContainsKey(pointer.fingerId))
            return;

        TryPointerBegin(pointer, false);
    }

#if UNITY_EDITOR
    private void HandleMouseInputForEditor()
    {
        if (ignoreEditorMouseWhenTouchActive && Input.touchCount > 0)
            return;

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
        {
            PointerBegin(pointer);
        }

        if (Input.GetMouseButton(0))
        {
            PointerMove(pointer);
        }

        if (Input.GetMouseButtonUp(0))
        {
            PointerEnd(pointer);
        }

        lastMousePosition = mousePosition;
    }
#endif

    private void HandleKeyboardLaneInput()
    {
        if (!enableKeyboardLaneInput)
            return;

        for (int laneIndex = 0; laneIndex < 4; laneIndex++)
        {
            KeyCode key = GetKeyboardKeyForLane(laneIndex);

            if (key == KeyCode.None)
                continue;

            int fingerId = KeyboardFingerBaseId - laneIndex;
            Vector2 position = GetKeyboardLanePosition(laneIndex);

            NotePointer pointer = new NotePointer(
                fingerId,
                position,
                Vector2.zero,
                Time.unscaledTime
            );

            if (WasKeyPressedThisFrame(key))
                PointerBegin(pointer);

            if (IsKeyPressed(key))
                PointerStationary(pointer);

            if (WasKeyReleasedThisFrame(key))
                PointerEnd(pointer);
        }
    }

    private KeyCode GetKeyboardKeyForLane(int laneIndex)
    {
        return laneIndex switch
        {
            0 => lane0Key,
            1 => lane1Key,
            2 => lane2Key,
            3 => lane3Key,
            _ => KeyCode.None
        };
    }

    private Vector2 GetKeyboardLanePosition(int laneIndex)
    {
        if (laneLayout != null)
            return laneLayout.GetLaneHitScreenPosition(laneIndex);

        float x = Screen.width * ((laneIndex + 1f) / 5f);
        float y = Screen.height * 0.2f;
        return new Vector2(x, y);
    }

    private static bool WasKeyPressedThisFrame(KeyCode key)
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(key))
            return true;
#endif

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        return key switch
        {
            KeyCode.A => keyboard.aKey.wasPressedThisFrame,
            KeyCode.S => keyboard.sKey.wasPressedThisFrame,
            KeyCode.Semicolon => keyboard.semicolonKey.wasPressedThisFrame,
            KeyCode.Quote => keyboard.quoteKey.wasPressedThisFrame,
            _ => false
        };
#else
        return false;
#endif
    }

    private static bool IsKeyPressed(KeyCode key)
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(key))
            return true;
#endif

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        return key switch
        {
            KeyCode.A => keyboard.aKey.isPressed,
            KeyCode.S => keyboard.sKey.isPressed,
            KeyCode.Semicolon => keyboard.semicolonKey.isPressed,
            KeyCode.Quote => keyboard.quoteKey.isPressed,
            _ => false
        };
#else
        return false;
#endif
    }

    private static bool WasKeyReleasedThisFrame(KeyCode key)
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyUp(key))
            return true;
#endif

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return false;

        return key switch
        {
            KeyCode.A => keyboard.aKey.wasReleasedThisFrame,
            KeyCode.S => keyboard.sKey.wasReleasedThisFrame,
            KeyCode.Semicolon => keyboard.semicolonKey.wasReleasedThisFrame,
            KeyCode.Quote => keyboard.quoteKey.wasReleasedThisFrame,
            _ => false
        };
#else
        return false;
#endif
    }

    private void PointerBegin(NotePointer pointer)
    {
        TryPointerBegin(pointer, true);
    }

    private bool TryPointerBegin(NotePointer pointer, bool punishTooEarly)
    {
        NoteBase note = FindBestNote(pointer.position);

        if (note == null)
        {
            if (punishTooEarly)
                TryPunishTooEarlyInput(pointer.position);

            return false;
        }

        if (!note.CanReceivePointer())
            return false;

        HitJudgment judgment = judgmentWindow.Judge(
            currentTime,
            note.HitTime,
            out float deltaMs
        );

        if (judgment == HitJudgment.Miss)
        {
            note.ForceMiss(deltaMs);
            return false;
        }

        note.SetJudgment(judgment, deltaMs);

        note.AssignFinger(pointer.fingerId);
        fingerToNote[pointer.fingerId] = note;

        note.OnPointerBegin(pointer);

        if (note.IsFinished)
        {
            fingerToNote.Remove(pointer.fingerId);
        }

        return true;
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
        {
            fingerToNote.Remove(pointer.fingerId);
        }
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
        {
            fingerToNote.Remove(pointer.fingerId);
        }
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
        float bestScore = float.MaxValue;
        bool hasLayoutLane = TryGetLayoutLane(screenPosition, out int pointerLane, out float laneDistance);

        foreach (NoteBase note in activeNotes)
        {
            if (note == null)
                continue;

            if (!note.CanReceivePointer())
                continue;

            // Không cho bấm quá sớm hoặc quá trễ.
            if (!judgmentWindow.IsInsideHitWindow(currentTime, note.HitTime))
                continue;

            // Test thêm vị trí note có gần hitline không.
            if (requireNearHitline)
            {
                float distanceToHitline = Mathf.Abs(note.AnchoredPosition.y - hitlineY);

                if (distanceToHitline > hitlineJudgeDistance)
                    continue;
            }

            float distanceToTouch;

            if (hasLayoutLane)
            {
                if (note.LaneIndex != pointerLane)
                    continue;

                distanceToTouch = laneDistance;
            }
            else
            {
                distanceToTouch = note.DistanceToPointer(screenPosition);

                if (distanceToTouch > note.TouchRadius)
                    continue;
            }

            // Ưu tiên note gần hitTime hơn.
            // Nếu timing gần nhau thì ưu tiên note gần vị trí chạm hơn.
            float timeDelta = Mathf.Abs(currentTime - note.HitTime);
            float score = timeDelta * 1000f + distanceToTouch;

            if (score < bestScore)
            {
                bestScore = score;
                bestNote = note;
            }
        }

        return bestNote;
    }

    private bool TryPunishTooEarlyInput(Vector2 screenPosition)
    {
        if (!punishTooEarlyInput)
            return false;

        bool hasLayoutLane = TryGetLayoutLane(screenPosition, out int pointerLane, out float laneDistance);
        NoteBase candidate = null;
        float bestScore = float.MaxValue;

        foreach (NoteBase note in activeNotes)
        {
            if (note == null || !note.CanReceivePointer())
                continue;

            if (!judgmentWindow.IsTooEarlyButInsideMissWindow(currentTime, note.HitTime))
                continue;

            float distanceScore;
            if (hasLayoutLane)
            {
                if (note.LaneIndex != pointerLane)
                    continue;

                distanceScore = laneDistance;
            }
            else
            {
                distanceScore = note.DistanceToPointer(screenPosition);
                if (distanceScore > note.TouchRadius)
                    continue;
            }

            float score = (note.HitTime - currentTime) * 1000f + distanceScore;
            if (score < bestScore)
            {
                bestScore = score;
                candidate = note;
            }
        }

        if (candidate == null)
            return false;

        float deltaMs = (currentTime - candidate.HitTime) * 1000f;
        candidate.ForceMiss(deltaMs);
        return true;
    }

    private bool TryGetLayoutLane(Vector2 screenPosition, out int laneIndex, out float distanceToLaneCenter)
    {
        laneIndex = -1;
        distanceToLaneCenter = float.MaxValue;

        if (!useLaneLayoutHitbox)
            return false;

        return laneLayout != null &&
            laneLayout.TryGetLaneIndexFromScreenPosition(
                screenPosition,
                laneHitboxWidthRatio,
                out laneIndex,
                out distanceToLaneCenter);
    }

    private void RemoveFingerBindingOfNote(NoteBase note)
    {
        int fingerToRemove = int.MinValue;

        foreach (KeyValuePair<int, NoteBase> pair in fingerToNote)
        {
            if (pair.Value == note)
            {
                fingerToRemove = pair.Key;
                break;
            }
        }

        if (fingerToRemove != int.MinValue)
        {
            fingerToNote.Remove(fingerToRemove);
        }
    }
}
