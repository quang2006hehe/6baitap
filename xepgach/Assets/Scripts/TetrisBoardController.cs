using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Script điều khiển toàn bộ bàn chơi Tetris:
/// - Tạo khối gạch mới
/// - Điều khiển di chuyển / xoay khối
/// - Cho khối tự rơi theo thời gian
/// - Khóa khối khi chạm đáy hoặc chạm khối khác
/// - Xóa hàng đầy
/// - Tính điểm và số hàng
/// - Hiển thị Game Over
/// - Chơi lại game
/// </summary>
public class TetrisBoardController : MonoBehaviour
{
    /// <summary>
    /// Danh sách 7 loại khối cơ bản trong game Tetris.
    /// O: hình vuông
    /// I: thanh dài
    /// T, L, J, S, Z: các khối còn lại
    /// </summary>
    private enum TetrominoType
    {
        O, I, T, L, J, S, Z
    }

    [Header("Kích thước bàn chơi")]
    [SerializeField] private int columns = 10; // Số cột của bàn chơi
    [SerializeField] private int rows = 20;    // Số hàng của bàn chơi

    [Header("Các thành phần tham chiếu")]
    [SerializeField] private RectTransform backgroundGridRoot; // Vùng chứa lưới nền
    [SerializeField] private RectTransform cellsRoot;          // Vùng chứa các ô gạch đã rơi xuống và bị khóa lại
    [SerializeField] private RectTransform activePieceRoot;    // Vùng chứa khối gạch đang rơi
    [SerializeField] private Image cellPrefab;                 // Prefab của một ô gạch

    [Header("Giao diện phía trên")]
    [SerializeField] private TextMeshProUGUI scoreValueText; // Text hiển thị điểm hiện tại
    [SerializeField] private TextMeshProUGUI linesValueText; // Text hiển thị số hàng đã xóa

    [Header("Giao diện Game Over")]
    [SerializeField] private GameObject gameOverPanel;              // Bảng Game Over
    [SerializeField] private TextMeshProUGUI finalScoreValueText;   // Text hiển thị điểm cuối cùng
    [SerializeField] private TextMeshProUGUI finalLinesValueText;   // Text hiển thị số hàng cuối cùng

    [Header("Tốc độ rơi")]
    [SerializeField] private float fallInterval = 0.7f; // Thời gian giữa mỗi lần khối tự rơi xuống 1 ô

    private GridLayoutGroup backgroundGridLayout; // GridLayoutGroup dùng để lấy kích thước ô và khoảng cách ô

    // Mảng kiểm tra ô nào trên bàn chơi đã bị chiếm bởi gạch đã khóa
    private bool[,] occupied;

    // Mảng lưu Image của các ô gạch đã khóa để có thể xóa / dịch chuyển khi xóa hàng
    private Image[,] lockedCellImages;

    // Danh sách Image của khối gạch đang rơi
    private readonly List<Image> activeCellImages = new List<Image>();

    private TetrominoType activeType;   // Loại khối đang rơi
    private Vector2Int activeOrigin;    // Vị trí gốc của khối đang rơi trên bàn chơi
    private Vector2Int[] activeShape;   // Danh sách vị trí các ô con tạo thành khối
    private Color activeColor;          // Màu của khối đang rơi

    private float fallTimer;            // Bộ đếm thời gian để khối tự rơi
    private bool isGameOver;            // Kiểm tra game đã kết thúc chưa

    private int score;                  // Điểm hiện tại
    private int lines;                  // Tổng số hàng đã xóa

    /// <summary>
    /// Hàm Awake chạy đầu tiên khi object được tạo.
    /// Dùng để lấy component cần thiết và khởi tạo mảng dữ liệu bàn chơi.
    /// </summary>
    private void Awake()
    {
        // Lấy GridLayoutGroup từ vùng lưới nền để dùng kích thước ô cho gạch
        backgroundGridLayout = backgroundGridRoot.GetComponent<GridLayoutGroup>();

        // Khởi tạo mảng đánh dấu ô đã có gạch
        occupied = new bool[columns, rows];

        // Khởi tạo mảng lưu ảnh của các ô gạch đã rơi xuống
        lockedCellImages = new Image[columns, rows];
    }

