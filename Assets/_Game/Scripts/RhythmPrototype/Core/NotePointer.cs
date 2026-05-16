using UnityEngine;

public struct NotePointer
{
    public int fingerId;
    public Vector2 position;
    public Vector2 deltaPosition;
    public float time;

    public NotePointer(int fingerId, Vector2 position, Vector2 deltaPosition, float time)
    {
        this.fingerId = fingerId;
        this.position = position;
        this.deltaPosition = deltaPosition;
        this.time = time;
    }
}