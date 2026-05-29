using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

// Script giúp bất kỳ nút bấm nào cũng có thể ấn giữ để tự lặp lại hành động
public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Sự kiện khi ấn giữ")]
    public UnityEvent onHold;

    [Header("Cấu hình thời gian (Giây)")]
    [Tooltip("Thời gian phải giữ chuột liên tục trước khi bắt đầu tự động nhảy số")]
    public float delayBeforeRepeat = 0.4f;

    [Tooltip("Tốc độ nhảy số khi đang giữ (Số càng nhỏ nhảy càng nhanh)")]
    public float repeatInterval = 0.06f;

    private bool isHolding = false;
    private float nextActionTime = 0f;
    private bool isFirstTickAfterDelay = false;

    void Update()
    {
        if (isHolding)
        {
            // Sử dụng unscaledTime để tính năng chạy được ngay cả khi game đang tạm dừng (Time.timeScale = 0)
            if (Time.unscaledTime >= nextActionTime)
            {
                onHold.Invoke();

                if (isFirstTickAfterDelay)
                {
                    isFirstTickAfterDelay = false;
                }

                nextActionTime = Time.unscaledTime + repeatInterval;
            }
        }
    }

    // Khi người chơi vừa nhấn giữ chuột/chạm tay vào nút
    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
        isFirstTickAfterDelay = true;

        onHold.Invoke(); // Chạy lệnh ngay lập tức 1 lần (giống như click đơn)

        // Đặt mốc thời gian chờ cho lần lặp tiếp theo
        nextActionTime = Time.unscaledTime + delayBeforeRepeat;
    }

    // Khi người chơi buông chuột/thả tay ra
    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
    }

    // Khi người chơi đang giữ nhưng rê chuột trượt ra ngoài phạm vi của nút
    public void OnPointerExit(PointerEventData eventData)
    {
        isHolding = false;
    }

    // Tự động tắt giữ nếu Object này bị ẩn đi nửa chừng
    void OnDisable()
    {
        isHolding = false;
    }
}