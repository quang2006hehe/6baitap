using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public float scoreSpeed = 1f; // tốc độ cộng điểm

    private float score;
    private bool isGameOver = false;

    void Update()
    {
        if (isGameOver) return;

        score += Time.deltaTime * scoreSpeed;
        scoreText.text = "Score: " + Mathf.FloorToInt(score);
    }

    public void GameOver()
    {
        isGameOver = true;
    }

    public void ResetScore()
    {
        score = 0;
        isGameOver = false;
    }
}
