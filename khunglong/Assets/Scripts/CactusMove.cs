using UnityEngine;

public class CactusMove : MonoBehaviour
{
    public float speed = 5f;
    public float destroyX = -12f;

    void Update()
    {
        if (Time.timeScale == 0) return;

        transform.Translate(Vector2.left * speed * Time.deltaTime);

        if (transform.position.x < destroyX)
        {
            Destroy(gameObject);
        }
    }
}
