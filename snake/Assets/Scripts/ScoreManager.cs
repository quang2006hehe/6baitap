using UnityEngine;
using UnityEngine.UI; // Dành cho hệ thống UI Text truyền thống của Unity

/// <summary>
/// Trình quản lý điểm số (ScoreManager) lưu trữ điểm hiện tại và điểm kỷ lục (High Score).
/// Sử dụng PlayerPrefs để lưu điểm kỷ lục trên ổ đĩa cứng của thiết bị.
/// Hỗ trợ vẽ điểm số retro trực tiếp lên màn hình qua OnGUI (với bóng đổ chuyên nghiệp) 
/// và hỗ trợ gán các đối tượng UI Text nếu người dùng muốn kéo thả trong Inspector.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    private static ScoreManager _instance;

    /// <summary>
    /// Thuộc tính Singleton để dễ dàng truy cập ScoreManager từ bất kỳ kịch bản nào khác (ví dụ: Snake).
    /// </summary>
    public static ScoreManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Tìm xem đã có ScoreManager nào tồn tại trong Scene chưa
                _instance = FindObjectOfType<ScoreManager>();
                if (_instance == null)
                {
                    // Nếu chưa, tự động tạo mới một GameObject và thêm thành phần này vào
                    GameObject go = new GameObject("ScoreManager (Tự động tạo)");
                    _instance = go.AddComponent<ScoreManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Điểm số hiện tại của người chơi (chỉ cho phép đọc từ ngoài, thay đổi từ trong lớp này).
    /// </summary>
    public int Score { get; private set; }

    /// <summary>
    /// Điểm số cao nhất từ trước đến nay (được lưu trữ trên ổ đĩa).
    /// </summary>
    public int HighScore { get; private set; }

    [Header("Giao diện UI tùy chọn (Kéo thả UI Text từ Canvas vào đây nếu muốn)")]
    [Tooltip("Thành phần UI Text hiển thị Điểm hiện tại")]
    public Text scoreText;

    [Tooltip("Thành phần UI Text hiển thị Điểm kỷ lục")]
    public Text highScoreText;

    // Đối tượng quả bom đỏ được tạo khi đạt 10 điểm
    private GameObject bombInstance;

    private void Awake()
    {
        // Thiết lập cấu trúc Singleton bảo vệ chống trùng lặp thực thể
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Tải điểm kỷ lục đã lưu từ PlayerPrefs (mặc định là 0 nếu chưa từng chơi)
        HighScore = PlayerPrefs.GetInt("HighScore", 0);
        ResetScore();
    }

    /// <summary>
    /// Cộng thêm một số điểm nhất định và cập nhật kỷ lục mới nếu có.
    /// </summary>
    /// <param name="amount">Số điểm cộng thêm</param>
    public void AddScore(int amount)
    {
        Score += amount;

        // Nếu điểm hiện tại vượt qua điểm kỷ lục cũ
        if (Score > HighScore)
        {
            HighScore = Score;
            // Lưu lại điểm kỷ lục mới vào bộ nhớ thiết bị
            PlayerPrefs.SetInt("HighScore", HighScore);
            PlayerPrefs.Save();
        }

        UpdateUI();
        CheckBombSpawn(); // Kiểm tra tạo hoặc hủy bom khi điểm thay đổi
    }

    /// <summary>
    /// Đặt lại điểm số hiện tại về 0 (thường gọi khi trò chơi bắt đầu hoặc khi rắn chết).
    /// </summary>
    public void ResetScore()
    {
        Score = 0;
        UpdateUI();
        CheckBombSpawn(); // Hủy quả bom đỏ khi chơi lại từ đầu (điểm về 0)
    }

    /// <summary>
    /// Cập nhật nội dung văn bản cho các thành phần UI Text (nếu người dùng có kéo thả gán đối tượng).
    /// </summary>
    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "SCORE: " + Score.ToString("D4"); // Định dạng hiển thị 4 chữ số (ví dụ: 0010)
        }
        if (highScoreText != null)
        {
            highScoreText.text = "HIGH SCORE: " + HighScore.ToString("D4");
        }
    }

    /// <summary>
    /// Phương thức vẽ văn bản có hiệu ứng bóng đổ (Drop Shadow) để tạo giao diện retro cao cấp,
    /// giúp chữ nổi bật và dễ đọc trên mọi nền màu của trò chơi.
    /// </summary>
    private void DrawTextWithShadow(Rect rect, string text, GUIStyle style, Color textColor, Color shadowColor)
    {
        // 1. Vẽ bóng đổ màu tối dịch chuyển đi một chút
        style.normal.textColor = shadowColor;
        GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, style);

        // 2. Vẽ chữ chính đè lên trên bóng đổ
        style.normal.textColor = textColor;
        GUI.Label(rect, text, style);
    }

    private void OnGUI()
    {
        // Định dạng kiểu chữ chung - Sử dụng GUI.skin.label làm cơ sở để tránh lỗi NullReference của Unity
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 18; // Thu nhỏ cỡ chữ xuống 18 để tinh tế hơn
        textStyle.fontStyle = FontStyle.Bold; // Chữ in đậm retro

        // Điểm kỷ lục ở dòng trên cùng bên trái
        Rect highScoreRect = new Rect(20, 20, 300, 25);
        DrawTextWithShadow(highScoreRect, "HI-SCORE: " + HighScore.ToString("D4"), textStyle, Color.yellow, Color.black);

        // Điểm số hiện tại ở dòng ngay bên dưới
        Rect scoreRect = new Rect(20, 45, 300, 25);
        DrawTextWithShadow(scoreRect, "SCORE: " + Score.ToString("D4"), textStyle, Color.green, Color.black);
    }

    /// <summary>
    /// Kiểm tra trạng thái điểm số để tạo bom (khi điểm >= 10) hoặc hủy bom (khi điểm < 10).
    /// </summary>
    private void CheckBombSpawn()
    {
        if (Score >= 10)
        {
            if (bombInstance == null)
            {
                SpawnBomb();
            }
        }
        else
        {
            if (bombInstance != null)
            {
                Destroy(bombInstance);
                bombInstance = null;
            }
        }
    }

    /// <summary>
    /// Tạo đối tượng bom đỏ dạng retro 1x1 ô lưới và gán tag Obstacle để xử lý va chạm chết.
    /// </summary>
    private void SpawnBomb()
    {
        // 1. Khởi tạo đối tượng bom và gán nhãn Obstacle giống thân rắn
        bombInstance = new GameObject("RedBomb");
        bombInstance.tag = "Obstacle";

        // 2. Thêm SpriteRenderer và tự vẽ texture màu đỏ viền đen kiểu 8-bit
        SpriteRenderer sr = bombInstance.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(16, 16);
        Color[] colors = new Color[16 * 16];
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                if (x == 0 || x == 15 || y == 0 || y == 15)
                {
                    colors[y * 16 + x] = Color.black; // Viền đen ngoài
                }
                else
                {
                    colors[y * 16 + x] = Color.red; // Ruột màu đỏ
                }
            }
        }
        tex.SetPixels(colors);
        tex.Apply();
        
        // Thiết lập 16 pixel tương đương 1 đơn vị thế giới để vừa khít lưới 1x1 ô vuông
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        sr.sortingOrder = 5; // Hiển thị đè lên trên

        // 3. Thêm BoxCollider2D dạng Trigger để nhận diện va chạm
        BoxCollider2D collider = bombInstance.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        // 4. Chọn vị trí ngẫu nhiên tránh đè lên rắn và thức ăn
        RandomizeBombPosition();
    }

    /// <summary>
    /// Đặt vị trí ngẫu nhiên cho quả bom đỏ trong phạm vi sân chơi.
    /// </summary>
    private void RandomizeBombPosition()
    {
        Food food = FindObjectOfType<Food>();
        Snake snake = FindObjectOfType<Snake>();

        if (food != null && snake != null && food.gridArea != null)
        {
            Bounds bounds = food.gridArea.bounds;
            
            int x = Mathf.RoundToInt(Random.Range(bounds.min.x, bounds.max.x));
            int y = Mathf.RoundToInt(Random.Range(bounds.min.y, bounds.max.y));

            // Thử tối đa 100 lần để tìm một ô trống không có rắn hay thức ăn
            int attempts = 0;
            while ((snake.Occupies(x, y) || 
                   (Mathf.RoundToInt(food.transform.position.x) == x && Mathf.RoundToInt(food.transform.position.y) == y)) 
                   && attempts < 100)
            {
                x = Mathf.RoundToInt(Random.Range(bounds.min.x, bounds.max.x));
                y = Mathf.RoundToInt(Random.Range(bounds.min.y, bounds.max.y));
                attempts++;
            }

            bombInstance.transform.position = new Vector2(x, y);
        }
    }
}
