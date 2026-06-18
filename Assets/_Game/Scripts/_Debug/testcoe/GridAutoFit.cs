using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class GridAutoFit : MonoBehaviour
{
    public int columns = 3;
    public float spacingX = 10f;
    public float paddingLeft = 15f;
    public float paddingRight = 15f;

    private GridLayoutGroup grid;
    private RectTransform rect;

    private void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();
        rect = GetComponent<RectTransform>();
        Fit(); // chuyển Fit() vào đây thay vì Start()
    }

    private void OnRectTransformDimensionsChange()
    {
        // Chỉ gọi khi đã khởi tạo xong
        if (grid == null || rect == null) return;
        Fit();
    }

    private void Fit()
    {
        float totalWidth = rect.rect.width;
        if (totalWidth <= 0) return;

        float availableWidth = totalWidth - paddingLeft - paddingRight
                               - spacingX * (columns - 1);
        float cellWidth = Mathf.Floor(availableWidth / columns);

        // Giới hạn kích thước tối đa
        cellWidth = Mathf.Min(cellWidth, 200f); // cap lại tối đa 200px

        float cellHeight = cellWidth * 1.3f; // tỉ lệ cao hơn rộng một chút

        grid.cellSize = new Vector2(cellWidth, cellHeight);
        grid.spacing = new Vector2(spacingX, spacingX);
        grid.padding.left = (int)paddingLeft;
        grid.padding.right = (int)paddingRight;
    }
}