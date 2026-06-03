using UnityEngine;
using UnityEngine.UI;

public enum GameState
{
    Idle,    // Trạng thái nghỉ, chờ người chơi bấm
    Running, // Đang chơi
    Dead,    // Khủng long chết, hiện bảng Replay
    Paused   // Tạm dừng game
}

public class GameManager : MonoBehaviour
{
    // Singleton để các script khác dễ dàng gọi GameManager
    public static GameManager Instance { get; private set; }

    [Header("Game Speed Settings")]
    [SerializeField] private float initialSpeed = 8f;
    [SerializeField] private float maxSpeed = 22f;
    [SerializeField] private float speedIncreaseRate = 0.1f; // Tốc độ tăng thêm mỗi giây
    
    public float GameSpeed { get; private set; }

    [Header("Score Settings")]
    [SerializeField] private float scoreMultiplier = 10f; // Hệ số tính điểm dựa trên thời gian chạy
    private float scoreAmount;
    public int CurrentScore => Mathf.FloorToInt(scoreAmount);
    public int HighScore { get; private set; }

    [Header("UI Panels")]
    public GameObject idlePanel;        // Bảng chữ "CHẠM ĐỂ CHƠI" (MainMenuBoard)
    public GameObject deadPanel;        // Bảng điểm và nút "CHƠI LẠI" (GameOverBoard)
    public GameObject exitConfirmPanel; // Bảng Tạm dừng (ExitConfirmBoard)
    public GameObject hudPanel;         // Bảng điểm khi đang chạy (ScoreHUD)

    [Header("Score Sprite Settings")]
    public Sprite[] numberSprites; // Gán 10 ảnh từ 0 đến 9 vào đây trong Inspector

    public GameState currentGameState;
    private GameState previousGameState;

    // Các mảng Image để hiển thị chữ số
    private Image[] hudDigits = new Image[3];
    private Image[] hudHighDigits = new Image[3];
    private Image[] gameOverDigits = new Image[3];
    private Image[] gameOverHighDigits = new Image[3];
    private Image[] pauseDigits = new Image[3];
    private Image[] pauseHighDigits = new Image[3];
    private GameObject pauseButtonObj;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Tự động kiểm tra và tạo AudioManager trên GameObject riêng biệt
        // để không làm GameManager bị DontDestroyOnLoad khi chạy game
        if (AudioManager.Instance == null)
        {
#if UNITY_2023_1_OR_NEWER
            AudioManager existing = FindAnyObjectByType<AudioManager>();
#else
            AudioManager existing = FindObjectOfType<AudioManager>();
#endif
            if (existing == null)
            {
                GameObject audioManagerObj = new GameObject("AudioManager");
                audioManagerObj.AddComponent<AudioManager>();
                Debug.Log("[GameManager] Created dedicated AudioManager GameObject.");
            }
        }

