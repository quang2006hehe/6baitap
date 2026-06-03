using UnityEngine;

public class ParallaxScroller : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Tooltip("Hệ số tốc độ (0 = đứng yên, 0.1 = rất xa như bầu trời, 0.5 = núi đồi, 0.9 = sát mặt đất)")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxFactor = 0.5f;

    private float spriteWidth;
    private float startPosX;
    
    // Lưu trữ sprite liền kề để làm mảnh ghép nối tiếp cho việc cuộn vô hạn
    private ParallaxScroller twinScroller;
    private bool isMaster = false;

    private void Start()
    {
        // Lấy độ rộng thực tế của Sprite để biết ranh giới cuộn
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteWidth = spriteRenderer.bounds.size.x;
        }
        else
        {
            // Dự phòng nếu không có SpriteRenderer
            spriteWidth = 20f; 
        }

        startPosX = transform.position.x;

        // Tự động tìm mảnh ghép đôi (nếu có vật thể cùng tên nằm cạnh) để quản lý ghép nối
        // Một cặp gồm 2 mảnh Sprite đặt sát nhau sẽ tạo ra chu trình cuộn vô hạn hoàn hảo.
        FindTwin();

        // Nếu không tìm thấy twin trong Scene và đối tượng này không phải là bản clone tự sinh, tự động nhân bản mảnh nền
        if (twinScroller == null && !gameObject.name.Contains("(Clone)"))
        {
            GameObject twinObj = Instantiate(gameObject, transform.parent);
            twinObj.name = gameObject.name + " (Clone)";

            // Đặt vị trí cho twin nằm ngay phía sau (bên phải) mảnh hiện tại
            Vector3 twinPos = transform.position;
            twinPos.x += spriteWidth - 0.02f;
            twinObj.transform.position = twinPos;

            // Liên kết 2 mảnh nền với nhau
            ParallaxScroller twinScript = twinObj.GetComponent<ParallaxScroller>();
            if (twinScript != null)
            {
                this.twinScroller = twinScript;
                twinScript.twinScroller = this;

                // Phân chia master/slave
                this.isMaster = false;
                twinScript.isMaster = true;
            }

            Debug.Log($"[ParallaxScroller] Automatically spawned twin for {gameObject.name} at X: {twinPos.x}");
        }
    }

    private void FindTwin()
    {
        // Nếu đã được gán sẵn twin (ví dụ do mảnh gốc gán khi sinh clone), không cần tìm nữa
        if (twinScroller != null) return;

        // Sử dụng FindObjectsByType hoặc FindObjectsOfType để tìm tất cả ParallaxScroller cùng tên trong scene
        #if UNITY_2023_1_OR_NEWER
        ParallaxScroller[] scrollers = FindObjectsByType<ParallaxScroller>(FindObjectsSortMode.None);
        #else
        ParallaxScroller[] scrollers = FindObjectsOfType<ParallaxScroller>();
        #endif

        // Loại bỏ phần đuôi "(Clone)" khi so sánh tên để tìm mảnh tương đương trong scene
        string cleanName = gameObject.name.Replace(" (Clone)", "");

        foreach (ParallaxScroller other in scrollers)
        {
            if (other != this)
            {
                string otherCleanName = other.name.Replace(" (Clone)", "");
                if (otherCleanName == cleanName)
                {
                    twinScroller = other;
                    // Thiết lập mảnh có X lớn hơn là Master để kiểm soát khớp nối ban đầu
                    if (transform.position.x > other.transform.position.x)
                    {
                        isMaster = true;
                    }
                    break;
                }
            }
        }
    }

    private void Update()
    {
        // Chỉ cuộn nền khi game đang ở trạng thái Running
        if (GameManager.Instance == null || GameManager.Instance.currentGameState != GameState.Running)
        {
            return;
        }

        // Tính toán tốc độ di chuyển thực tế dựa trên tốc độ game và hệ số parallax
        float currentSpeed = GameManager.Instance.GameSpeed * parallaxFactor;
        
        // Di chuyển sang trái
        transform.Translate(Vector3.left * currentSpeed * Time.deltaTime, Space.World);

        // Biên giới hạn biến mất chung cho cả 2 mảnh (lấy theo mảnh bắt đầu ở xa nhất về bên trái)
        float leftLimit = Mathf.Min(startPosX, twinScroller != null ? twinScroller.startPosX : startPosX) - spriteWidth;

        // Kiểm tra xem mảnh nền đã đi quá giới hạn biến mất hoàn toàn chưa
        if (transform.position.x <= leftLimit)
        {
            Reposition();
        }
    }

    private void Reposition()
    {
        // Dịch chuyển mảnh nền về phía bên phải để nối tiếp mảnh nền đang chạy
        if (twinScroller != null)
        {
            // Nối trực tiếp vào sau mảnh ghép đôi của nó để tránh khe hở ánh sáng
            Vector3 targetPos = twinScroller.transform.position;
            targetPos.x += spriteWidth - 0.02f; // Trừ đi một khoảng rất nhỏ để khít khịt
            transform.position = new Vector3(targetPos.x, transform.position.y, transform.position.z);
        }
        else
        {
            // Phương án dự phòng nếu không tìm thấy mảnh ghép đôi:
            // Tự dịch chuyển theo chu kỳ chiều rộng
            Vector3 targetPos = transform.position;
            targetPos.x += spriteWidth * 2f;
            transform.position = targetPos;
        }
    }
}
