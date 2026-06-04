using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Core References")]
    [SerializeField] private Player player;
    [SerializeField] private Spawner spawner;

    [Header("UI Text References")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text comboText;
    [SerializeField] private Text achievementText;

    [Header("UI Panels")]
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject gameOver;
    [SerializeField] private GameObject getReady;
    [SerializeField] private Image muteIcon; // Image component on the Mute Button

    [Header("Icons for Sound")]
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;

    public int score { get; private set; } = 0;
    public int highScore { get; private set; } = 0;
    public int pipesPassed { get; private set; } = 0;

    // Combo variables
    private int comboMultiplier = 1;

    // Achievement trackers
    private bool unlocked10 = false;
    private bool unlocked25 = false;
    private bool unlocked50 = false;
    private bool unlocked100 = false;

    private Coroutine achievementCoroutine;

    private void Awake()
    {
        if (Instance != null) {
            DestroyImmediate(gameObject);
        } else {
            Instance = this;
        }

        // Auto spawn AudioManager if missing
        if (FindObjectOfType<AudioManager>() == null) {
            GameObject amGo = new GameObject("AudioManager");
            amGo.AddComponent<AudioManager>();
        }

        // Auto-find missing references to make setup in Unity Editor easy
        AutoFindReferences();
    }

    private void OnDestroy()
    {
        if (Instance == this) {
            Instance = null;
        }
    }

    private void Start()
    {
        // Force Portrait orientation for mobile devices
        Screen.orientation = ScreenOrientation.Portrait;

        // Load High Score
        highScore = PlayerPrefs.GetInt("HighScore", 0);

        // Load sound setting icon state
        UpdateMuteIconUI();

        // Show Start Screen UI elements (Play Button & Get Ready panel), hide GameOver
        if (playButton != null) playButton.SetActive(true);
        if (getReady != null) getReady.SetActive(true);
        if (gameOver != null) gameOver.SetActive(false);

        Pause();
    }

    private void Update()
    {
    }

    private void AutoFindReferences()
    {
        if (player == null) player = FindObjectOfType<Player>();
        if (spawner == null) spawner = FindObjectOfType<Spawner>();

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // ScoreText
        if (scoreText == null) {
            scoreText = FindComponentInCanvas<Text>(canvas, "ScoreText");
            if (scoreText == null) {
                Text txt = FindChildContainingRecursive(canvas.transform, "score")?.GetComponent<Text>();
                if (txt != null) scoreText = txt;
            }
        }

        // ComboText
        if (comboText == null) {
            comboText = FindComponentInCanvas<Text>(canvas, "ComboText");
            if (comboText == null) {
                comboText = CreateTextElement(canvas.transform, "ComboText", "", new Vector2(150, 200), 24, TextAnchor.UpperRight);
                comboText.color = Color.yellow;
            }
        }

        // AchievementText
        if (achievementText == null) {
            achievementText = FindComponentInCanvas<Text>(canvas, "AchievementText");
            if (achievementText == null) {
                achievementText = CreateTextElement(canvas.transform, "AchievementText", "", new Vector2(0, 100), 30, TextAnchor.MiddleCenter);
                achievementText.color = new Color(1f, 0.84f, 0f); // Gold color
                Outline outline = achievementText.gameObject.AddComponent<Outline>();
                if (outline != null) {
                    outline.effectColor = Color.black;
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }
        }

        // PlayButton
        if (playButton == null) {
            playButton = FindChildContainingRecursive(canvas.transform, "play");
        }

        // GameOver
        if (gameOver == null) {
            gameOver = FindChildContainingRecursive(canvas.transform, "over");
        }

        // GetReady
        if (getReady == null) {
            getReady = FindChildContainingRecursive(canvas.transform, "ready");
        }

        // Mute Button setup
        if (muteIcon == null) {
            GameObject muteGo = FindChildContainingRecursive(canvas.transform, "mute");
            if (muteGo != null) {
                muteIcon = muteGo.GetComponent<Image>();
            } else {
                // Auto create a Mute Button
                muteIcon = CreateMuteButton(canvas, new Vector2(-220, -220));
            }
        }
    }

    private GameObject FindChildContainingRecursive(Transform parent, string keyword)
    {
        foreach (Transform child in parent) {
            if (child.name.ToLower().Contains(keyword.ToLower())) {
                return child.gameObject;
            }
            GameObject result = FindChildContainingRecursive(child, keyword);
            if (result != null) return result;
        }
        return null;
    }

    private T FindComponentInCanvas<T>(Canvas canvas, string name) where T : Component
    {
        GameObject go = FindChildContainingRecursive(canvas.transform, name);
        if (go != null) return go.GetComponent<T>();
        return null;
    }

    private Text CreateTextElement(Transform parent, string name, string initialText, Vector2 anchoredPosition, int fontSize, TextAnchor alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        
        Text text = go.AddComponent<Text>();
        text.text = initialText;
        
        // Try Unity 6 default font, then fallback to Arial, wrapped in try-catch to prevent crash
        try {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        } catch {
            text.font = null;
        }

        if (text.font == null) {
            try {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            } catch {
                text.font = null;
            }
        }
        
        // Find custom font in the scene/project if it exists
        Font customFont = null;
        Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
        foreach (var f in fonts) {
            if (f.name == "bit5x3") {
                customFont = f;
                break;
            }
        }
        if (customFont != null) {
            text.font = customFont;
        }
        
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;
        
        return text;
    }

    private Image CreateMuteButton(Canvas canvas, Vector2 anchoredPosition)
    {
        GameObject go = new GameObject("MuteButton");
        go.transform.SetParent(canvas.transform, false);

        // Add Button & Image
        Image img = go.AddComponent<Image>();
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(ToggleSound);

        // Position it at corner
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(45, 45);
        rect.anchoredPosition = anchoredPosition;

        // Try to generate simple sound icons dynamically as placeholder textures if we don't have sprites!
        if (soundOnSprite == null || soundOffSprite == null) {
            CreateProceduralSoundSprites();
        }

        return img;
    }

    private void CreateProceduralSoundSprites()
    {
        // We can dynamically construct two simple 32x32 textures (Speaker and Muted Speaker)
        // Speaker Icon (Sound On)
        Texture2D texOn = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++) {
            for (int x = 0; x < 32; x++) {
                texOn.SetPixel(x, y, Color.clear);
            }
        }
        for (int y = 10; y <= 22; y++) {
            for (int x = 6; x <= 12; x++) {
                texOn.SetPixel(x, y, Color.white);
            }
        }
        for (int y = 6; y <= 26; y++) {
            int width = y >= 16 ? (26 - y) : (y - 6);
            for (int x = 12; x <= 12 + width + 3; x++) {
                if (x < 20) texOn.SetPixel(x, y, Color.white);
            }
        }
        for (int y = 10; y <= 22; y += 2) {
            texOn.SetPixel(23, y, Color.white);
        }
        for (int y = 6; y <= 26; y += 3) {
            texOn.SetPixel(27, y, Color.white);
        }
        texOn.Apply();
        soundOnSprite = Sprite.Create(texOn, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));

        // Speaker Muted Icon (Sound Off)
        Texture2D texOff = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++) {
            for (int x = 0; x < 32; x++) {
                texOff.SetPixel(x, y, Color.clear);
            }
        }
        for (int y = 10; y <= 22; y++) {
            for (int x = 6; x <= 12; x++) {
                texOff.SetPixel(x, y, Color.white);
            }
        }
        for (int y = 6; y <= 26; y++) {
            int width = y >= 16 ? (26 - y) : (y - 6);
            for (int x = 12; x <= 12 + width + 3; x++) {
                if (x < 20) texOff.SetPixel(x, y, Color.white);
            }
        }
        for (int i = 0; i < 8; i++) {
            texOff.SetPixel(22 + i, 12 + i, Color.red);
            texOff.SetPixel(22 + i, 19 - i, Color.red);
        }
        texOff.Apply();
        soundOffSprite = Sprite.Create(texOff, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }


    public void Pause()
    {
        Time.timeScale = 0f;
        if (player != null) player.enabled = false;
    }

    public void Play()
    {
        score = 0;
        pipesPassed = 0;
        if (scoreText != null) scoreText.text = score.ToString();

        // Reset Combo
        ResetCombo();

        // Reset Achievements
        unlocked10 = false;
        unlocked25 = false;
        unlocked50 = false;
        unlocked100 = false;
        if (achievementText != null) achievementText.text = "";

        if (playButton != null) playButton.SetActive(false);
        if (getReady != null) getReady.SetActive(false);
        if (gameOver != null) gameOver.SetActive(false);

        Time.timeScale = 1f;
        if (player != null) player.enabled = true;

        // Reset spawner state
        if (spawner != null) spawner.ResetSpawner();

        // Clear existing pipes
        Pipes[] pipes = FindObjectsOfType<Pipes>();
        for (int i = 0; i < pipes.Length; i++) {
            Destroy(pipes[i].gameObject);
        }
    }

    public void GameOver()
    {
        if (playButton != null) playButton.SetActive(true);
        if (gameOver != null) gameOver.SetActive(true);
        if (getReady != null) getReady.SetActive(false);

        // Update High Score if needed
        if (score > highScore) {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }

        ResetCombo();
        Pause();
    }

    public void IncreaseScore()
    {
        // Sound effect
        if (AudioManager.Instance != null) {
            AudioManager.Instance.PlayScore();
        }

        pipesPassed++;

        // Apply Milestone-based Combo: 10 pts -> x2, 30 pts -> x3
        if (score >= 30) {
            comboMultiplier = 3;
        } else if (score >= 10) {
            comboMultiplier = 2;
        } else {
            comboMultiplier = 1;
        }

        score += comboMultiplier;

        if (scoreText != null) {
            scoreText.text = score.ToString();
        }

        UpdateComboUI();
        CheckAchievements();
    }

    private void ResetCombo()
    {
        comboMultiplier = 1;
        UpdateComboUI();
    }

    private void UpdateComboUI()
    {
        if (comboText == null) return;

        if (comboMultiplier > 1) {
            comboText.text = $"Combo x{comboMultiplier}";
            comboText.gameObject.SetActive(true);
        } else {
            comboText.text = "";
            comboText.gameObject.SetActive(false);
        }
    }



    private void CheckAchievements()
    {
        if (score >= 100 && !unlocked100) {
            TriggerAchievement("Bạch Kim (100+)");
            unlocked100 = true;
        } else if (score >= 50 && !unlocked50) {
            TriggerAchievement("Huy Chương Vàng (50+)");
            unlocked50 = true;
        } else if (score >= 25 && !unlocked25) {
            TriggerAchievement("Huy Chương Bạc (25+)");
            unlocked25 = true;
        } else if (score >= 10 && !unlocked10) {
            TriggerAchievement("Huy Chương Đồng (10+)");
            unlocked10 = true;
        }
    }

    private void TriggerAchievement(string title)
    {
        if (achievementCoroutine != null) {
            StopCoroutine(achievementCoroutine);
        }
        achievementCoroutine = StartCoroutine(ShowAchievementBanner(title));
    }

    private IEnumerator ShowAchievementBanner(string title)
    {
        if (achievementText != null) {
            achievementText.text = $"🏆 Đạt thành tích:\n{title}!";
            achievementText.gameObject.SetActive(true);
            
            // Wait for 3 seconds
            yield return new WaitForSecondsRealtime(3f);
            
            // Fade out
            achievementText.text = "";
            achievementText.gameObject.SetActive(false);
        }
    }

    // Dynamic difficulty calculation helpers
    public float GetCurrentPipeSpeed(float defaultSpeed)
    {
        // Gentle speed increase: by 0.015 per pipe passed, capping speed increase at 0.8f (max 5.8)
        float speedIncrease = Mathf.Min(pipesPassed * 0.015f, 0.8f);
        return defaultSpeed + speedIncrease;
    }

    public float GetDifficultySpawnRateModifier()
    {
        // Gentle spawn frequency increase: cap at 85% of original spawn rate based on pipes passed
        return Mathf.Max(1f - pipesPassed * 0.003f, 0.85f);
    }

    public float GetDifficultyGapModifier()
    {
        // Gentle vertical gap decrease: cap at 90% of original gap based on pipes passed
        return Mathf.Max(1f - pipesPassed * 0.002f, 0.9f);
    }

    public float GetDifficultyHeightRangeModifier()
    {
        // Primary difficulty mechanism: Widen vertical spawn range as pipes passed increases.
        // Widens minHeight/maxHeight by up to 1.2 units.
        return Mathf.Min(pipesPassed * 0.04f, 1.2f);
    }

    // Sound mute toggle
    public void ToggleSound()
    {
        if (AudioManager.Instance != null) {
            AudioManager.Instance.ToggleMute();
            UpdateMuteIconUI();
        }
    }

    private void UpdateMuteIconUI()
    {
        if (muteIcon == null || AudioManager.Instance == null) return;

        bool isMuted = AudioManager.Instance.IsMuted();
        if (isMuted) {
            if (soundOffSprite != null) muteIcon.sprite = soundOffSprite;
        } else {
            if (soundOnSprite != null) muteIcon.sprite = soundOnSprite;
        }
    }
}

