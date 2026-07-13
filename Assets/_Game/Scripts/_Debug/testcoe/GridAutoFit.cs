using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class GridAutoFit : MonoBehaviour
{
    [Tooltip("Số cột cố định. Đặt 0 = tự tính số cột theo bề rộng màn hình (khuyên dùng cho mobile)")]
    public int columns = 0;
    public float spacingX = 10f;
    public float paddingLeft = 15f;
    public float paddingRight = 15f;

    [Tooltip("Bề rộng tối đa của 1 ô (px) - tăng số này nếu muốn ô to hơn nữa")]
    public float maxCellWidth = 280f;

    [Tooltip("Tỉ lệ chiều cao / chiều rộng của ô")]
    public float heightRatio = 1.3f;

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

        // columns = 0 -> tự tính: nhét được bao nhiêu ô cỡ maxCellWidth thì bấy nhiêu cột
        int cols = columns;
        if (cols <= 0)
        {
            cols = Mathf.Max(1, Mathf.FloorToInt(
                (totalWidth - paddingLeft - paddingRight + spacingX) / (maxCellWidth + spacingX)));
        }

        float availableWidth = totalWidth - paddingLeft - paddingRight
                               - spacingX * (cols - 1);
        float cellWidth = Mathf.Floor(availableWidth / cols);

        // Giới hạn kích thước tối đa
        cellWidth = Mathf.Min(cellWidth, maxCellWidth);

        float cellHeight = cellWidth * heightRatio; // ô cao hơn rộng để chứa tên + nút mua

        // Ép số cột của GridLayoutGroup khớp để không bị tràn hàng
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;

        grid.cellSize = new Vector2(cellWidth, cellHeight);
        grid.spacing = new Vector2(spacingX, spacingX);
        grid.padding.left = (int)paddingLeft;
        grid.padding.right = (int)paddingRight;
    }
}
