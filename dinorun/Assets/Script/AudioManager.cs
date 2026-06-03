using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Clips")]
    public AudioClip bgmClip;
    public AudioClip jumpClip;
    public AudioClip scoreClip;
    public AudioClip deathClip;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Chỉ gọi DontDestroyOnLoad nếu AudioManager nằm trên GameObject riêng biệt (không chứa GameManager)
            if (transform.parent == null && GetComponent<GameManager>() == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Tự động cấu hình AudioSource nếu chưa có
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = 0.4f; // Nhạc nền vừa phải
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = 0.7f; // SFX to rõ
        }
    }

    public void PlayBGM()
    {
        if (musicSource != null && bgmClip != null)
        {
            if (musicSource.clip != bgmClip)
            {
                musicSource.clip = bgmClip;
            }
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }
    }

    public void StopBGM()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public void PauseBGM()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    public void ResumeBGM()
    {
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.UnPause();
        }
    }

    public void PlayJump()
    {
        if (sfxSource != null && jumpClip != null)
        {
            sfxSource.PlayOneShot(jumpClip);
        }
    }

    public void PlayScore()
    {
        if (sfxSource != null && scoreClip != null)
        {
            sfxSource.PlayOneShot(scoreClip);
        }
    }

    public void PlayDeath()
    {
        if (sfxSource != null && deathClip != null)
        {
            sfxSource.PlayOneShot(deathClip);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Tự động gán âm thanh từ thư mục Assets/Sounds khi biên dịch trong Editor
        if (bgmClip == null)
        {
            bgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/the_mountain-8-bit-retro-522443.mp3");
        }
        if (jumpClip == null)
        {
            jumpClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/dino_jump.wav");
        }
        if (scoreClip == null)
        {
            scoreClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/dino_score.wav");
        }
        if (deathClip == null)
        {
            deathClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/dino_death.wav");
        }
    }
#endif
}
