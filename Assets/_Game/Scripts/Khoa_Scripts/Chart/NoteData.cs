using System;

public enum ChartNoteType
{
    Tap,
    Hold,
    Flick,
    Slide
}

public enum ChartFlickDirection
{
    Any,
    Up,
    Down,
    Left,
    Right
}

[Serializable]
public class NoteData
{
    public float time;
    public int lane;

    public ChartNoteType type = ChartNoteType.Tap;

    // Dung cho Hold / Slide
    public float duration;

    // Dung cho Flick
    public ChartFlickDirection flickDirection = ChartFlickDirection.Any;

    // Dung cho Slide: danh sach lane checkpoint
    public int[] slidePath;
}