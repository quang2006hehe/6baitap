using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Cycle Duration Settings")]
    [Tooltip("Thời gian (tính bằng giây) để hoàn thành một chu kỳ ngày/đêm đầy đủ")]
    [SerializeField] private float cycleDuration = 60f;

    [Header("Camera Background Color Presets")]
    [SerializeField] private Color dayColor = new Color(0.86f, 0.90f, 0.98f);     // Xanh lam pastel nhạt sáng
    [SerializeField] private Color sunsetColor = new Color(1f, 0.49f, 0.61f);     // Hồng hoàng hôn rực rỡ phong cách Sakura
    [SerializeField] private Color nightColor = new Color(0.06f, 0.05f, 0.15f);    // Tím đậm/Đen nửa đêm
    [SerializeField] private Color sunriseColor = new Color(0.89f, 0.71f, 1f);     // Tím hồng bình minh dịu dàng

    [Header("Sprite Renderers for Cross-fade")]
    [Tooltip("Ảnh nền ngày (background_day_keep_original)")]
    [SerializeField] private SpriteRenderer dayBackgroundRenderer;
    [Tooltip("Ảnh nền đêm (background_night_keep_original)")]
    [SerializeField] private SpriteRenderer nightBackgroundRenderer;

    private float currentTime = 0f;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        // Tự động tìm kiếm các Renderer nếu chưa gán trong Inspector
        if (dayBackgroundRenderer == null)
        {
            GameObject dayObj = GameObject.Find("bg_sky_day");
            if (dayObj != null) dayBackgroundRenderer = dayObj.GetComponent<SpriteRenderer>();
        }
        if (nightBackgroundRenderer == null)
        {
            GameObject nightObj = GameObject.Find("bg_sky_night");
            if (nightObj != null) nightBackgroundRenderer = nightObj.GetComponent<SpriteRenderer>();
        }

        // Đảm bảo ban đầu ảnh nền ngày hiện đầy đủ, ảnh nền đêm ẩn đi
        SetSpriteAlpha(dayBackgroundRenderer, 1f);
        SetSpriteAlpha(nightBackgroundRenderer, 0f);
        UpdateCycleColors(0f);
    }

    private void Update()
    {
        // Chỉ chạy chu kỳ thời gian khi game đang ở trạng thái Running
        if (GameManager.Instance == null || GameManager.Instance.currentGameState != GameState.Running)
        {
            return;
        }

        // Cập nhật thời gian chu kỳ
        currentTime += Time.deltaTime;
        if (currentTime >= cycleDuration)
        {
            currentTime -= cycleDuration;
        }

        // Chuẩn hóa thời gian về khoảng [0, 1]
        float progress = currentTime / cycleDuration;

        UpdateCycleColors(progress);
    }

    private void UpdateCycleColors(float progress)
    {
        Color currentBgColor = dayColor;
        float dayAlpha = 1f;
        float nightAlpha = 0f;

        // Chu kỳ chia làm 4 giai đoạn bằng nhau:
        // 0.0 -> 0.25: Ngày (Day)
        // 0.25 -> 0.50: Hoàng hôn (Sunset)
        // 0.50 -> 0.75: Đêm (Night)
        // 0.75 -> 1.00: Bình minh (Sunrise)

        if (progress < 0.25f)
        {
            // Giai đoạn 1: Ngày (Day)
            // Giữ nguyên màu ngày rực rỡ
            currentBgColor = dayColor;
            dayAlpha = 1f;
            nightAlpha = 0f;
        }
        else if (progress < 0.50f)
        {
            // Giai đoạn 2: Hoàng hôn (Day -> Sunset -> Night)
            // Tỷ lệ chuyển tiếp từ 0.0 đến 1.0 trong giai đoạn này
            float t = (progress - 0.25f) / 0.25f;

            if (t < 0.5f)
            {
                // Chuyển từ Ngày sang Hoàng hôn
                currentBgColor = Color.Lerp(dayColor, sunsetColor, t * 2f);
                dayAlpha = Mathf.Lerp(1f, 0.5f, t * 2f);
                nightAlpha = Mathf.Lerp(0f, 0.5f, t * 2f);
            }
            else
            {
                // Chuyển từ Hoàng hôn sang Đêm
                currentBgColor = Color.Lerp(sunsetColor, nightColor, (t - 0.5f) * 2f);
                dayAlpha = Mathf.Lerp(0.5f, 0f, (t - 0.5f) * 2f);
                nightAlpha = Mathf.Lerp(0.5f, 1f, (t - 0.5f) * 2f);
            }
        }
        else if (progress < 0.75f)
        {
            // Giai đoạn 3: Đêm (Night)
            // Giữ nguyên màu đêm huyền bí
            currentBgColor = nightColor;
            dayAlpha = 0f;
            nightAlpha = 1f;
        }
        else
        {
            // Giai đoạn 4: Bình minh (Night -> Sunrise -> Day)
            float t = (progress - 0.75f) / 0.25f;

            if (t < 0.5f)
            {
                // Chuyển từ Đêm sang Bình minh
                currentBgColor = Color.Lerp(nightColor, sunriseColor, t * 2f);
                dayAlpha = Mathf.Lerp(0f, 0.5f, t * 2f);
                nightAlpha = Mathf.Lerp(1f, 0.5f, t * 2f);
            }
            else
            {
                // Chuyển từ Bình minh sang Ngày
                currentBgColor = Color.Lerp(sunriseColor, dayColor, (t - 0.5f) * 2f);
                dayAlpha = Mathf.Lerp(0.5f, 1f, (t - 0.5f) * 2f);
                nightAlpha = Mathf.Lerp(0.5f, 0f, (t - 0.5f) * 2f);
            }
        }

        // 1. Áp dụng màu nền cho Camera
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = currentBgColor;
        }

        // 2. Thực hiện Cross-fade mượt mà giữa các Sprite Nền
        SetSpriteAlpha(dayBackgroundRenderer, dayAlpha);
        SetSpriteAlpha(nightBackgroundRenderer, nightAlpha);
    }

    private void SetSpriteAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer != null)
        {
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
