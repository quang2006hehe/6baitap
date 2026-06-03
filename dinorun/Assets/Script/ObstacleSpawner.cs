using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Pool Tags for Obstacles")]
    [SerializeField] private List<string> smallCactusTags = new List<string> { "CactusSmall" };
    [SerializeField] private List<string> largeCactusTags = new List<string> { "CactusLarge" };
    [SerializeField] private List<string> birdTags = new List<string> { "Bird" };

    [Header("Spawn Position Settings")]
    [SerializeField] private Transform obstacleSpawnPoint;
    [SerializeField] private float birdLowHeight = -1.5f;   // Độ cao chim tầm thấp (phải nhảy qua)
    [SerializeField] private float birdMediumHeight = 0.5f; // Độ cao chim tầm trung (nhảy qua hoặc cúi dưới)
    [SerializeField] private float birdHighHeight = 1.8f;   // Độ cao chim tầm cao (phải cúi dưới)

    [Header("Algorithmic Spawning (Distance-based)")]
    [SerializeField] private float baseMinDistance = 12f;    // Khoảng cách tối thiểu cơ bản giữa 2 chướng ngại vật
    [SerializeField] private float baseMaxDistance = 25f;    // Khoảng cách tối đa cơ bản giữa 2 chướng ngại vật
    [SerializeField] private float speedDistanceScale = 0.5f; // Hệ số kéo dãn khoảng cách khi tốc độ tăng (giúp người chơi kịp phản xạ)

    [Header("Ground Spawning Settings")]
    [SerializeField] private string groundTag = "Ground";
    [SerializeField] private Transform groundSpawnPoint;
    [SerializeField] private float groundWidth = 20f;         // Độ rộng thực tế của một mảnh đất
    [SerializeField] private float groundOverlap = 0.35f;     // Phần xếp chồng đè lên nhau giữa 2 mảnh đất để tránh hở khe sáng (có thể tùy chỉnh trong Inspector)
    [SerializeField] private int initialGroundCount = 3;     // Số lượng mảnh đất tạo ban đầu để phủ kín màn hình
    
    private List<GameObject> activeGrounds = new List<GameObject>();
    private float nextObstacleDistance; // Khoảng cách cần thiết để sinh chướng ngại vật tiếp theo
    private Vector3 lastObstaclePos;
    private bool hasSpawnedFirstObstacle;
    private float distanceSinceLastSpawn;

    private void Start()
    {
        // Tự động tìm Ground_Complete trong scene để đồng bộ độ cao
        GameObject initialGround = GameObject.Find("Ground_Complete");
        if (initialGround == null)
        {
            initialGround = GameObject.FindWithTag("Ground");
        }

        if (initialGround != null && groundSpawnPoint != null)
        {
            float targetY = initialGround.transform.position.y;
            float currentY = groundSpawnPoint.position.y;
            float offset = targetY - currentY;

            if (Mathf.Abs(offset) > 0.001f)
            {
                // Dịch chuyển groundSpawnPoint
                Vector3 newGroundSpawnPos = groundSpawnPoint.position;
                newGroundSpawnPos.y = targetY;
                groundSpawnPoint.position = newGroundSpawnPos;

                Debug.Log($"[ObstacleSpawner] Adjusted GroundSpawnPoint height by offset {offset} to match {initialGround.name} Y ({targetY})");
            }
        }

        // Khởi tạo hệ thống mặt đất vô hạn ban đầu
        InitializeGrounds();
        
        // Đặt khoảng cách ngẫu nhiên đầu tiên
        ResetSpawner();
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentGameState != GameState.Running)
        {
            return;
        }

        // Tích lũy quãng đường di chuyển dựa trên tốc độ game
        distanceSinceLastSpawn += GameManager.Instance.GameSpeed * Time.deltaTime;

        // 1. QUẢN LÝ MẶT ĐẤT VÔ HẠN (GROUND CYCLE)
        ManageGrounds();

        // 2. SINH CHƯỚNG NGẠI VẬT THEO THUẬT TOÁN (ALGORITHMIC SPONSORING)
        ManageObstacles();
    }

    private void InitializeGrounds()
    {
        if (ObjectPooler.Instance == null) return;

        activeGrounds.Clear();

        // Tìm đối tượng đất bắt đầu có sẵn trong Scene để đo đạc và nối tiếp
        GameObject initialGround = GameObject.Find("Ground_Complete");
        if (initialGround == null)
        {
            initialGround = GameObject.FindWithTag("Ground");
        }

        Vector3 spawnPos;
        if (initialGround != null)
        {
            activeGrounds.Add(initialGround);

            // Đo độ rộng tổng hợp của mảnh đất từ toàn bộ SpriteRenderer con (tránh việc đo thiếu khi có nhiều sprite ghép lại)
            SpriteRenderer[] renderers = initialGround.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length > 0)
            {
                float minX = float.MaxValue;
                float maxX = float.MinValue;
                foreach (var r in renderers)
                {
                    minX = Mathf.Min(minX, r.bounds.min.x);
                    maxX = Mathf.Max(maxX, r.bounds.max.x);
                }
                groundWidth = maxX - minX;
                Debug.Log($"[ObstacleSpawner] Measured composite groundWidth from {initialGround.name}: {groundWidth}");
            }
            else
            {
                groundWidth = 20f; // Dự phòng
            }

            spawnPos = initialGround.transform.position;
            spawnPos.x += groundWidth - groundOverlap; // Đặt mảnh tiếp theo đè lên mảnh trước một chút để tránh hở khe sáng
        }
        else
        {
            spawnPos = groundSpawnPoint != null ? groundSpawnPoint.position : Vector3.zero;
        }

        // Sinh thêm các mảnh đất còn lại để lấp đầy màn hình bắt đầu từ spawnPos
        for (int i = activeGrounds.Count; i < initialGroundCount; i++)
        {
            GameObject ground = ObjectPooler.Instance.SpawnFromPool(groundTag, spawnPos, Quaternion.identity);
            if (ground != null)
            {
                activeGrounds.Add(ground);
                spawnPos.x += groundWidth - groundOverlap;
            }
        }
    }

    private void ManageGrounds()
    {
        if (ObjectPooler.Instance == null || activeGrounds.Count == 0) return;

        // Loại bỏ các mặt đất đã bị deactivate (do đi qua biên trái màn hình)
        activeGrounds.RemoveAll(g => g == null || !g.activeSelf);

        // Nếu số mảnh đất đang hoạt động ít hơn số lượng mong muốn, sinh thêm nối tiếp mảnh cuối cùng
        while (activeGrounds.Count < initialGroundCount)
        {
            float rightmostX = -999f;
            foreach (var g in activeGrounds)
            {
                if (g.transform.position.x > rightmostX)
                {
                    rightmostX = g.transform.position.x;
                }
            }

            // Nếu không tìm thấy mảnh đất nào đang chạy, dùng tọa độ spawn gốc
            Vector3 spawnPos = groundSpawnPoint.position;
            if (rightmostX > -999f)
            {
                spawnPos.x = rightmostX + groundWidth - groundOverlap; // Đè lên một chút để tránh bị hở khe sáng
            }

            GameObject newGround = ObjectPooler.Instance.SpawnFromPool(groundTag, spawnPos, Quaternion.identity);
            if (newGround != null)
            {
                activeGrounds.Add(newGround);
            }
            else
            {
                break;
            }
        }
    }

    private void ManageObstacles()
    {
        if (ObjectPooler.Instance == null || obstacleSpawnPoint == null) return;

        // Nếu chưa sinh chướng ngại vật nào, bắt đầu tính từ vị trí spawn gốc
        if (!hasSpawnedFirstObstacle)
        {
            SpawnObstacle();
            hasSpawnedFirstObstacle = true;
            distanceSinceLastSpawn = 0f;
            return;
        }

        // Kiểm tra quãng đường tích lũy đã vượt quá khoảng cách sinh chướng ngại vật tiếp theo chưa
        if (distanceSinceLastSpawn >= nextObstacleDistance)
        {
            SpawnObstacle();
            distanceSinceLastSpawn = 0f;
        }
    }

    private void SpawnObstacle()
    {
        if (ObjectPooler.Instance == null) return;

        string selectedTag = ChooseObstacleTagBasedOnDifficulty();
        
        // Đẩy điểm spawn ra xa màn hình về bên phải (+10f) để vật thể xuất hiện từ ngoài rìa camera một cách mượt mà
        Vector3 spawnPosition = obstacleSpawnPoint.position;
        spawnPosition.x += 10f;

        // Nếu là chim, điều chỉnh độ cao bay ngẫu nhiên
        if (birdTags.Contains(selectedTag))
        {
            float[] heights = new float[] { birdLowHeight, birdMediumHeight, birdHighHeight };
            float chosenHeight = heights[Random.Range(0, heights.Length)];
            spawnPosition.y = chosenHeight;
        }

        // Kích hoạt vật thể từ Object Pool
        GameObject obstacle = ObjectPooler.Instance.SpawnFromPool(selectedTag, spawnPosition, Quaternion.identity);
        
        if (obstacle != null)
        {
            SpriteRenderer sr = obstacle.GetComponent<SpriteRenderer>();
            if (sr == null) sr = obstacle.GetComponentInChildren<SpriteRenderer>();

            if (sr != null)
            {
                if (birdTags.Contains(selectedTag))
                {
                    // Lật ngang Sprite để đầu chim hướng sang bên trái (hướng bay) thay vì quay về bên phải cùng hướng khủng long
                    sr.flipX = true;
                }
                else
                {
                    // Điều chỉnh trục Y cho các loại xương rồng dựa trên SpriteRenderer để nằm đúng mặt đất
                    // (Tránh việc pivot đặt ở tâm làm xương rồng lớn bị thụt xuống đất)
                    if (sr.sprite != null)
                    {
                        obstacle.transform.position = spawnPosition;
                        float pivotToBottom = spawnPosition.y - sr.bounds.min.y;
                        
                        Vector3 alignedPos = obstacle.transform.position;
                        alignedPos.y = obstacleSpawnPoint.position.y + pivotToBottom;
                        obstacle.transform.position = alignedPos;
                    }
                }
            }

            lastObstaclePos = obstacle.transform.position;
        }
        else
        {
            // Dự phòng nếu lỗi pool
            lastObstaclePos = obstacleSpawnPoint.position;
        }

        // Thuật toán tính khoảng cách cho chướng ngại vật tiếp theo:
        // Càng chạy nhanh (GameSpeed cao) thì khoảng cách sinh càng phải dãn ra để người chơi có đủ thời gian phản xạ.
        float speedBonus = GameManager.Instance.GameSpeed * speedDistanceScale;
        nextObstacleDistance = Random.Range(baseMinDistance, baseMaxDistance) + speedBonus;
    }

    private string ChooseObstacleTagBasedOnDifficulty()
    {
        int score = GameManager.Instance.CurrentScore;

        // Phân cấp độ khó bằng điểm số hiện tại
        if (score < 150)
        {
            // Điểm dưới 150: Chỉ sinh xương rồng nhỏ (Dễ nhất)
            return GetRandomTag(smallCactusTags);
        }
        else if (score < 400)
        {
            // Điểm từ 150 -> 400: Thêm xương rồng lớn
            float rand = Random.value;
            if (rand < 0.6f) return GetRandomTag(smallCactusTags);
            else return GetRandomTag(largeCactusTags);
        }
        else
        {
            // Điểm trên 400: Đầy đủ thử thách, xuất hiện cả chim bay lượn ở nhiều tầng cao độ
            float rand = Random.value;
            if (rand < 0.4f) return GetRandomTag(smallCactusTags);
            else if (rand < 0.7f) return GetRandomTag(largeCactusTags);
            else return GetRandomTag(birdTags);
        }
    }

    private string GetRandomTag(List<string> tagList)
    {
        if (tagList == null || tagList.Count == 0) return "CactusSmall";
        return tagList[Random.Range(0, tagList.Count)];
    }

    public void ResetSpawner()
    {
        hasSpawnedFirstObstacle = false;
        distanceSinceLastSpawn = 0f;
        
        // Tạo khoảng cách ban đầu nhỏ hơn để game vào guồng nhanh
        nextObstacleDistance = Random.Range(baseMinDistance, baseMinDistance + 3f);
    }
}
