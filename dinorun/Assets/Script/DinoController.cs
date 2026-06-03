using UnityEngine;

public enum DinoState
{
    Idle,
    Running,
    Jumping,
    Crouching,
    Dead
}

[RequireComponent(typeof(Rigidbody2D))]
public class DinoController : MonoBehaviour
{
    public static DinoController Instance { get; private set; }

    [Header("State")]
    public DinoState currentState = DinoState.Idle;

    [Header("Movement & Physics")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float doubleJumpForce = 10f;
    [SerializeField] private float gravityMultiplier = 3f;      // Trọng lực mặc định của game
    [SerializeField] private float lowGravityMultiplier = 1.5f;  // Trọng lực khi giữ nút nhảy (nhảy cao hơn)
    [SerializeField] private float fastFallMultiplier = 6f;      // Trọng lực khi vuốt xuống trên không (rơi nhanh)
    
    [Header("Crouch Settings")]
    [SerializeField] private Vector2 normalColliderSize = new Vector2(0.8f, 1.6f);
    [SerializeField] private Vector2 normalColliderOffset = new Vector2(0f, 0f);
    [SerializeField] private Vector2 crouchColliderSize = new Vector2(0.8f, 0.8f);
    [SerializeField] private Vector2 crouchColliderOffset = new Vector2(0f, -0.4f);

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.6f, 0.1f);
    [SerializeField] private LayerMask groundLayer;
    private bool isGrounded;

    // Các thành phần cấu phần
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private Animator animator;

    // Biến điều khiển nhảy
    private bool canDoubleJump;
    private bool jumpRequested;
    private bool jumpHeld;
    private bool doubleJumpRequested;
    private bool crouchRequested;
    private float lastTapTime;
    private const float DOUBLE_TAP_TIME = 0.3f;

    // Swipe Mobile
    private Vector2 touchStartPos;
    private bool isSwiping;
    private const float MIN_SWIPE_DISTANCE = 30f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // Khởi tạo kích thước Collider ban đầu
        if (boxCollider != null)
        {
            normalColliderSize = boxCollider.size;
            normalColliderOffset = boxCollider.offset;
            // Crouch size tự động bằng 1/2 chiều cao
            crouchColliderSize = new Vector2(normalColliderSize.x, normalColliderSize.y * 0.5f);
            crouchColliderOffset = new Vector2(normalColliderOffset.x, normalColliderOffset.y - (normalColliderSize.y * 0.25f));
        }