        // Tự động kiểm tra và tạo EventSystem nếu bị thiếu trong Scene
        // (Nếu thiếu EventSystem, toàn bộ UI Canvas sẽ không thể click hay tương tác được)
        #if UNITY_2023_1_OR_NEWER
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        #else
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        #endif
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[GameManager] Automatically created missing EventSystem to enable UI clicking.");
        }

        // 1. Tự động liên kết các Panel UI nếu chưa kéo thả trong Inspector (hỗ trợ tìm cả panel ẩn dưới Canvas)
        GameObject mainCanvas = GameObject.Find("Canvas");
        if (mainCanvas != null)
        {
            if (idlePanel == null)
            {
                Transform trans = FindDeepChild(mainCanvas.transform, "MainMenuBoard");
                if (trans != null) idlePanel = trans.gameObject;
            }
            if (deadPanel == null)
            {
                Transform trans = FindDeepChild(mainCanvas.transform, "GameOverBoard");
                if (trans != null) deadPanel = trans.gameObject;
            }
            if (exitConfirmPanel == null)
            {
                Transform trans = FindDeepChild(mainCanvas.transform, "ExitConfirmBoard");
                if (trans != null) exitConfirmPanel = trans.gameObject;
            }
            if (hudPanel == null)
            {
                Transform trans = FindDeepChild(mainCanvas.transform, "ScoreHUD");
                if (trans != null) hudPanel = trans.gameObject;
            }
        }
        else
        {
            // Dự phòng nếu không tìm thấy Canvas
            if (idlePanel == null) idlePanel = GameObject.Find("MainMenuBoard");
            if (deadPanel == null) deadPanel = GameObject.Find("GameOverBoard");
            if (exitConfirmPanel == null) exitConfirmPanel = GameObject.Find("ExitConfirmBoard");
            if (hudPanel == null) hudPanel = GameObject.Find("ScoreHUD");
        }

        // 2. Tìm kiếm các UI Image của chữ số
        FindDigitImages();

        // 3. Tự động kết nối sự kiện nút Play (ở MainMenuBoard)
        if (idlePanel != null)
        {
            Transform playBtnTrans = FindDeepChild(idlePanel.transform, "Button");
            if (playBtnTrans != null)
            {
                Button playBtn = playBtnTrans.GetComponent<Button>();
                if (playBtn != null)
                {
                    playBtn.onClick.RemoveAllListeners();
                    playBtn.onClick.AddListener(() => ChangeState(GameState.Running));
                    Debug.Log($"[GameManager] Connected Play button in MainMenuBoard.");
                }
            }
        }

        // 4. Tự động kết nối các nút của bảng GameOverBoard (deadPanel)
        ConfigurePanelButtons(deadPanel);

        // 5. Tự động kết nối nút Pause (nút Button trực tiếp dưới Canvas)
        if (mainCanvas != null)
        {
            Transform pauseBtnTrans = mainCanvas.transform.Find("PauseButton");
            if (pauseBtnTrans == null) pauseBtnTrans = mainCanvas.transform.Find("Button"); // Tìm trực tiếp dưới Canvas, tránh tìm nhầm nút ở các panel con

            if (pauseBtnTrans != null)
            {
                pauseButtonObj = pauseBtnTrans.gameObject;
                Button pauseBtn = pauseBtnTrans.GetComponent<Button>();
                if (pauseBtn != null)
                {
                    pauseBtn.onClick.RemoveAllListeners();
                    pauseBtn.onClick.AddListener(PauseGame);
                    Debug.Log($"[GameManager] Connected PauseButton.");
                }
            }
        }

        // 6. Tự động kết nối các nút trong bảng Pause (ExitConfirmBoard)
        ConfigurePanelButtons(exitConfirmPanel);
    }

    private void Start()
    {
        // Tải điểm số cao nhất từ trước đó
        HighScore = PlayerPrefs.GetInt("DinoSakura_HighScore", 0);
        
        // Trạng thái ban đầu là Idle
        ChangeState(GameState.Idle);
    }

    private void Update()
    {
        // Nhấn chuột / chạm màn hình / phím Space để bắt đầu chơi khi ở màn hình chờ
        if (currentGameState == GameState.Idle)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                ChangeState(GameState.Running);
            }
            return;
        }

        if (currentGameState == GameState.Running)
        {
            // Tăng tốc độ game dần dần theo thời gian chạy
            GameSpeed = Mathf.Min(GameSpeed + speedIncreaseRate * Time.deltaTime, maxSpeed);

            // Tăng điểm dựa trên quãng đường/thời gian chạy và phát nhạc khi qua mốc 100 điểm
            float prevScoreAmount = scoreAmount;
            scoreAmount += scoreMultiplier * Time.deltaTime;
            UpdateScoreUI();

            int prevInt = Mathf.FloorToInt(prevScoreAmount);
            int currInt = Mathf.FloorToInt(scoreAmount);
            if (currInt > 0 && currInt / 100 > prevInt / 100)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayScore();
                }
            }
        }
    }

    public void ChangeState(GameState newState)
    {
        previousGameState = currentGameState;
        currentGameState = newState;

        switch (currentGameState)
        {
            case GameState.Idle:
                Time.timeScale = 0f; // Đóng băng mọi chuyển động
                GameSpeed = 0f;
                scoreAmount = 0f;
                
                if (idlePanel != null) idlePanel.SetActive(true);
                if (deadPanel != null) deadPanel.SetActive(false);
                if (exitConfirmPanel != null) exitConfirmPanel.SetActive(false);
                if (hudPanel != null) hudPanel.SetActive(false);
                if (pauseButtonObj != null) pauseButtonObj.SetActive(false);

                // Phát nhạc nền ở màn hình chờ
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayBGM();
                }
                break;

            case GameState.Running:
                Time.timeScale = 1f; // Cho phép game chạy bình thường

                if (previousGameState == GameState.Idle || previousGameState == GameState.Dead)
                {
                    GameSpeed = initialSpeed;
                    scoreAmount = 0f;
                    
                    // Reset khủng long về vị trí chạy
                    if (DinoController.Instance != null)
                    {
                        DinoController.Instance.TransitionToState(DinoState.Running);
                    }
                }
                
                if (idlePanel != null) idlePanel.SetActive(false);
                if (deadPanel != null) deadPanel.SetActive(false);
                if (exitConfirmPanel != null) exitConfirmPanel.SetActive(false);
                if (hudPanel != null) hudPanel.SetActive(true);
                if (pauseButtonObj != null) pauseButtonObj.SetActive(true);
                
                UpdateScoreUI();

                // Phát/Tiếp tục phát nhạc nền
                if (AudioManager.Instance != null)
                {
                    if (previousGameState == GameState.Paused)
                    {
                        AudioManager.Instance.ResumeBGM();
                    }
                    else
                    {
                        AudioManager.Instance.PlayBGM();
                    }
                }
                break;

            case GameState.Dead:
                Time.timeScale = 0f; // Dừng mọi chuyển động
                GameSpeed = 0f;

                // Kiểm tra và cập nhật điểm cao nhất
                int finalScore = CurrentScore;
                if (finalScore > HighScore)
                {
                    HighScore = finalScore;
                    PlayerPrefs.SetInt("DinoSakura_HighScore", HighScore);
                    PlayerPrefs.Save();
                }

                if (idlePanel != null) idlePanel.SetActive(false);
                if (deadPanel != null) deadPanel.SetActive(true);
                if (exitConfirmPanel != null) exitConfirmPanel.SetActive(false);
                if (hudPanel != null) hudPanel.SetActive(false);
                if (pauseButtonObj != null) pauseButtonObj.SetActive(false);

                // Đồng bộ điểm lên màn hình kết thúc bằng Sprite
                SetScoreSprites(gameOverDigits, finalScore);
                SetScoreSprites(gameOverHighDigits, HighScore);

                if (DinoController.Instance != null)
                {
                    DinoController.Instance.TransitionToState(DinoState.Dead);
                }

                // Dừng nhạc nền và phát âm thanh chết
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopBGM();
                    AudioManager.Instance.PlayDeath();
                }
                break;

            case GameState.Paused:
                Time.timeScale = 0f; // Dừng chuyển động vật lý

                if (idlePanel != null) idlePanel.SetActive(false);
                if (deadPanel != null) deadPanel.SetActive(false);
                if (exitConfirmPanel != null) exitConfirmPanel.SetActive(true);
                if (hudPanel != null) hudPanel.SetActive(false);
                if (pauseButtonObj != null) pauseButtonObj.SetActive(false);

                // Đồng bộ điểm lên màn hình Tạm dừng bằng Sprite
                SetScoreSprites(pauseDigits, CurrentScore);
                SetScoreSprites(pauseHighDigits, HighScore);

                // Tạm ngưng nhạc nền khi pause game
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PauseBGM();
                }
                break;
        }
    }

    private void FindDigitImages()
    {
        // 1. Tìm trong ScoreHUD
        if (hudPanel != null)
        {
            FindDigitsInPanel(hudPanel, hudDigits, hudHighDigits, "Digit_");
        }

        // 2. Tìm trong GameOverBoard (deadPanel)
        if (deadPanel != null)
        {
            FindDigitsInPanel(deadPanel, gameOverDigits, gameOverHighDigits, "GO_Digit_");
        }

        // 3. Tìm trong ExitConfirmBoard (exitConfirmPanel)
        if (exitConfirmPanel != null)
        {
            FindDigitsInPanel(exitConfirmPanel, pauseDigits, pauseHighDigits, "Pause_Digit_");
        }
    }

    private void SetScoreSprites(Image[] digits, int score)
    {
        if (digits == null || digits.Length == 0 || numberSprites == null || numberSprites.Length < 10)
        {
            return;
        }

        // Giới hạn điểm số tối đa dựa vào số lượng chữ số (ví dụ: 3 chữ số là 999)
        int maxVal = Mathf.RoundToInt(Mathf.Pow(10, digits.Length)) - 1;
        int clampedScore = Mathf.Clamp(score, 0, maxVal);

        for (int i = 0; i < digits.Length; i++)
        {
            if (digits[i] != null)
            {
                // Tính chữ số từ trái qua phải (i = 0 là Hàng trăm, i = 1 là Hàng chục, i = 2 là Hàng đơn vị)
                int divisor = Mathf.RoundToInt(Mathf.Pow(10, digits.Length - 1 - i));
                int digitValue = (clampedScore / divisor) % 10;

                digits[i].sprite = numberSprites[digitValue];
            }
        }
    }

    private void UpdateScoreUI()
    {
        SetScoreSprites(hudDigits, CurrentScore);
        
        // Nếu điểm hiện tại vượt qua điểm cao nhất, hiển thị điểm cao nhất chạy theo điểm hiện tại thời gian thực
        int displayHighScore = Mathf.Max(CurrentScore, HighScore);
        SetScoreSprites(hudHighDigits, displayHighScore);
    }

    // Hàm gắn vào nút Pause
    public void PauseGame()
    {
        if (currentGameState == GameState.Running)
        {
            ChangeState(GameState.Paused);
        }
    }

    // Hàm gắn vào nút Resume
    public void ResumeGame()
    {
        if (currentGameState == GameState.Paused)
        {
            ChangeState(GameState.Running);
        }
    }

    // Hàm gắn vào nút "CHƠI LẠI" (Replay Button) trên UI
    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    // Hàm để thoát game
    public void ExitGame()
    {
        Debug.Log("Thoát game!");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    // Tìm kiếm Transform con ở mọi cấp độ phân cấp
    private Transform FindDeepChild(Transform parent, string name)
    {
        Transform result = parent.Find(name);
        if (result != null) return result;

        foreach (Transform child in parent)
        {
            result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Tự động tìm và gán numberSprites từ ảnh numbers_0_9_transparent nếu chưa gán trong Editor
        if (numberSprites == null || numberSprites.Length < 10)
        {
            string path = "Assets/ảnh/numbers_0_9_transparent.png";
            Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets != null && assets.Length > 1)
            {
                var spritesList = new System.Collections.Generic.List<Sprite>();
                foreach (var asset in assets)
                {
                    if (asset is Sprite sprite)
                    {
                        spritesList.Add(sprite);
                    }
                }

                if (spritesList.Count >= 10)
                {
                    // Sắp xếp các sprite theo thứ tự số tăng dần (0 đến 9)
                    spritesList.Sort((a, b) => {
                        string numA = System.Text.RegularExpressions.Regex.Match(a.name, @"\d+$").Value;
                        string numB = System.Text.RegularExpressions.Regex.Match(b.name, @"\d+$").Value;
                        if (int.TryParse(numA, out int valA) && int.TryParse(numB, out int valB))
                        {
                            return valA.CompareTo(valB);
                        }
                        return string.Compare(a.name, b.name);
                    });

                    numberSprites = spritesList.ToArray();
                    Debug.Log($"[GameManager] Automatically loaded {numberSprites.Length} number sprites from {path}");
                }
            }
        }
    }
#endif

    // Tự động phân loại và liên kết chức năng cho nút dựa trên nội dung hiển thị hoặc tên gọi
    private void ConfigurePanelButtons(GameObject panel)
    {
        if (panel == null) return;

        Button[] buttons = panel.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            string type = GetButtonType(btn);
            btn.onClick.RemoveAllListeners();

            if (type == "exit")
            {
                btn.onClick.AddListener(ExitGame);
                Debug.Log($"[GameManager] Connected button {btn.gameObject.name} in {panel.name} to ExitGame.");
            }
            else if (type == "restart")
            {
                btn.onClick.AddListener(RestartGame);
                Debug.Log($"[GameManager] Connected button {btn.gameObject.name} in {panel.name} to RestartGame.");
            }
            else if (type == "resume")
            {
                btn.onClick.AddListener(ResumeGame);
                Debug.Log($"[GameManager] Connected button {btn.gameObject.name} in {panel.name} to ResumeGame.");
            }
            else
            {
                // Dự phòng nếu không phân loại được bằng nội dung
                string name = btn.gameObject.name.ToLower();
                if (name.Contains("exit") || name.Contains("quit"))
                {
                    btn.onClick.AddListener(ExitGame);
                }
                else if (name.Contains("restart") || name.Contains("replay"))
                {
                    btn.onClick.AddListener(RestartGame);
                }
                else
                {
                    if (panel == exitConfirmPanel)
                    {
                        btn.onClick.AddListener(ResumeGame);
                    }
                    else
                    {
                        btn.onClick.AddListener(RestartGame);
                    }
                }
            }
        }
    }

    private string GetButtonType(Button button)
    {
        if (button == null) return "";

        System.Text.StringBuilder visibleContent = new System.Text.StringBuilder();

        // 1. Kiểm tra Sprite Name của toàn bộ Image trên nút và các con (đề phòng ảnh đặt ở con)
        Image[] images = button.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img != null && img.sprite != null)
            {
                visibleContent.Append(img.sprite.name.ToLower() + " ");
            }
        }

        // 2. Kiểm tra TextMeshProUGUI
        var tmp = button.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (tmp != null)
        {
            visibleContent.Append(tmp.text.ToLower() + " ");
        }

        // 3. Kiểm tra Text thường
        var txt = button.GetComponentInChildren<Text>(true);
        if (txt != null)
        {
            visibleContent.Append(txt.text.ToLower() + " ");
        }

        string visibleStr = visibleContent.ToString();

        // Kiểm tra sprite đặc tả từ bảng thoát thoat_tro_choi_xoa_nen_v2 để phân biệt nút Chơi tiếp và Thoát
        if (visibleStr.Contains("thoat_tro_choi_xoa_nen_v2_2"))
        {
            return "resume";
        }
        if (visibleStr.Contains("thoat_tro_choi_xoa_nen_v2_1"))
        {
            return "exit";
        }

        // Kiểm tra từ khóa hiển thị (Ưu tiên cao nhất, hỗ trợ cả tiếng Việt có dấu và không dấu)
        if (visibleStr.Contains("thoat") || visibleStr.Contains("thoát") || visibleStr.Contains("exit") || visibleStr.Contains("quit"))
        {
            return "exit";
        }
        if (visibleStr.Contains("lai") || visibleStr.Contains("lại") || visibleStr.Contains("restart") || visibleStr.Contains("replay"))
        {
            return "restart";
        }
        if (visibleStr.Contains("tiep") || visibleStr.Contains("tiếp") || visibleStr.Contains("resume") || visibleStr.Contains("continue") || visibleStr.Contains("play"))
        {
            return "resume";
        }

        // Dự phòng bằng tên GameObject trong Scene (nếu phần hiển thị không chứa từ khóa)
        string goName = button.gameObject.name.ToLower();
        if (goName.Contains("exit") || goName.Contains("quit") || goName.Contains("thoat") || goName.Contains("thoát"))
        {
            return "exit";
        }
        if (goName.Contains("restart") || goName.Contains("replay") || goName.Contains("lai") || goName.Contains("lại"))
        {
            return "restart";
        }
        if (goName.Contains("resume") || goName.Contains("continue") || goName.Contains("play") || goName.Contains("tiep") || goName.Contains("tiếp"))
        {
            return "resume";
        }

        return "";
    }

    // Tự động tìm kiếm và phân loại các chữ số UI dưới Panel
    private void FindDigitsInPanel(GameObject panel, Image[] currentDigits, Image[] highDigits, string defaultPrefix)
    {
        if (panel == null) return;

        // Tìm tất cả Image dưới panel (kể cả bị ẩn)
        Image[] allImages = panel.GetComponentsInChildren<Image>(true);
        
        // Tạo danh sách tạm
        var currentList = new System.Collections.Generic.List<Image>();
        var highList = new System.Collections.Generic.List<Image>();

        foreach (var img in allImages)
        {
            if (img == null) continue;
            string name = img.gameObject.name.ToLower();
            string parentName = img.transform.parent != null ? img.transform.parent.name.ToLower() : "";

            // Chỉ lấy các đối tượng có tên chứa "digit", "num", "số", "so"
            if (name.Contains("digit") || name.Contains("num") || name.Contains("số") || name.Contains("so"))
            {
                // Kiểm tra xem là thuộc nhóm Điểm cao hay Điểm hiện tại
                bool isHigh = name.Contains("high") || name.Contains("hi") || name.Contains("kỷ lục") || name.Contains("kỷlục") || name.Contains("ky luc") || name.Contains("cao") ||
                             parentName.Contains("high") || parentName.Contains("hi") || parentName.Contains("kỷ lục") || parentName.Contains("kỷlục") || parentName.Contains("ky luc") || parentName.Contains("cao");
                
                // Kiểm tra chỉ số số cuối cùng trong tên (Ví dụ: Digit_4 -> 4 là điểm cao)
                string numStr = System.Text.RegularExpressions.Regex.Match(name, @"\d+$").Value;
                if (int.TryParse(numStr, out int numVal))
                {
                    if (numVal >= 4 && numVal <= 6)
                    {
                        isHigh = true;
                    }
                }

                if (isHigh)
                {
                    highList.Add(img);
                }
                else
                {
                    currentList.Add(img);
                }
            }
        }

        // Sắp xếp các danh sách theo số cuối trong tên hoặc theo thứ tự X tăng dần (từ trái qua phải)
        SortDigits(currentList);
        SortDigits(highList);

        // Gán vào mảng kết quả
        for (int i = 0; i < currentDigits.Length; i++)
        {
            if (i < currentList.Count) currentDigits[i] = currentList[i];
            else currentDigits[i] = null;
        }

        for (int i = 0; i < highDigits.Length; i++)
        {
            if (i < highList.Count) highDigits[i] = highList[i];
            else highDigits[i] = null;
        }

        // Log kết quả để kiểm tra xem đã tìm thấy bao nhiêu đối tượng
        Debug.Log($"[GameManager] Panel {panel.name}: Found {currentList.Count} current digits, {highList.Count} high score digits.");
    }

    private void SortDigits(System.Collections.Generic.List<Image> list)
    {
        list.Sort((a, b) => {
            // Sắp xếp strictly theo vị trí X từ trái qua phải (tọa độ thế giới)
            // để khớp với thứ tự hiển thị của chữ số từ hàng cao nhất (trái) đến hàng đơn vị (phải).
            return a.transform.position.x.CompareTo(b.transform.position.x);
        });
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}