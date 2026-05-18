using UnityEngine;

/// <summary>
/// Tạo chart note dựa trên BPM và cấu hình độ khó.
///
/// Cải tiến so với phiên bản trước:
/// - Note được đặt có TRỌNG SỐ theo vị trí trong nhịp (4/4).
/// - Beat 1 và beat 3 (downbeat) → ưu tiên cao nhất → luôn có note.
/// - Beat 2 và beat 4 (weak beat) → ít hơn.
/// - Off-beat (giữa các beat) → rất hiếm, chỉ xuất hiện ở Hard.
///
/// Kết quả: note cảm giác "đi theo nhịp nhạc" thay vì random bừa bãi.
/// </summary>
public static class SimpleChartGenerator
{
    // Giả định nhịp 4/4 (4 beat per measure).
    private const int BeatsPerMeasure = 4;

    // Trọng số cho từng vị trí beat trong measure (4/4):
    // Beat 1 (index 0): downbeat → mạnh nhất
    // Beat 3 (index 2): mid-bar  → mạnh thứ hai
    // Beat 2, 4 (index 1, 3): weak beats → nhẹ
    private static readonly float[] BeatWeights = { 3.0f, 0.6f, 2.0f, 0.6f };

    // Trọng số cho off-beat (subdivision giữa các beat) → rất nhỏ
    private const float OffBeatWeight = 0.25f;

    public static ChartData Generate(
        string songName,
        float bpm,
        float songLength,
        float offset,
        int laneCount,
        int subdivision,
        float noteChance)
    {
        ChartData chart = new ChartData
        {
            songName = songName,
            bpm = bpm,
            offset = offset,
            laneCount = laneCount
        };

        if (bpm <= 0f || songLength <= 0f || laneCount <= 0 || subdivision <= 0)
        {
            Debug.LogError("SimpleChartGenerator: Invalid chart generation settings.");
            return chart;
        }

        float beatDuration    = 60f / bpm;
        float stepDuration    = beatDuration / subdivision;
        int stepsPerMeasure   = BeatsPerMeasure * subdivision;

        int previousLane      = -1;
        float previousNoteTime = -999f;
        float minSameLaneInterval = beatDuration;

        // Dùng stepIndex để tránh lỗi float tích lũy
        int stepIndex = 0;

        while (true)
        {
            float time = stepIndex * stepDuration;
            if (time >= songLength) break;

            // --- Tính trọng số của vị trí này trong measure ---
            int stepInMeasure = stepIndex % stepsPerMeasure;
            float weight = GetStepWeight(stepInMeasure, subdivision);

            // Xác suất đặt note = noteChance × weight, giới hạn tối đa 1.0
            float adjustedChance = Mathf.Min(1f, noteChance * weight);

            if (Random.value <= adjustedChance)
            {
                int lane = PickLane(laneCount, previousLane, time,
                                    previousNoteTime, minSameLaneInterval);

                NoteData note = new NoteData
                {
                    time           = time,
                    lane           = lane,
                    type           = NoteType.Tap,
                    duration       = 0f,
                    flickDirection = FlickDirection.Any,
                    slidePath      = null
                };

                chart.notes.Add(note);

                previousLane     = lane;
                previousNoteTime = time;
            }

            stepIndex++;
        }

        return chart;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Trả về trọng số của một step trong measure.
    ///
    /// Ví dụ subdivision=2 (8th notes), 4/4:
    ///   step 0: beat 1 (downbeat) → weight 3.0
    ///   step 1: off-beat          → weight 0.25
    ///   step 2: beat 2 (weak)     → weight 0.6
    ///   step 3: off-beat          → weight 0.25
    ///   step 4: beat 3 (mid-bar)  → weight 2.0
    ///   step 5: off-beat          → weight 0.25
    ///   step 6: beat 4 (weak)     → weight 0.6
    ///   step 7: off-beat          → weight 0.25
    /// </summary>
    private static float GetStepWeight(int stepInMeasure, int subdivision)
    {
        bool isOnBeat = (stepInMeasure % subdivision) == 0;

        if (!isOnBeat)
            return OffBeatWeight;

        int beatInMeasure = (stepInMeasure / subdivision) % BeatsPerMeasure;
        return BeatWeights[beatInMeasure];
    }

    private static int PickLane(
        int laneCount,
        int previousLane,
        float currentTime,
        float previousNoteTime,
        float minSameLaneInterval)
    {
        if (laneCount <= 1)
            return 0;

        int lane = Random.Range(0, laneCount);

        bool tooSoonSameLane =
            lane == previousLane &&
            currentTime - previousNoteTime < minSameLaneInterval;

        if (!tooSoonSameLane)
            return lane;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            lane = Random.Range(0, laneCount);
            if (lane != previousLane)
                return lane;
        }

        return (previousLane + 1) % laneCount;
    }
}