        rb.gravityScale = gravityMultiplier;
        TransitionToState(DinoState.Idle);
    }

    private void Update()
    {
        // Nếu game chưa bắt đầu hoặc đã chết, vô hiệu hóa phím bấm điều khiển gameplay chính
        if (GameManager.Instance == null) return;
        
        GameState gameState = GameManager.Instance.currentGameState;

        if (gameState == GameState.Idle)
        {
            if (currentState != DinoState.Idle)
            {
                TransitionToState(DinoState.Idle);
            }
            return;
        }

        if (gameState == GameState.Dead)
        {
            if (currentState != DinoState.Dead)
            {
                TransitionToState(DinoState.Dead);
            }
            return;
        }

        if (gameState == GameState.Paused)
        {
            // Tạm dừng mọi điều khiển khủng long khi đang pause
            return;
        }

        // --- ĐANG CHƠI (RUNNING GAMEPLAY) ---
        CheckGroundStatus();
        HandleInputs();
    }

    private void FixedUpdate()
    {
        if (currentState == DinoState.Dead || currentState == DinoState.Idle)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Áp dụng lực nhảy
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpRequested = false;
        }
        else if (doubleJumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce);
            doubleJumpRequested = false;
        }

        // Tối ưu hóa trọng lực để nhảy biến thiên (Variable Jump Height) và Fast Fall
        if (currentState == DinoState.Jumping)
        {
            if (crouchRequested) // Đang trên không mà nhấn S / Vuốt xuống -> Rơi nhanh
            {
                rb.gravityScale = fastFallMultiplier;
            }
            else if (jumpHeld && rb.linearVelocity.y > 0) // Đang nhảy lên và giữ nút nhảy -> Giảm trọng lực để nhảy cao hơn
            {
                rb.gravityScale = lowGravityMultiplier;
            }
            else // Nhảy lên bình thường hoặc bắt đầu rơi xuống
            {
                rb.gravityScale = gravityMultiplier;
            }
        }
        else
        {
            rb.gravityScale = gravityMultiplier;
        }

        // Xử lý State Machine chuyển đổi từ Jumping -> Running/Crouching khi tiếp đất
        if (currentState == DinoState.Jumping && isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            if (crouchRequested)
            {
                TransitionToState(DinoState.Crouching);
            }
            else
            {
                TransitionToState(DinoState.Running);
            }
        }
    }

    private void CheckGroundStatus()
    {
        Vector2 checkPos = groundCheckPoint != null ? (Vector2)groundCheckPoint.position : new Vector2(transform.position.x, transform.position.y - (normalColliderSize.y / 2f));
        isGrounded = Physics2D.OverlapBox(checkPos, groundCheckSize, 0f, groundLayer);

        if (isGrounded && currentState == DinoState.Jumping && rb.linearVelocity.y <= 0.1f)
        {
            canDoubleJump = true;
        }
    }

    private void HandleInputs()
    {
        // 1. INPUTS TRÊN PC
        bool pcJumpStart = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
        jumpHeld = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
        bool pcCrouch = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        crouchRequested = pcCrouch;

        if (pcJumpStart)
        {
            TriggerJump();
        }

        // 2. INPUTS TRÊN MOBILE (Chạm & Vuốt)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.position;
                isSwiping = false;

                // Phát hiện Double Tap trên mobile để nhảy cao / nhảy kép
                float timeSinceLastTap = Time.time - lastTapTime;
                if (timeSinceLastTap < DOUBLE_TAP_TIME)
                {
                    // Chạm lần 2 nhanh -> Kích hoạt Nhảy Kép / Nhảy Cao
                    TriggerJump(true);
                }
                else
                {
                    // Chạm lần đầu
                    TriggerJump(false);
                }
                lastTapTime = Time.time;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                Vector2 diff = touch.position - touchStartPos;
                // Phát hiện vuốt xuống (Swipe Down) để cúi người
                if (diff.y < -MIN_SWIPE_DISTANCE)
                {
                    crouchRequested = true;
                    isSwiping = true;
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                if (isSwiping)
                {
                    crouchRequested = false;
                }
            }

            // Giữ chạm thì jumpHeld = true (để nhảy cao hơn)
            if (touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved)
            {
                jumpHeld = true;
            }
        }

        // Áp dụng trạng thái cúi thời gian thực
        if (currentState == DinoState.Running && crouchRequested)
        {
            TransitionToState(DinoState.Crouching);
        }
        else if (currentState == DinoState.Crouching && !crouchRequested)
        {
            TransitionToState(DinoState.Running);
        }
    }

    private void TriggerJump(bool isDoubleTap = false)
    {
        if (isGrounded)
        {
            jumpRequested = true;
            canDoubleJump = true;
            TransitionToState(DinoState.Jumping);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayJump();
            }
        }
        else if (canDoubleJump || isDoubleTap)
        {
            // Cho phép double jump nếu chưa thực hiện
            doubleJumpRequested = true;
            canDoubleJump = false;
            TransitionToState(DinoState.Jumping);
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayJump();
            }
        }
    }

    public void TransitionToState(DinoState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case DinoState.Idle:
                SetColliderSize(normalColliderSize, normalColliderOffset);
                UpdateAnimatorState("Idle");
                break;

            case DinoState.Running:
                SetColliderSize(normalColliderSize, normalColliderOffset);
                UpdateAnimatorState("Running");
                break;

            case DinoState.Jumping:
                SetColliderSize(normalColliderSize, normalColliderOffset);
                UpdateAnimatorState("Jumping");
                break;

            case DinoState.Crouching:
                SetColliderSize(crouchColliderSize, crouchColliderOffset);
                UpdateAnimatorState("Crouching");
                break;

            case DinoState.Dead:
                SetColliderSize(normalColliderSize, normalColliderOffset);
                UpdateAnimatorState("Dead");
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }

    private void SetColliderSize(Vector2 size, Vector2 offset)
    {
        if (boxCollider != null)
        {
            boxCollider.size = size;
            boxCollider.offset = offset;
        }
    }

    private void UpdateAnimatorState(string stateName)
    {
        if (animator == null) return;

        // Reset toàn bộ các bools
        animator.SetBool("isIdle", stateName == "Idle");
        animator.SetBool("isRunning", stateName == "Running");
        animator.SetBool("isJumping", stateName == "Jumping");
        animator.SetBool("isCrouching", stateName == "Crouching");
        animator.SetBool("isDead", stateName == "Dead");
        
        // Hỗ trợ thêm biến Integer State để dễ cấu hình (0: Idle, 1: Run, 2: Jump, 3: Crouch, 4: Dead)
        int stateId = 0;
        if (stateName == "Running") stateId = 1;
        else if (stateName == "Jumping") stateId = 2;
        else if (stateName == "Crouching") stateId = 3;
        else if (stateName == "Dead") stateId = 4;
        
        animator.SetInteger("state", stateId);
    }
    private void OnDrawGizmosSelected()
    {
        // Vẽ ô ground check trong Scene Editor để dễ gán ghép
        Gizmos.color = Color.green;
        Vector2 checkPos = groundCheckPoint != null ? (Vector2)groundCheckPoint.position : new Vector2(transform.position.x, transform.position.y - (normalColliderSize.y / 2f));
        Gizmos.DrawWireCube(checkPos, groundCheckSize);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Khủng long chết khi đụng xương rồng hoặc chim
        if (other.CompareTag("CactusSmall") || other.CompareTag("CactusLarge") || other.CompareTag("Bird"))
        {
            if (GameManager.Instance != null && GameManager.Instance.currentGameState == GameState.Running)
            {
                GameManager.Instance.ChangeState(GameState.Dead);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("CactusSmall") || collision.gameObject.CompareTag("CactusLarge") || collision.gameObject.CompareTag("Bird"))
        {
            if (GameManager.Instance != null && GameManager.Instance.currentGameState == GameState.Running)
            {
                GameManager.Instance.ChangeState(GameState.Dead);
            }
        }
    }
}
