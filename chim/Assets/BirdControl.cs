using UnityEngine;

public partial class BirdControl : MonoBehaviour
{
    public Rigidbody2D rb;
    public float jumpForce = 5f; // Độ cao khi nhảy

    void Update()
    {
        // Kiểm tra nếu nhấn phím Cách (Space) hoặc chuột trái
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            // Reset vận tốc về 0 để mỗi lần nhảy đều có lực như nhau
            rb.linearVelocity = Vector2.up * jumpForce;
        }
    }
}