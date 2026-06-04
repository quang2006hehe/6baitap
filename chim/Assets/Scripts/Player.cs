using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    public Sprite[] sprites;
    public float strength = 5f;
    public float tilt = 5f;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private int spriteIndex;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        
        // Configure Rigidbody2D settings dynamically
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 2f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
    }

    private void Start()
    {
        InvokeRepeating(nameof(AnimateSprite), 0.15f, 0.15f);
    }

    private void OnEnable()
    {
        transform.position = Vector3.zero;
        if (rb != null) {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = true;
        }
    }

    private void OnDisable()
    {
        if (rb != null) {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) {
            rb.linearVelocity = Vector2.up * strength;
            if (AudioManager.Instance != null) {
                AudioManager.Instance.PlayFlap();
            }
        }

        // Tilt the bird based on the velocity
        float targetAngle = Mathf.Clamp(rb.linearVelocity.y * tilt, -90f, 30f);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, targetAngle), 15f * Time.deltaTime);
    }

    private void AnimateSprite()
    {
        spriteIndex++;

        if (spriteIndex >= sprites.Length) {
            spriteIndex = 0;
        }

        if (spriteIndex < sprites.Length && spriteIndex >= 0) {
            spriteRenderer.sprite = sprites[spriteIndex];
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Obstacle")) {
            if (AudioManager.Instance != null) {
                AudioManager.Instance.PlayHit();
            }
            GameManager.Instance.GameOver();
        } else if (other.gameObject.CompareTag("Scoring")) {
            GameManager.Instance.IncreaseScore();
        }
    }

}

