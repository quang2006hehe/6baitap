using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [HideInInspector] public int id; // Số định danh để biết cặp nào giống nhau
    [HideInInspector] public Sprite frontImage; // Ảnh mặt trước (hình quả táo, cam...)
    public Sprite backImage;  // Ảnh mặt sau (hình dấu hỏi)

    private Image img;
    private Button btn;
    private GameManager gameManager;

    void Awake()
    {
        img = GetComponent<Image>();
        btn = GetComponent<Button>();
        // Tìm đối tượng GameManager trong cảnh để báo cáo khi được click
        gameManager = Object.FindFirstObjectByType<GameManager>();

        btn.onClick.AddListener(OnCardClick); // Tự động gán sự kiện click
    }

    public void OnCardClick()
    {
        // Hiện ảnh mặt trước
        img.sprite = frontImage;
        // Báo cho GameManager biết thẻ này vừa được lật
        gameManager.CardClicked(this);
    }

    public void Close()
    {
        img.sprite = backImage; // Úp mặt lại
    }

    public void SetMatched()
    {
        btn.interactable = false; // Khóa nút nếu đã tìm đúng cặp
    }
}