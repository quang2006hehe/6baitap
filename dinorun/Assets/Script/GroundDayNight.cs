using UnityEngine;

public class GroundDayNight : MonoBehaviour
{
    [Tooltip("Kéo đối tượng bg_ground_night vào đây")]
    public SpriteRenderer nightGroundRenderer;

    private SpriteRenderer nightSkyRenderer;

    void Start()
    {
        // Tìm bức ảnh bầu trời đêm trên màn hình (để copy độ mờ)
        GameObject nightSky = GameObject.Find("bg_sky_night");
        if (nightSky != null)
        {
            nightSkyRenderer = nightSky.GetComponent<SpriteRenderer>();
        }
    }

    void Update()
    {
        // Bầu trời mờ bao nhiêu, mặt đất sẽ mờ y hệt bấy nhiêu
        if (nightSkyRenderer != null && nightGroundRenderer != null)
        {
            Color groundColor = nightGroundRenderer.color;
            groundColor.a = nightSkyRenderer.color.a;
            nightGroundRenderer.color = groundColor;
        }
    }
}