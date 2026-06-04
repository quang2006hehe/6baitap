using UnityEngine;

/// <summary>
/// Trình quản lý âm thanh (AudioManager) chịu trách nhiệm phát nhạc nền (BGM) và hiệu ứng âm thanh (SFX).
/// Hỗ trợ cả hai chế độ: sử dụng tệp âm thanh kéo thả từ Unity Inspector hoặc tự động sinh âm thanh retro 8-bit bằng toán học (procedural audio) khi không có tệp âm thanh nào được gán.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;

    /// <summary>
    /// Thuộc tính Singleton để các lớp khác (ví dụ: Snake) dễ dàng gọi phát âm thanh.
    /// </summary>
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Tìm kiếm xem đã có AudioManager nào trong Scene chưa
                _instance = FindObjectOfType<AudioManager>();
                if (_instance == null)
                {
                    // Nếu chưa có, tự động tạo mới một GameObject và gán thành phần AudioManager vào
                    GameObject go = new GameObject("AudioManager (Tự động tạo)");
                    _instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("Tệp âm thanh tùy chỉnh (Không bắt buộc - Để trống sẽ tự sinh âm thanh 8-bit)")]
    [Tooltip("Tệp âm thanh khi rắn ăn thức ăn")]
    public AudioClip eatClip;
    
    [Tooltip("Tệp âm thanh khi rắn bị chết/reset")]
    public AudioClip dieClip;
    
    [Tooltip("Tệp nhạc nền của game")]
    public AudioClip bgmClip;

    // Các thành phần phát âm thanh được tạo lúc chạy
    private AudioSource sfxSource;
    private AudioSource bgmSource;

    private void Awake()
    {
        // Đảm bảo chỉ có một thực thể duy nhất hoạt động trong suốt quá trình chạy game (Singleton)
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Khởi tạo các thành phần AudioSource để phát nhạc
        sfxSource = gameObject.AddComponent<AudioSource>();
        bgmSource = gameObject.AddComponent<AudioSource>();

        // Thiết lập nhạc nền: lặp lại liên tục và để âm lượng phù hợp
        bgmSource.loop = true;
        bgmSource.volume = 0.18f; 

        // Khởi tạo âm thanh tự sinh nếu người dùng không gán tệp trong Inspector
        InitializeProceduralSounds();
    }

    private void Start()
    {
        // Bắt đầu phát nhạc nền khi game khởi chạy
        PlayBGM();
    }

    /// <summary>
    /// Kiểm tra và tự sinh âm thanh bằng code nếu các trường Clip trong Inspector bị để trống.
    /// </summary>
    private void InitializeProceduralSounds()
    {
        if (eatClip == null)
        {
            eatClip = CreateEatClip();
        }
        if (dieClip == null)
        {
            dieClip = CreateDieClip();
        }
        if (bgmClip == null)
        {
            bgmClip = CreateBGMClip();
        }
    }

    /// <summary>
    /// Tự động chạy khi game được tải để đảm bảo AudioManager luôn tồn tại trong game.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeOnLoad()
    {
        // Tìm AudioManager đã được đặt thủ công trong Scene (nếu có)
        AudioManager existing = FindObjectOfType<AudioManager>();
        if (existing == null)
        {
            // Nếu không có, tạo thực thể tự động
            GameObject go = new GameObject("AudioManager (Tự động khởi tạo)");
            _instance = go.AddComponent<AudioManager>();
            DontDestroyOnLoad(go);
        }
        else
        {
            _instance = existing;
            DontDestroyOnLoad(existing.gameObject);
        }
    }

    /// <summary>
    /// Tạo âm thanh Bíp ngắn có tần số tăng dần khi rắn ăn mồi (Retro Beep SFX).
    /// </summary>
    private AudioClip CreateEatClip()
    {
        int sampleRate = 44100; // Tần số lấy mẫu chuẩn CD
        float duration = 0.12f; // Độ dài âm thanh: 0.12 giây
        int numSamples = (int)(sampleRate * duration);
        float[] samples = new float[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float progress = (float)i / numSamples;
            
            // Tần số trượt từ 520Hz (Nốt Đô C5) lên 1040Hz (Nốt Đô C6) để tạo cảm giác đi lên vui tươi
            float frequency = Mathf.Lerp(520f, 1040f, progress);
            float phase = 2f * Mathf.PI * frequency * t;
            
            // Biên độ giảm dần về cuối để âm thanh tắt dần tự nhiên, không bị giật cục
            float amplitude = Mathf.Lerp(0.3f, 0f, progress);
            samples[i] = Mathf.Sin(phase) * amplitude;
        }

        // Tạo AudioClip từ mảng dữ liệu âm thanh vừa tính toán
        AudioClip clip = AudioClip.Create("EatSound_Procedural", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Tạo âm thanh va chạm mạnh có tần số giảm dần pha trộn tiếng nhiễu trắng khi rắn đâm vào tường (Retro Crash/Explosion SFX).
    /// </summary>
    private AudioClip CreateDieClip()
    {
        int sampleRate = 44100;
        float duration = 0.35f; // Độ dài âm thanh: 0.35 giây
        int numSamples = (int)(sampleRate * duration);
        float[] samples = new float[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float progress = (float)i / numSamples;
            
            // Tần số trượt đi xuống từ 250Hz về 50Hz mô phỏng tiếng rơi/đâm sầm
            float frequency = Mathf.Lerp(250f, 50f, progress);
            float phase = 2f * Mathf.PI * frequency * t;
            
            // Sinh tiếng ồn ngẫu nhiên (White Noise) để tạo hiệu ứng nổ/va chạm
            float noise = (Random.value * 2f - 1f) * 0.4f;
            
            // Kết hợp sóng Sin cơ bản với tiếng ồn ngẫu nhiên
            float wave = Mathf.Sin(phase) * 0.6f;
            
            // Âm lượng nhỏ dần đều theo thời gian (giảm dần về 0)
            float envelope = 1f - progress;
            samples[i] = (wave + noise) * envelope * 0.25f;
        }

        AudioClip clip = AudioClip.Create("DieSound_Procedural", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Tạo một vòng lặp nhạc nền chiptune 8-bit đơn giản (Arpeggio) chạy tuần hoàn.
    /// </summary>
    private AudioClip CreateBGMClip()
    {
        int sampleRate = 22050; // Sử dụng tần số thấp hơn để tiết kiệm dung lượng RAM
        float duration = 4.0f;  // Nhạc nền dài 4.0 giây trước khi lặp lại
        int numSamples = (int)(sampleRate * duration);
        float[] samples = new float[numSamples];

        // Chuỗi các nốt nhạc được định nghĩa bằng tần số Hz tương ứng (mô-típ hòa thanh A-minor arpeggio)
        float[] melody = new float[] {
            220.00f, 261.63f, 329.63f, 261.63f, // Ô nhịp 1: A3, C4, E4, C4
            146.83f, 174.61f, 220.00f, 174.61f, // Ô nhịp 2: D3, F3, A3, F3
            196.00f, 246.94f, 293.66f, 246.94f, // Ô nhịp 3: G3, B3, D4, B3
            164.81f, 196.00f, 246.94f, 196.00f  // Ô nhịp 4: E3, G3, B3, G3
        };

        float noteLen = 0.25f; // Mỗi nốt nhạc vang lên trong 0.25 giây (nhịp móc đơn)
        int samplesPerNote = (int)(sampleRate * noteLen);

        for (int i = 0; i < numSamples; i++)
        {
            // Xác định nốt nhạc hiện tại dựa trên mẫu thời gian i
            int noteIndex = (i / samplesPerNote) % melody.Length;
            float freq = melody[noteIndex];
            float t = (float)i / sampleRate;
            
            // Tạo sóng Vuông (Square Wave) cho âm thanh chiptune 8-bit sắc sảo đặc trưng của game cổ điển
            float phase = 2f * Mathf.PI * freq * t;
            float squareWave = Mathf.Sin(phase) >= 0f ? 1f : -1f;
            
            // Áp dụng bộ lọc âm lượng tắt dần cho mỗi nốt (Envelope Decay) tạo cảm giác gảy phím
            float noteProgress = (float)(i % samplesPerNote) / samplesPerNote;
            float envelope = Mathf.Exp(-4.5f * noteProgress); // Tắt nhanh để các nốt tách rời rõ ràng
            
            samples[i] = squareWave * envelope * 0.05f; // Âm lượng cực nhỏ để làm nền dễ chịu
        }

        AudioClip clip = AudioClip.Create("BGM_Procedural", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Phát hiệu ứng âm thanh ăn mồi.
    /// </summary>
    public void PlayEatSound()
    {
        if (eatClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(eatClip);
        }
    }

    /// <summary>
    /// Phát hiệu ứng âm thanh va chạm/chết.
    /// </summary>
    public void PlayDieSound()
    {
        if (dieClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(dieClip);
        }
    }

    /// <summary>
    /// Phát nhạc nền lặp đi lặp lại.
    /// </summary>
    public void PlayBGM()
    {
        if (bgmClip != null && bgmSource != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.Play();
        }
    }
}
