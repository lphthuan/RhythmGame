using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Phân tích AudioClip để ước tính BPM tự động.
///
/// Thuật toán (Onset Strength + Autocorrelation):
///   1. Đọc sóng âm thanh → mảng số
///   2. Tính "độ thay đổi năng lượng" theo từng frame (onset strength)
///      → frame nào âm thanh tăng mạnh = có khả năng là beat
///   3. Dùng Autocorrelation: kiểm tra xem chuỗi beat có chu kỳ nào lặp lại không
///      → tìm chu kỳ lặp lại mạnh nhất = khoảng cách giữa các beat = BPM
///
/// Chính xác hơn thuật toán interval-histogram với nhạc phức tạp.
/// Vẫn có thể sai với nhạc BPM thay đổi liên tục (variable BPM).
/// </summary>
public static class BpmDetector
{
    // Phân tích tối đa bao nhiêu giây đầu bài nhạc
    private const float AnalysisLengthSeconds = 45f;

    // Kích thước frame để tính năng lượng
    private const int FrameSize = 1024;

    // Bước nhảy giữa các frame
    private const int HopSize = 512;

    // Khoảng BPM hợp lệ
    private const int MinBpm = 60;
    private const int MaxBpm = 220;

    /// <summary>
    /// Phân tích AudioClip và trả về BPM ước tính.
    /// </summary>
    /// <param name="clip">AudioClip cần phân tích.</param>
    /// <returns>BPM ước tính. Trả về 120 nếu không đủ dữ liệu.</returns>
    public static float Detect(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("BpmDetector: AudioClip is null.");
            return 120f;
        }

        // Bước 1: Đọc samples, giới hạn độ dài phân tích
        int maxSamples = Mathf.Min(clip.samples,
            Mathf.FloorToInt(AnalysisLengthSeconds * clip.frequency));

        float[] mono = GetMonoSamples(clip, maxSamples);

        // Bước 2: Tính onset strength function
        // = mức độ "tăng năng lượng" ở mỗi frame
        float[] onset = ComputeOnsetStrength(mono);

        if (onset.Length < 64)
        {
            Debug.LogWarning("BpmDetector: File nhạc quá ngắn để phân tích.");
            return 120f;
        }

        // Bước 3: Tính autocorrelation của onset function
        // → tìm chu kỳ nào lặp lại nhiều nhất
        float[] autocorr = ComputeAutocorrelation(onset);

        // Bước 4: Chuyển lag (frame) sang BPM
        // Chỉ xét các lag tương ứng với BPM trong khoảng [MinBpm, MaxBpm]
        float fps = (float)clip.frequency / HopSize; // onset frames per second
        int lagMin = Mathf.CeilToInt(fps * 60f / MaxBpm);
        int lagMax = Mathf.FloorToInt(fps * 60f / MinBpm);

        lagMin = Mathf.Max(lagMin, 1);
        lagMax = Mathf.Min(lagMax, autocorr.Length - 1);

        // Bước 5: Tìm lag có autocorrelation cao nhất trong vùng BPM hợp lệ
        // (có kiểm tra thêm harmonic để tránh nhầm half-time)
        int bestLag = FindBestLag(autocorr, lagMin, lagMax);

        // Kiểm tra half-time: nếu bestLag × 0.5 vẫn trong range hợp lệ
        // và autocorr tại đó đủ cao → chọn lag ngắn hơn (BPM nhanh hơn).
        // Ví dụ: detect 85 BPM nhưng 170 BPM cũng khả thi → chọn 170.
        int halfLag = bestLag / 2;
        if (halfLag >= lagMin && halfLag <= lagMax)
        {
            float halfCorr = autocorr[halfLag];
            float fullCorr = autocorr[bestLag];
            if (halfCorr >= fullCorr * 0.55f)
            {
                bestLag = halfLag;
                Debug.Log("BpmDetector: Phát hiện half-time, chọn tempo nhanh hơn.");
            }
        }

        float detectedBpm = fps * 60f / bestLag;

        // Làm tròn tới 0.5 BPM
        detectedBpm = Mathf.Round(detectedBpm * 2f) / 2f;

        Debug.Log($"BpmDetector: Phát hiện được {detectedBpm:F1} BPM " +
                  $"(lag={bestLag}, fps={fps:F1})");

