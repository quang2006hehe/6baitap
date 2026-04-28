using UnityEngine;
using TMPro;

public class Diem : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    private float timeAlive = 0f;
    public float scoreMultiplier = 10f; // chỉnh tốc độ tăng điểm

    void Update()
    {
        if (!GameManager.isGameOver)
        {
            timeAlive += Time.deltaTime;

            int score = Mathf.FloorToInt(timeAlive * scoreMultiplier);
            scoreText.text = "Score: " + score.ToString();
        }
    }

    public void ResetScore()
    {
        timeAlive = 0f;
    }
}
