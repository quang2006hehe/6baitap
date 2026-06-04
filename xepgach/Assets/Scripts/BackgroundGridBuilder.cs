using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Bắt buộc GameObject gắn script này phải có component GridLayoutGroup
// Nếu chưa có thì Unity sẽ tự thêm vào
[RequireComponent(typeof(GridLayoutGroup))]
public class BackgroundGridBuilder : MonoBehaviour
{
    [Header("Kích thước bàn chơi")]
    [SerializeField] private int columns = 10; // Số cột của bàn chơi Tetris
    [SerializeField] private int rows = 20;    // Số hàng của bàn chơi Tetris

    [Header("Tham chiếu đối tượng")]
    [SerializeField] private Image cellPrefab; // Prefab ô nền dùng để tạo lưới

    [Header("Thiết lập bố cục")]
    [SerializeField] private float spacing = 2f;          // Khoảng cách giữa các ô
    [SerializeField] private bool generateOnStart = true; // Có tự tạo lưới khi bắt đầu game hay không

    private GridLayoutGroup grid;           // Component dùng để sắp xếp các ô theo dạng lưới
    private RectTransform rectTransform;    // RectTransform của vùng chứa lưới

    private void Awake()
    {
        // Lấy GridLayoutGroup gắn trên chính GameObject này
        grid = GetComponent<GridLayoutGroup>();

        // Lấy RectTransform để biết kích thước vùng tạo lưới
        rectTransform = GetComponent<RectTransform>();
    }

    private IEnumerator Start()
    {
        // Chờ 1 frame để Canvas cập nhật kích thước đầy đủ
        yield return null;

        // Bắt Unity cập nhật lại toàn bộ Canvas
        Canvas.ForceUpdateCanvases();

        // Nếu bật generateOnStart thì tự động tạo lưới khi game chạy
        if (generateOnStart)
        {
            GenerateGrid();
        }
    }

    // Cho phép bấm chuột phải vào component trong Inspector để tạo lưới thủ công
    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        // Kiểm tra xem đã gán prefab ô nền chưa
        if (cellPrefab == null)
        {
            Debug.LogError("Chưa gán Cell Prefab.", this);
            return;
        }

        // Nếu biến grid bị null thì lấy lại GridLayoutGroup
        if (grid == null)
            grid = GetComponent<GridLayoutGroup>();

        // Nếu biến rectTransform bị null thì lấy lại RectTransform
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        // Xóa toàn bộ ô cũ trước khi tạo lưới mới
        ClearChildren();

        // Cập nhật lại Canvas để lấy đúng kích thước vùng chứa
        Canvas.ForceUpdateCanvases();

        // Lấy chiều rộng và chiều cao của vùng chứa lưới
        float areaWidth = rectTransform.rect.width;
        float areaHeight = rectTransform.rect.height;

        // Nếu vùng chứa chưa có kích thước hợp lệ thì báo lỗi
        if (areaWidth <= 0 || areaHeight <= 0)
        {
            Debug.LogError("BackgroundGridRoot chưa có kích thước hợp lệ.", this);
            return;
        }

        // Tính kích thước mỗi ô sao cho toàn bộ lưới vừa trong vùng chứa
        float cellSize = Mathf.Floor(
            Mathf.Min(
                (areaWidth - spacing * (columns - 1)) / columns,
                (areaHeight - spacing * (rows - 1)) / rows
            )
        );

        // Nếu kích thước ô không hợp lệ thì dừng lại
        if (cellSize <= 0)
        {
            Debug.LogError("Cell size không hợp lệ.", this);
            return;
        }

        // Tính tổng chiều rộng và chiều cao thực tế mà lưới sẽ sử dụng
        float usedWidth = cellSize * columns + spacing * (columns - 1);
        float usedHeight = cellSize * rows + spacing * (rows - 1);

        // Tính padding để căn lưới vào giữa vùng chứa
        int padLeft = Mathf.RoundToInt((areaWidth - usedWidth) * 0.5f);
        int padRight = Mathf.RoundToInt(areaWidth - usedWidth - padLeft);
        int padTop = Mathf.RoundToInt((areaHeight - usedHeight) * 0.5f);
        int padBottom = Mathf.RoundToInt(areaHeight - usedHeight - padTop);

        // Thiết lập GridLayoutGroup để sắp xếp ô từ góc trên bên trái
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;

        // Cố định số cột theo biến columns
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;

        // Gán kích thước ô và khoảng cách giữa các ô
        grid.cellSize = new Vector2(cellSize, cellSize);
        grid.spacing = new Vector2(spacing, spacing);

        // Gán padding để lưới nằm cân đối trong khung
        grid.padding = new RectOffset(padLeft, padRight, padTop, padBottom);

        // Tổng số ô cần tạo = số cột * số hàng
        int total = columns * rows;

        // Tạo từng ô nền cho bàn chơi
        for (int i = 0; i < total; i++)
        {
            // Tạo một ô mới từ prefab
            Image newCell = Instantiate(cellPrefab, transform, false);

            // Lấy RectTransform của ô mới
            RectTransform cellRect = newCell.rectTransform;

            // Đặt anchor và pivot về giữa để ô hiển thị ổn định
            cellRect.anchorMin = new Vector2(0.5f, 0.5f);
            cellRect.anchorMax = new Vector2(0.5f, 0.5f);
            cellRect.pivot = new Vector2(0.5f, 0.5f);

            // Đặt lại scale và rotation mặc định
            cellRect.localScale = Vector3.one;
            cellRect.localRotation = Quaternion.identity;

            // Tắt Raycast Target để ô nền không chặn thao tác bấm UI
            newCell.raycastTarget = false;
        }

        // Ép Unity cập nhật lại layout ngay lập tức
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private void ClearChildren()
    {
        // Duyệt ngược từ cuối về đầu để xóa toàn bộ ô con cũ
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;

            // Nếu đang chạy game thì dùng Destroy
            if (Application.isPlaying)
                Destroy(child);

            // Nếu đang ở chế độ chỉnh sửa trong Unity Editor thì dùng DestroyImmediate
            else
                DestroyImmediate(child);
        }
    }

    private void OnValidate()
    {
        // Đảm bảo số cột luôn lớn hơn hoặc bằng 1
        columns = Mathf.Max(1, columns);

        // Đảm bảo số hàng luôn lớn hơn hoặc bằng 1
        rows = Mathf.Max(1, rows);

        // Đảm bảo khoảng cách giữa các ô không bị âm
        spacing = Mathf.Max(0f, spacing);
    }
}