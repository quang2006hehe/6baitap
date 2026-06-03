using UnityEngine;

public class MoveLeft : MonoBehaviour
{
    [Header("Recycle Boundary")]
    [SerializeField] private float leftBoundary = -15f; // Ranh giới bên trái màn hình để thu hồi vật thể

    [Header("Speed Modifier")]
    [Tooltip("Hệ số nhân tốc độ di chuyển (ví dụ: ground là 1.0, parallax background có thể nhỏ hơn)")]
    [SerializeField] private float speedMultiplier = 1.0f;

    private void Start()
    {
        // Đảm bảo biên trái thu hồi đủ xa (tối thiểu là -30) để tránh biến mất đột ngột khi vẫn còn trên màn hình
        if (leftBoundary > -30f)
        {
            leftBoundary = -30f;
        }
    }

    private void Update()
    {
        // Chỉ di chuyển khi game đang ở trạng thái Running
        if (GameManager.Instance == null || GameManager.Instance.currentGameState != GameState.Running)
        {
            return;
        }

        // Lấy tốc độ chuẩn từ GameManager
        float currentSpeed = GameManager.Instance.GameSpeed * speedMultiplier;

        // Di chuyển sang trái
        transform.Translate(Vector3.left * currentSpeed * Time.deltaTime, Space.World);

        // Thu hồi về Object Pool nếu vượt quá biên trái màn hình
        if (transform.position.x < leftBoundary)
        {
            gameObject.SetActive(false);
        }
    }
}