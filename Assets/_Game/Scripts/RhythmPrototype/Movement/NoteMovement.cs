using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class NoteMovement : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private float hitTime;
    [SerializeField] private float hitlineY;
    [SerializeField] private float scrollSpeed = 600f;

    private RectTransform rectTransform;
    private bool initialized;

    public float HitTime => hitTime;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(NoteRuntimeData data)
    {
        hitTime = data.hitTime;
        hitlineY = data.hitlineY;
        scrollSpeed = data.scrollSpeed;

        Vector2 pos = rectTransform.anchoredPosition;
        pos.x = data.anchoredX;
        rectTransform.anchoredPosition = pos;

        initialized = true;
    }

    public void Tick(float currentTime)
    {
        if (!initialized)
            return;

        float y = hitlineY + (hitTime - currentTime) * scrollSpeed;

        Vector2 pos = rectTransform.anchoredPosition;
        pos.y = y;
        rectTransform.anchoredPosition = pos;
    }
}