        return detectedBpm;
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Đọc samples từ AudioClip và chuyển về mono.
    /// </summary>
    private static float[] GetMonoSamples(AudioClip clip, int sampleCount)
    {
        int channels = clip.channels;
        float[] raw = new float[sampleCount * channels];
        clip.GetData(raw, 0);

        if (channels == 1)
            return raw;

        // Trộn channels về mono
        float[] mono = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float sum = 0f;
            for (int c = 0; c < channels; c++)
                sum += raw[i * channels + c];
            mono[i] = sum / channels;
        }
        return mono;
    }

    /// <summary>
    /// Tính Onset Strength Function:
    /// Mỗi frame = max(0, energy[frame] - energy[frame-1])
    /// → chỉ lấy phần tăng, bỏ phần giảm
    /// → đỉnh cao = âm thanh bắt đầu to lên = khả năng có beat
    /// </summary>
    private static float[] ComputeOnsetStrength(float[] mono)
    {
        int frameCount = (mono.Length - FrameSize) / HopSize;
        float[] energy = new float[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            int start = i * HopSize;
            float sum = 0f;
            for (int j = 0; j < FrameSize; j++)
            {
                float s = mono[start + j];
                sum += s * s;
            }
            energy[i] = Mathf.Sqrt(sum / FrameSize);
        }

        // Onset = phần tăng của energy (positive flux)
        float[] onset = new float[frameCount];
        for (int i = 1; i < frameCount; i++)
        {
            onset[i] = Mathf.Max(0f, energy[i] - energy[i - 1]);
        }

        // Normalize về [0, 1]
        float maxVal = 0f;
        foreach (float v in onset) if (v > maxVal) maxVal = v;
        if (maxVal > 0f)
            for (int i = 0; i < onset.Length; i++)
                onset[i] /= maxVal;

        return onset;
    }

    /// <summary>
    /// Tính Autocorrelation của onset function.
    /// autocorr[lag] = tổng(onset[i] * onset[i+lag]) cho tất cả i
    /// → lag nào cho giá trị cao = onset có chu kỳ lặp lại theo lag đó
    /// </summary>
    private static float[] ComputeAutocorrelation(float[] signal)
    {
        // Chỉ tính đến maxLag để tiết kiệm thời gian
        int maxLag = signal.Length / 2;
        float[] result = new float[maxLag];

        for (int lag = 1; lag < maxLag; lag++)
        {
            float sum = 0f;
            int count = signal.Length - lag;
            for (int i = 0; i < count; i++)
            {
                sum += signal[i] * signal[i + lag];
            }
            result[lag] = sum / count;
        }

        return result;
    }

    /// <summary>
    /// Tìm lag tốt nhất trong [lagMin, lagMax].
    /// Có kiểm tra harmonic để tránh nhầm half-time (BPM/2) hoặc double-time (BPM*2).
    ///
    /// Ví dụ: BPM thật 170 → lag thật = L
    /// Half-time 85 BPM → lag = 2L (autocorr thường cũng cao)
    /// Thuật toán ưu tiên lag nhỏ hơn (BPM cao hơn) nếu harmonic của nó cũng mạnh.
    /// </summary>
    private static int FindBestLag(float[] autocorr, int lagMin, int lagMax)
    {
        // Tìm tất cả peak trong vùng hợp lệ
        List<(int lag, float value)> peaks = new List<(int, float)>();

        for (int lag = lagMin; lag <= lagMax; lag++)
        {
            bool isPeak = autocorr[lag] > autocorr[lag - 1] &&
                          (lag + 1 >= autocorr.Length || autocorr[lag] >= autocorr[lag + 1]);

            if (isPeak)
                peaks.Add((lag, autocorr[lag]));
        }

        if (peaks.Count == 0)
        {
            // Fallback: lấy lag có giá trị cao nhất
            float best = -1f;
            int bestLag = lagMin;
            for (int lag = lagMin; lag <= lagMax; lag++)
            {
                if (autocorr[lag] > best)
                {
                    best = autocorr[lag];
                    bestLag = lag;
                }
            }
            return bestLag;
        }

        // Sắp xếp peaks theo autocorr value giảm dần
        peaks.Sort((a, b) => b.value.CompareTo(a.value));

        // Kiểm tra: peak cao nhất có phải là half-time của peak khác không?
        // Nếu lag[0] ≈ 2 * lag[1] → thì lag[1] (BPM cao hơn) có thể đúng hơn
        int topLag = peaks[0].lag;

        if (peaks.Count >= 2)
        {
            int secondLag = peaks[1].lag;
            float ratio = (float)topLag / secondLag;

            // Nếu top lag ≈ 2× second lag → second lag (nhanh hơn) khả năng đúng hơn
            if (ratio > 1.8f && ratio < 2.2f && peaks[1].value > peaks[0].value * 0.6f)
            {
                return secondLag;
            }
        }

        return topLag;
    }
}
