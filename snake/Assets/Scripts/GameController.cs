using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public Button playButton;
    public Button pauseButton;
    public Button restartButton;

    public TMP_Text scoreText;
    private Snake snake;
    private bool isPaused = false;

    private void Start()
    {
        // Add listeners to buttons
        playButton.onClick.AddListener(PlayGame);
        pauseButton.onClick.AddListener(PauseGame);
        restartButton.onClick.AddListener(RestartGame);

        snake = FindObjectOfType<Snake>();

        // Initialize score text
        UpdateScoreUI(0);

        // Ensure the game starts with the canvas focused
        FocusCanvas();
    }

    private void Update()
    {
        // Check for spacebar key press
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isPaused)
            {
                PlayGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PlayGame()
    {
        Debug.Log("Play clicked");
        if (isPaused)
        {
            Time.timeScale = 1f;
            isPaused = false;
            playButton.gameObject.SetActive(false);
            pauseButton.gameObject.SetActive(true);
        }
    }

    public void PauseGame()
    {
        Debug.Log("Pause clicked");
        if (!isPaused)
        {
            Time.timeScale = 0f;
            isPaused = true;
            playButton.gameObject.SetActive(true);
            pauseButton.gameObject.SetActive(false);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // Ensure the game is running
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void UpdateScoreUI(int score)
    {
        Debug.Log("Scene: " + SceneManager.GetActiveScene().name);
        if (scoreText != null)
        {
            scoreText.SetText("Score: " + score);
        }
    }

    private void FocusCanvas()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            var canvasComponent = canvas.GetComponent<Canvas>();
            if (canvasComponent != null)
            {
                canvasComponent.gameObject.AddComponent<CanvasFocus>();
            }
        }
    }
}

public class CanvasFocus : MonoBehaviour
{
    private void Start()
    {
        Focus();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Focus();
        }
    }

    private void Focus()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            var canvasGameObject = canvas.gameObject;
            canvasGameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, Screen.height);
            canvasGameObject.SetActive(false);
            canvasGameObject.SetActive(true);
        }
    }
}
