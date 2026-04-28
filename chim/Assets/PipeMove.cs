using UnityEngine;

public class PipeMove : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float deadZone = -15f; // Điểm mà ống biến mất để nhẹ máy

    void Update()
    {
        // Làm ống chạy sang trái
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        // Nếu chạy quá màn hình bên trái thì xóa đi
        if (transform.position.x < deadZone)
        {
            Destroy(gameObject);
        }
    }
}