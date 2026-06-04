using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Snake : MonoBehaviour
{
    public Transform segmentPrefab;
    public Vector2Int direction = Vector2Int.right;
    public float speed = 10f; // Giảm tốc độ cơ bản mặc định từ 20 xuống 10 để game khởi động chậm dễ điều khiển
    public float speedMultiplier = 1f;
    public int initialSize = 4;
    public bool moveThroughWalls = false;

    // Các trạng thái của trò chơi
    public enum GameState
    {
        GetReady, // Trạng thái chuẩn bị, chờ người chơi bấm phím
        Playing,  // Trạng thái đang chơi, rắn di chuyển tự động
        GameOver  // Trạng thái game over, rắn dừng lại tại chỗ va chạm
    }

    [Header("Trạng thái game hiện tại")]
    public GameState gameState = GameState.GetReady;

    [Header("Điều khiển trên điện thoại")]
    [Tooltip("Khoảng cách vuốt tối thiểu (pixel) để nhận diện hướng vuốt")]
    public float minSwipeDistance = 40f;
    private Vector2 touchStartPos;
    private bool swipeDetected;

    private readonly List<Transform> segments = new List<Transform>();
    private Vector2Int input;
    private float nextUpdate;

    private void Start()
    {
        ResetState();
    }

    private void Update()
    {
        // 1. Chế độ Get Ready: Chờ người chơi bấm phím hoặc chạm màn hình để bắt đầu chạy rắn
        if (gameState == GameState.GetReady)
        {
            if (Input.anyKeyDown || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) || Input.GetMouseButtonDown(0))
            {
                gameState = GameState.Playing;
                input = direction; // Thiết lập hướng đi ban đầu theo hướng định sẵn
            }
            return;
        }

        // 2. Chế độ Game Over: Chờ người chơi bấm Space hoặc Enter hoặc chạm màn hình để chơi lại từ đầu
        if (gameState == GameState.GameOver)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) || Input.GetMouseButtonDown(0))
            {
                ResetState();
            }
            return;
        }

        // 3. Chế độ đang chơi (Playing): Nhận phím đổi hướng di chuyển
        // Chỉ cho phép quay lên hoặc xuống khi đang di chuyển theo trục X
        if (direction.x != 0f)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) {
                input = Vector2Int.up;
            } else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) {
                input = Vector2Int.down;
            }
        }
        // Chỉ cho phép quay trái hoặc phải khi đang di chuyển theo trục Y
        else if (direction.y != 0f)
        {
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) {
                input = Vector2Int.right;
            } else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) {
                input = Vector2Int.left;
            }
        }

        // Nhận diện vuốt màn hình trên điện thoại hoặc chuột giả lập trên PC/Editor
        HandleSwipeInput();
    }

    /// <summary>
    /// Nhận diện thao tác vuốt trên điện thoại di động hoặc kéo thả chuột trên Editor.
    /// </summary>
    private void HandleSwipeInput()
    {
        // Nhận diện chạm cảm ứng trên thiết bị di động
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.position;
                swipeDetected = false;
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Ended)
            {
                if (!swipeDetected)
                {
                    Vector2 diff = touch.position - touchStartPos;
                    if (diff.magnitude > minSwipeDistance)
                    {
                        ProcessSwipe(diff);
                        swipeDetected = true;
                    }
                }
            }
        }
        // Nhận diện bằng chuột trái để người dùng dễ dàng test trên Unity Editor
        else if (Input.GetMouseButtonDown(0))
        {
            touchStartPos = Input.mousePosition;
            swipeDetected = false;
        }
        else if (Input.GetMouseButton(0))
        {
            if (!swipeDetected)
            {
                Vector2 diff = (Vector2)Input.mousePosition - touchStartPos;
                if (diff.magnitude > minSwipeDistance)
                {
                    ProcessSwipe(diff);
                    swipeDetected = true;
                }
            }
        }
    }

    /// <summary>
    /// Tính toán hướng vuốt và thử đổi hướng đi.
    /// </summary>
    private void ProcessSwipe(Vector2 diff)
    {
        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
        {
            // Vuốt ngang
            if (diff.x > 0) {
                TrySetDirection(Vector2Int.right);
            } else {
                TrySetDirection(Vector2Int.left);
            }
        }
        else
        {
            // Vuốt dọc
            if (diff.y > 0) {
                TrySetDirection(Vector2Int.up);
            } else {
                TrySetDirection(Vector2Int.down);
            }
        }
    }

    /// <summary>
    /// Thay đổi hướng đi tạm thời nếu hướng mới hợp lệ (vuông góc với hướng đang di chuyển).
    /// </summary>
    private void TrySetDirection(Vector2Int newDir)
    {
        if (direction.x != 0f)
        {
            if (newDir.y != 0) {
                input = newDir;
            }
        }
        else if (direction.y != 0f)
        {
            if (newDir.x != 0) {
                input = newDir;
            }
        }
    }

    private void FixedUpdate()
    {
        // Rắn sẽ đứng yên và không di chuyển nếu không ở trạng thái Playing
        if (gameState != GameState.Playing) {
            return;
        }

        // Tự động tăng tốc độ dần theo điểm số (mỗi điểm tăng 5% tốc độ)
        if (ScoreManager.Instance != null) {
            speedMultiplier = 1f + (ScoreManager.Instance.Score * 0.05f);
        }

        // Wait until the next update before proceeding
        if (Time.time < nextUpdate) {
            return;
        }

        // Set the new direction based on the input
        if (input != Vector2Int.zero) {
            direction = input;
        }

        // Set each segment's position to be the same as the one it follows. We
        // must do this in reverse order so the position is set to the previous
        // position, otherwise they will all be stacked on top of each other.
        for (int i = segments.Count - 1; i > 0; i--) {
            segments[i].position = segments[i - 1].position;
        }

        // Move the snake in the direction it is facing
        // Round the values to ensure it aligns to the grid
        int x = Mathf.RoundToInt(transform.position.x) + direction.x;
        int y = Mathf.RoundToInt(transform.position.y) + direction.y;
        transform.position = new Vector2(x, y);

        // Set the next update time based on the speed
        nextUpdate = Time.time + (1f / (speed * speedMultiplier));
    }

    public void Grow()
    {
        Transform segment = Instantiate(segmentPrefab);
        segment.position = segments[segments.Count - 1].position;
        segments.Add(segment);
    }

    public void ResetState()
    {
        // Đặt lại điểm số về 0 mỗi khi trò chơi bắt đầu hoặc khi rắn chết và hồi sinh
        if (ScoreManager.Instance != null) {
            ScoreManager.Instance.ResetScore();
        }

        // Đưa game về trạng thái chuẩn bị chơi khi reset
        gameState = GameState.GetReady;
        input = Vector2Int.zero; // Reset hướng đi tạm thời để tránh tự chạy lúc GetReady

        direction = Vector2Int.right;
        transform.position = Vector3.zero;

        // Start at 1 to skip destroying the head
        for (int i = 1; i < segments.Count; i++) {
            Destroy(segments[i].gameObject);
        }

        // Clear the list but add back this as the head
        segments.Clear();
        segments.Add(transform);

        // -1 since the head is already in the list
        for (int i = 0; i < initialSize - 1; i++) {
            Grow();
        }
    }

    public bool Occupies(int x, int y)
    {
        foreach (Transform segment in segments)
        {
            if (Mathf.RoundToInt(segment.position.x) == x &&
                Mathf.RoundToInt(segment.position.y) == y) {
                return true;
            }
        }

        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Chỉ xử lý va chạm khi đang chơi game
        if (gameState != GameState.Playing) {
            return;
        }

        // Khi đầu rắn chạm vào thức ăn (đối tượng có tag "Food")
        if (other.gameObject.CompareTag("Food"))
        {
            Grow(); // Rắn dài thêm 1 đốt
            AudioManager.Instance.PlayEatSound(); // Phát hiệu ứng âm thanh ăn mồi
            ScoreManager.Instance.AddScore(1); // Cộng thêm 1 điểm vào điểm số hiện tại
        }
        // Khi đầu rắn chạm vào chướng ngại vật (đối tượng có tag "Obstacle" - ví dụ: thân rắn)
        else if (other.gameObject.CompareTag("Obstacle"))
        {
            Die(); // Xử lý Game Over khi tự đâm vào đuôi
        }
        // Khi đầu rắn va chạm với tường (đối tượng có tag "Wall")
        else if (other.gameObject.CompareTag("Wall"))
        {
            if (moveThroughWalls) {
                Traverse(other.transform); // Đi xuyên qua tường sang phía bên kia
            } else {
                Die(); // Xử lý Game Over khi đâm vào tường
            }
        }
    }

    /// <summary>
    /// Xử lý chuyển sang trạng thái Game Over, dừng mọi di chuyển và phát hiệu ứng âm thanh.
    /// </summary>
    private void Die()
    {
        gameState = GameState.GameOver; // Chuyển đổi trạng thái sang kết thúc game
        AudioManager.Instance.PlayDieSound(); // Phát âm thanh khi chết
    }

    /// <summary>
    /// Vẽ giao diện văn bản "GET READY!" và "GAME OVER" có bóng đổ giữa màn hình bằng GUI.
    /// </summary>
    private void OnGUI()
    {
        // Cấu hình kiểu chữ cho Tiêu đề chính - Sử dụng GUI.skin.label làm cơ sở
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 40;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;

        // Cấu hình kiểu chữ cho Hướng dẫn phụ - Sử dụng GUI.skin.label làm cơ sở
        GUIStyle subStyle = new GUIStyle(GUI.skin.label);
        subStyle.fontSize = 18;
        subStyle.fontStyle = FontStyle.Normal;
        subStyle.alignment = TextAnchor.MiddleCenter;

        if (gameState == GameState.GetReady)
        {
            // Vẽ bóng đổ chữ màu đen phía sau
            titleStyle.normal.textColor = Color.black;
            GUI.Label(new Rect(2, 2, Screen.width, Screen.height - 40), "GET READY!", titleStyle);

            // Vẽ tiêu đề chữ màu Cyan (xanh ngọc) phía trước
            titleStyle.normal.textColor = Color.cyan;
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height - 40), "GET READY!", titleStyle);

            // Hướng dẫn phụ bóng đổ
            subStyle.normal.textColor = Color.black;
            GUI.Label(new Rect(2, 62, Screen.width, Screen.height - 40), "Nhấn phím bất kỳ hoặc chạm để di chuyển", subStyle);

            // Hướng dẫn phụ màu trắng
            subStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(0, 60, Screen.width, Screen.height - 40), "Nhấn phím bất kỳ hoặc chạm để di chuyển", subStyle);
        }
        else if (gameState == GameState.GameOver)
        {
            titleStyle.fontSize = 48; // Làm chữ Game Over to hơn một chút

            // Vẽ bóng đổ chữ Game Over màu đen
            titleStyle.normal.textColor = Color.black;
            GUI.Label(new Rect(2, 2, Screen.width, Screen.height - 40), "GAME OVER", titleStyle);

            // Vẽ chữ Game Over chính màu Đỏ
            titleStyle.normal.textColor = Color.red;
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height - 40), "GAME OVER", titleStyle);

            // Hướng dẫn chơi lại bóng đổ
            subStyle.normal.textColor = Color.black;
            GUI.Label(new Rect(2, 62, Screen.width, Screen.height - 40), "Nhấn SPACE, ENTER hoặc chạm để chơi lại", subStyle);

            // Hướng dẫn chơi lại màu trắng
            subStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(0, 60, Screen.width, Screen.height - 40), "Nhấn SPACE, ENTER hoặc chạm để chơi lại", subStyle);
        }
    }

    private void Traverse(Transform wall)
    {
        Vector3 position = transform.position;

        if (direction.x != 0f) {
            position.x = Mathf.RoundToInt(-wall.position.x + direction.x);
        } else if (direction.y != 0f) {
            position.y = Mathf.RoundToInt(-wall.position.y + direction.y);
        }

        transform.position = position;
    }
}