    /// <summary>
    /// Hàm Start chạy khi bắt đầu game.
    /// Kiểm tra lưới, ẩn Game Over, cập nhật UI và tạo khối đầu tiên.
    /// </summary>
    private void Start()
    {
        // Nếu BackgroundGridRoot chưa có GridLayoutGroup thì báo lỗi và tắt script
        if (backgroundGridLayout == null)
        {
            Debug.LogError("BackgroundGridRoot chưa có GridLayoutGroup.", this);
            enabled = false;
            return;
        }

        // Ẩn bảng Game Over khi mới vào game
        HideGameOverUI();

        // Cập nhật điểm và hàng ban đầu lên giao diện
        UpdateUI();

        // Tạo khối gạch đầu tiên
        SpawnRandomPiece();
    }

    /// <summary>
    /// Hàm Update chạy liên tục mỗi frame.
    /// Dùng để nhận phím điều khiển và cho khối tự rơi theo thời gian.
    /// </summary>
    private void Update()
    {
        // Xử lý thao tác bàn phím
        HandleInput();

        // Nếu đã Game Over thì không cho khối tiếp tục rơi
        if (isGameOver)
            return;

        // Tăng bộ đếm thời gian rơi
        fallTimer += Time.deltaTime;

        // Khi đủ thời gian thì cho khối rơi xuống 1 ô
        if (fallTimer >= fallInterval)
        {
            fallTimer = 0f;
            StepDown();
        }
    }

    /// <summary>
    /// Xử lý các phím điều khiển:
    /// - A hoặc mũi tên trái: sang trái
    /// - D hoặc mũi tên phải: sang phải
    /// - S hoặc mũi tên xuống: rơi nhanh xuống 1 ô
    /// - W, mũi tên lên hoặc Space: xoay khối
    /// - R: chơi lại
    /// </summary>
    private void HandleInput()
    {
        // Nếu không có bàn phím thì không xử lý
        if (Keyboard.current == null)
            return;

        // Nhấn R để chơi lại bất cứ lúc nào
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartBoard();
            return;
        }

        // Nếu đã Game Over thì không cho điều khiển khối nữa
        if (isGameOver)
            return;

        // Di chuyển khối sang trái
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
        {
            TryMove(new Vector2Int(-1, 0));
        }

        // Di chuyển khối sang phải
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
        {
            TryMove(new Vector2Int(1, 0));
        }

        // Cho khối rơi xuống nhanh hơn 1 ô
        if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
        {
            StepDown();
            fallTimer = 0f;
        }

