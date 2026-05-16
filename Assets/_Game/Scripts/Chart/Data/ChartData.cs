using System;
using System.Collections.Generic;

[Serializable]
public class ChartData
{
    public string songName;
    public float bpm;
    public float offset;
    public int laneCount;
    public List<NoteData> notes = new();
}