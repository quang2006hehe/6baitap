using UnityEngine;

public class DinoCollision : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Obstacle"))
        {
            Debug.Log("💀 GAME OVER - Va chạm xương rồng");

            FindObjectOfType<GameManager>().GameOver();
        }
    }
}
