using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlideCheckpointSystem : MonoBehaviour
{
    [Header("Checkpoint")]
    [SerializeField] private float checkpointRadius = 120f;
    [SerializeField] private List<RectTransform> checkpoints = new List<RectTransform>();

    private int currentIndex;
    private bool running;
    private bool failed;

    public int CurrentIndex => currentIndex;
    public int TotalCount => checkpoints.Count;

    public void Initialize(List<RectTransform> newCheckpoints, float radius)
    {
        checkpoints = newCheckpoints;
        checkpointRadius = radius;
        ResetSystem();
    }

    public void ResetSystem()
    {
        currentIndex = 0;
        running = false;
        failed = false;

        RefreshVisual();
    }

    public bool Begin(Vector2 screenPosition)
    {
        ResetSystem();
        running = true;

        if (!IsNearCheckpoint(screenPosition, currentIndex))
        {
            failed = true;
            running = false;
            return false;
        }

        MarkCheckpoint(currentIndex);
        currentIndex++;

        return true;
    }

    public void Move(Vector2 screenPosition)
    {
        if (!running || failed)
            return;

        if (currentIndex >= checkpoints.Count)
            return;

        if (IsNearCheckpoint(screenPosition, currentIndex))
        {
            MarkCheckpoint(currentIndex);
            currentIndex++;
        }
    }

    public void Cancel()
    {
        running = false;
        failed = true;
        MarkRemainingFailed();
    }

    public bool IsCompleted()
    {
        return running && !failed && currentIndex >= checkpoints.Count;
    }

    public bool IsFailed()
    {
        return failed;
    }

    private bool IsNearCheckpoint(Vector2 screenPosition, int index)
    {
        if (index < 0 || index >= checkpoints.Count)
            return false;

        Vector2 checkpointScreenPosition = RectTransformUtility.WorldToScreenPoint(
            null,
            checkpoints[index].position
        );

        float distance = Vector2.Distance(screenPosition, checkpointScreenPosition);

        return distance <= checkpointRadius;
    }

    private void MarkCheckpoint(int index)
    {
        if (index < 0 || index >= checkpoints.Count)
            return;

        Image image = checkpoints[index].GetComponent<Image>();

        if (image != null)
            image.color = Color.green;
    }

    private void MarkRemainingFailed()
    {
        for (int i = currentIndex; i < checkpoints.Count; i++)
        {
            Image image = checkpoints[i].GetComponent<Image>();

            if (image != null)
                image.color = Color.red;
        }
    }

    private void RefreshVisual()
    {
        for (int i = 0; i < checkpoints.Count; i++)
        {
            Image image = checkpoints[i].GetComponent<Image>();

            if (image != null)
                image.color = Color.white;
        }
    }
}