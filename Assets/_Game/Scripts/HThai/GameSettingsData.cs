using UnityEngine;

[System.Serializable]
public class GameSettingsData
{
    // --- LỐI CHƠI ---
    public float noteSpeed = 3.0f;      // Tốc độ rơi của nốt
    public int pauseType = 0;           // Kiểu tạm dừng (0: Chạm 1 lần, 1: Chạm đúp)

    // --- ÂM THANH ---
    public int offset = 0;              // Độ trễ (ms)
    public float noteVolume = 1.0f;     // Âm lượng nốt (SFX)
    public float musicVolume = 1.0f;    // Âm lượng nhạc (BGM) <--- CHÍNH DÒNG NÀY SẼ CHỮA HẾT LỖI ĐỎ

    // --- HÌNH ẢNH ---
    public int graphicsQuality = 1;     // Chất lượng đồ họa (0: Thấp, 1: Cao)
}