        // Xoay khối theo chiều kim đồng hồ
        if (Keyboard.current.upArrowKey.wasPressedThisFrame ||
            Keyboard.current.wKey.wasPressedThisFrame ||
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryRotateClockwise();
        }
    }

    /// <summary>
    /// Cho khối hiện tại rơi xuống 1 ô.
    /// Nếu không rơi được nữa thì khóa khối, kiểm tra xóa hàng và tạo khối mới.
    /// </summary>
    private void StepDown()
    {
        // Thử di chuyển khối xuống dưới 1 ô
        bool moved = TryMove(new Vector2Int(0, -1));

        // Nếu không di chuyển xuống được nữa
        if (!moved)
        {
            // Khóa khối hiện tại vào bàn chơi
            LockActivePiece();

            // Kiểm tra và xóa các hàng đã đầy
            int clearedLines = ClearCompletedLines();

            // Nếu có hàng bị xóa thì cộng điểm và cập nhật số hàng
            if (clearedLines > 0)
            {
                AddScoreAndLines(clearedLines);
            }

            // Tạo khối mới
            SpawnRandomPiece();
        }
    }

    /// <summary>
    /// Thử di chuyển khối đang rơi theo hướng truyền vào.
    /// Nếu vị trí mới hợp lệ thì cập nhật vị trí khối.
    /// </summary>
    private bool TryMove(Vector2Int delta)
    {
        // Tính vị trí gốc mới sau khi di chuyển
        Vector2Int nextOrigin = activeOrigin + delta;

        // Nếu vị trí mới không hợp lệ thì không cho di chuyển
        if (!IsValidPosition(nextOrigin, activeShape))
            return false;

        // Cập nhật vị trí mới
        activeOrigin = nextOrigin;

        // Vẽ lại khối đang rơi theo vị trí mới
        RefreshActivePieceVisual();

        return true;
    }

    /// <summary>
    /// Thử xoay khối theo chiều kim đồng hồ.
    /// Khối O là hình vuông nên không cần xoay.
    /// Có dùng kickTests để thử dịch trái/phải khi xoay sát tường.
    /// </summary>
    private void TryRotateClockwise()
    {
        // Khối O xoay cũng không đổi hình nên bỏ qua
        if (activeType == TetrominoType.O)
            return;

        // Tạo hình dạng mới sau khi xoay
        Vector2Int[] rotatedShape = RotateClockwise(activeShape);

        // Danh sách các vị trí thử sau khi xoay
        // Giúp khối vẫn xoay được khi gần tường hoặc gần khối khác
        Vector2Int[] kickTests = new Vector2Int[]
        {
            Vector2Int.zero,
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(-2, 0),
            new Vector2Int(2, 0),
            new Vector2Int(0, 1)
        };

        // Thử từng vị trí dịch chuyển
        for (int i = 0; i < kickTests.Length; i++)
        {
            Vector2Int testOrigin = activeOrigin + kickTests[i];

            // Nếu vị trí xoay hợp lệ thì áp dụng xoay
            if (IsValidPosition(testOrigin, rotatedShape))
            {
                activeOrigin = testOrigin;
                activeShape = rotatedShape;
                RefreshActivePieceVisual();
                return;
            }
        }
    }

    /// <summary>
    /// Tính tọa độ các ô sau khi xoay 90 độ theo chiều kim đồng hồ.
    /// Công thức xoay: (x, y) thành (y, -x).
    /// </summary>
    private Vector2Int[] RotateClockwise(Vector2Int[] shape)
    {
        Vector2Int[] rotated = new Vector2Int[shape.Length];

        for (int i = 0; i < shape.Length; i++)
        {
            Vector2Int block = shape[i];
            rotated[i] = new Vector2Int(block.y, -block.x);
        }

        return rotated;
    }

    /// <summary>
    /// Tạo ngẫu nhiên một khối Tetris mới.
    /// Nếu vị trí sinh khối đã bị chiếm thì game kết thúc.
    /// </summary>
    private void SpawnRandomPiece()
    {
        // Xóa hình ảnh khối cũ nếu còn tồn tại
        ClearActivePieceVisual();

        // Random loại khối từ 0 đến 6
        activeType = (TetrominoType)Random.Range(0, 7);

        // Lấy hình dạng, màu và vị trí sinh của khối
        activeShape = GetSpawnShape(activeType);
        activeColor = GetPieceColor(activeType);
        activeOrigin = GetSpawnOrigin(activeType);

        // Nếu vị trí sinh khối không hợp lệ thì Game Over
        if (!IsValidPosition(activeOrigin, activeShape))
        {
            TriggerGameOver();
            return;
        }

        // Tạo các ô Image cho khối đang rơi
        for (int i = 0; i < activeShape.Length; i++)
        {
            Image newCell = Instantiate(cellPrefab, activePieceRoot);
            newCell.color = activeColor;
            newCell.raycastTarget = false;
            activeCellImages.Add(newCell);
        }

        // Cập nhật vị trí hiển thị của khối mới
        RefreshActivePieceVisual();
    }

    /// <summary>
    /// Kích hoạt trạng thái Game Over và hiển thị bảng Game Over.
    /// </summary>
    private void TriggerGameOver()
    {
        isGameOver = true;
        ShowGameOverUI();
        Debug.Log("Game Over");
    }

    /// <summary>
    /// Trả về vị trí sinh khối ban đầu trên bàn chơi.
    /// Với bàn 10x20, vị trí x = 4 và y = 18 sẽ nằm gần phía trên.
    /// </summary>
    private Vector2Int GetSpawnOrigin(TetrominoType type)
    {
        return new Vector2Int(4, 18);
    }

    /// <summary>
    /// Trả về danh sách tọa độ các ô con của từng loại khối Tetris.
    /// Các tọa độ này là vị trí tương đối so với activeOrigin.
    /// </summary>
    private Vector2Int[] GetSpawnShape(TetrominoType type)
    {
        switch (type)
        {
            case TetrominoType.O:
                return new Vector2Int[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1)
                };

            case TetrominoType.I:
                return new Vector2Int[]
                {
                    new Vector2Int(-1, 0),
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(2, 0)
                };

            case TetrominoType.T:
                return new Vector2Int[]
                {
                    new Vector2Int(-1, 0),
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(0, 1)
                };

            case TetrominoType.L:
                return new Vector2Int[]
                {
                    new Vector2Int(-1, 0),
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(1, 1)
                };

            case TetrominoType.J:
                return new Vector2Int[]
                {
                    new Vector2Int(-1, 0),
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(-1, 1)
                };

            case TetrominoType.S:
                return new Vector2Int[]
                {
                    new Vector2Int(0, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(-1, 1),
                    new Vector2Int(0, 1)
                };

            case TetrominoType.Z:
                return new Vector2Int[]
                {
                    new Vector2Int(-1, 0),
                    new Vector2Int(0, 0),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1)
                };
        }

        // Trường hợp dự phòng nếu type không khớp
        return new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        };
    }

    /// <summary>
    /// Lấy màu tương ứng với từng loại khối.
    /// </summary>
    private Color GetPieceColor(TetrominoType type)
    {
        switch (type)
        {
            case TetrominoType.O: return new Color32(255, 215, 70, 255);  // Vàng
            case TetrominoType.I: return new Color32(70, 220, 255, 255);  // Xanh cyan
            case TetrominoType.T: return new Color32(180, 90, 255, 255);  // Tím
            case TetrominoType.L: return new Color32(255, 150, 60, 255);  // Cam
            case TetrominoType.J: return new Color32(70, 120, 255, 255);  // Xanh dương
            case TetrominoType.S: return new Color32(90, 220, 120, 255);  // Xanh lá
            case TetrominoType.Z: return new Color32(255, 90, 90, 255);   // Đỏ
        }

        return Color.white;
    }

    /// <summary>
    /// Kiểm tra một vị trí của khối có hợp lệ không.
    /// Vị trí hợp lệ là:
    /// - Không vượt ra ngoài bàn chơi
    /// - Không trùng với ô đã có gạch
    /// </summary>
    private bool IsValidPosition(Vector2Int origin, Vector2Int[] shape)
    {
        for (int i = 0; i < shape.Length; i++)
        {
            Vector2Int boardPos = origin + shape[i];

            // Không cho vượt ra ngoài bên trái hoặc bên phải
            if (boardPos.x < 0 || boardPos.x >= columns)
                return false;

            // Không cho vượt quá đáy hoặc đỉnh bàn chơi
            if (boardPos.y < 0 || boardPos.y >= rows)
                return false;

            // Không cho đi vào ô đã có gạch
            if (occupied[boardPos.x, boardPos.y])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Cập nhật vị trí và màu sắc của khối đang rơi trên giao diện.
    /// </summary>
    private void RefreshActivePieceVisual()
    {
        for (int i = 0; i < activeShape.Length; i++)
        {
            Vector2Int boardPos = activeOrigin + activeShape[i];

            // Đặt ô gạch vào đúng vị trí trên bàn chơi
            PositionCell(activeCellImages[i].rectTransform, boardPos.x, boardPos.y);

            // Cập nhật màu cho ô gạch
            activeCellImages[i].color = activeColor;
        }
    }

    /// <summary>
    /// Khóa khối đang rơi vào bàn chơi.
    /// Sau khi khóa, các ô của khối sẽ trở thành gạch cố định.
    /// </summary>
    private void LockActivePiece()
    {
        for (int i = 0; i < activeShape.Length; i++)
        {
            Vector2Int boardPos = activeOrigin + activeShape[i];

            // Đánh dấu vị trí này đã có gạch
            occupied[boardPos.x, boardPos.y] = true;

            // Tạo Image mới cho ô gạch đã khóa
            Image lockedCell = Instantiate(cellPrefab, cellsRoot);
            lockedCell.color = activeColor;
            lockedCell.raycastTarget = false;

            // Đặt ô gạch đã khóa vào đúng vị trí
            PositionCell(lockedCell.rectTransform, boardPos.x, boardPos.y);

            // Lưu lại Image để sau này có thể xóa hoặc dịch chuyển
            lockedCellImages[boardPos.x, boardPos.y] = lockedCell;
        }

        // Xóa hình ảnh khối đang rơi vì nó đã được chuyển thành gạch cố định
        ClearActivePieceVisual();
    }

    /// <summary>
    /// Kiểm tra toàn bộ bàn chơi và xóa các hàng đã đầy.
    /// Trả về số hàng đã xóa.
    /// </summary>
    private int ClearCompletedLines()
    {
        int clearedCount = 0;

        // Duyệt từng hàng từ dưới lên trên
        for (int y = 0; y < rows; y++)
        {
            // Nếu hàng đầy thì xóa hàng
            if (IsRowFull(y))
            {
                ClearRow(y);
                ShiftRowsDown(y);
                clearedCount++;

                // Lùi y lại để kiểm tra tiếp hàng vừa được kéo xuống
                y--;
            }
        }

        return clearedCount;
    }

    /// <summary>
    /// Kiểm tra một hàng có đầy gạch hay chưa.
    /// Nếu tất cả cột trong hàng đều có gạch thì hàng đó đầy.
    /// </summary>
    private bool IsRowFull(int row)
    {
        for (int x = 0; x < columns; x++)
        {
            if (!occupied[x, row])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Xóa toàn bộ ô gạch trong một hàng.
    /// </summary>
    private void ClearRow(int row)
    {
        for (int x = 0; x < columns; x++)
        {
            // Đánh dấu ô không còn bị chiếm
            occupied[x, row] = false;

            // Xóa hình ảnh ô gạch nếu có
            if (lockedCellImages[x, row] != null)
            {
                Destroy(lockedCellImages[x, row].gameObject);
                lockedCellImages[x, row] = null;
            }
        }
    }

    /// <summary>
    /// Sau khi xóa một hàng, dịch toàn bộ các hàng phía trên xuống 1 ô.
    /// </summary>
    private void ShiftRowsDown(int clearedRow)
    {
        // Duyệt từ hàng ngay phía trên hàng bị xóa lên đến hàng trên cùng
        for (int y = clearedRow + 1; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                // Chuyển trạng thái ô từ hàng y xuống hàng y - 1
                occupied[x, y - 1] = occupied[x, y];

                // Chuyển Image của ô gạch xuống hàng y - 1
                lockedCellImages[x, y - 1] = lockedCellImages[x, y];

                // Nếu có ô gạch thì cập nhật lại vị trí hiển thị của nó
                if (lockedCellImages[x, y - 1] != null)
                {
                    PositionCell(lockedCellImages[x, y - 1].rectTransform, x, y - 1);
                }
            }
        }

        // Sau khi dịch xuống, hàng trên cùng phải được làm trống
        for (int x = 0; x < columns; x++)
        {
            occupied[x, rows - 1] = false;
            lockedCellImages[x, rows - 1] = null;
        }
    }

    /// <summary>
    /// Cộng điểm và cộng số hàng sau khi xóa hàng.
    /// Hiện tại mỗi hàng được 100 điểm.
    /// </summary>
    private void AddScoreAndLines(int clearedLines)
    {
        lines += clearedLines;
        score += clearedLines * 100;
        UpdateUI();
    }

    /// <summary>
    /// Cập nhật điểm và số hàng lên giao diện chính.
    /// </summary>
    private void UpdateUI()
    {
        if (scoreValueText != null)
            scoreValueText.text = score.ToString();

        if (linesValueText != null)
            linesValueText.text = lines.ToString();
    }

    /// <summary>
    /// Hiển thị bảng Game Over và cập nhật điểm cuối cùng lên bảng.
    /// </summary>
    private void ShowGameOverUI()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (finalScoreValueText != null)
            finalScoreValueText.text = score.ToString();

        if (finalLinesValueText != null)
            finalLinesValueText.text = lines.ToString();
    }

    /// <summary>
    /// Ẩn bảng Game Over.
    /// </summary>
    private void HideGameOverUI()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    /// <summary>
    /// Đặt một ô gạch vào đúng vị trí x, y trên bàn chơi UI.
    /// Hàm này chuyển tọa độ bàn chơi thành tọa độ RectTransform.
    /// </summary>
    private void PositionCell(RectTransform cellRect, int x, int y)
    {
        // Lấy kích thước ô từ GridLayoutGroup của lưới nền
        float cellWidth = backgroundGridLayout.cellSize.x;
        float cellHeight = backgroundGridLayout.cellSize.y;

        // Tính khoảng cách giữa các ô
        float stepX = cellWidth + backgroundGridLayout.spacing.x;
        float stepY = cellHeight + backgroundGridLayout.spacing.y;

        // Lấy padding bên trái và bên trên
        int padLeft = backgroundGridLayout.padding.left;
        int padTop = backgroundGridLayout.padding.top;

        // Đặt anchor về góc trên bên trái để dễ tính vị trí
        cellRect.anchorMin = new Vector2(0f, 1f);
        cellRect.anchorMax = new Vector2(0f, 1f);
        cellRect.pivot = new Vector2(0f, 1f);

        // Reset scale và rotation
        cellRect.localScale = Vector3.one;
        cellRect.localRotation = Quaternion.identity;

        // Đặt kích thước ô bằng đúng kích thước ô nền
        cellRect.sizeDelta = new Vector2(cellWidth, cellHeight);

        // Tính vị trí X theo cột
        float posX = padLeft + x * stepX;

        // Tính vị trí Y theo hàng
        // Do UI tính từ trên xuống, còn bàn chơi tính y từ dưới lên nên phải đảo y
        float posY = -(padTop + (rows - 1 - y) * stepY);

        // Gán vị trí cho ô gạch
        cellRect.anchoredPosition = new Vector2(posX, posY);
    }

    /// <summary>
    /// Xóa toàn bộ hình ảnh của khối đang rơi.
    /// Dùng khi khối đã bị khóa hoặc khi cần tạo khối mới.
    /// </summary>
    private void ClearActivePieceVisual()
    {
        for (int i = 0; i < activeCellImages.Count; i++)
        {
            if (activeCellImages[i] != null)
            {
                Destroy(activeCellImages[i].gameObject);
            }
        }

        activeCellImages.Clear();
    }

    /// <summary>
    /// Hàm public để gắn vào nút CHƠI LẠI trong Unity Button On Click().
    /// Khi người chơi bấm nút, game sẽ được reset.
    /// </summary>
    public void RestartGame()
    {
        RestartBoard();
    }

    /// <summary>
    /// Reset toàn bộ bàn chơi về trạng thái ban đầu:
    /// - Tắt Game Over
    /// - Đưa điểm và số hàng về 0
    /// - Xóa toàn bộ gạch đã rơi
    /// - Xóa khối đang rơi
    /// - Tạo khối mới
    /// </summary>
    private void RestartBoard()
    {
        // Đặt lại trạng thái game
        isGameOver = false;
        fallTimer = 0f;
        score = 0;
        lines = 0;

        // Cập nhật lại giao diện và ẩn bảng Game Over
        UpdateUI();
        HideGameOverUI();

        // Xóa toàn bộ dữ liệu và hình ảnh các ô gạch đã khóa
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                occupied[x, y] = false;

                if (lockedCellImages[x, y] != null)
                {
                    Destroy(lockedCellImages[x, y].gameObject);
                    lockedCellImages[x, y] = null;
                }
            }
        }

        // Xóa khối đang rơi nếu còn
        ClearActivePieceVisual();

        // Tạo khối mới để bắt đầu lại
        SpawnRandomPiece();
    }
}
