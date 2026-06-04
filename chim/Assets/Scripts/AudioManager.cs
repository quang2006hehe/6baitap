using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private AudioClip flapClip;
    private AudioClip scoreClip;
    private AudioClip hitClip;
    private AudioClip bgmClip;

    private bool isMuted = false;

    private void Awake()
    {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Add AudioSources dynamically if not present
        bgmSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        // Generate procedural audio clips
        GenerateAudioClips();

        // Load mute setting
        isMuted = PlayerPrefs.GetInt("Muted", 0) == 1;
        UpdateMuteState();
    }

    private void Start()
    {
        PlayBGM();
    }

    private void GenerateAudioClips()
    {
        int sampleRate = 44100;

        // 1. Wing Flap Sound (Sine wave frequency sweep)
        float flapDuration = 0.12f;
        int flapSamplesLength = (int)(sampleRate * flapDuration);
        float[] flapSamples = new float[flapSamplesLength];
        for (int i = 0; i < flapSamplesLength; i++) {
            float t = (float)i / sampleRate;
            float freq = 150f + (350f - 150f) * (t / flapDuration);
            float phase = 2f * Mathf.PI * (150f * t + 0.5f * (350f - 150f) * t * t / flapDuration);
            float envelope = Mathf.Clamp01(1f - t / flapDuration);
            flapSamples[i] = Mathf.Sin(phase) * envelope * 0.3f;
        }
        flapClip = AudioClip.Create("Flap", flapSamplesLength, 1, sampleRate, false);
        flapClip.SetData(flapSamples, 0);

        // 2. Score Sound (Classic retro coin sound - two tones)
        float scoreDuration = 0.22f;
        int scoreSamplesLength = (int)(sampleRate * scoreDuration);
        float[] scoreSamples = new float[scoreSamplesLength];
        float changeTime = 0.07f;
        for (int i = 0; i < scoreSamplesLength; i++) {
            float t = (float)i / sampleRate;
            float freq = t < changeTime ? 587.33f : 880f; // D5 then A5
            float phase = 2f * Mathf.PI * freq * t;
            float envelope = Mathf.Clamp01(1f - t / scoreDuration);
            scoreSamples[i] = Mathf.Sin(phase) * envelope * 0.25f;
        }
        scoreClip = AudioClip.Create("Score", scoreSamplesLength, 1, sampleRate, false);
        scoreClip.SetData(scoreSamples, 0);

        // 3. Collision Hit Sound (Downward sweep + Noise)
        float hitDuration = 0.35f;
        int hitSamplesLength = (int)(sampleRate * hitDuration);
        float[] hitSamples = new float[hitSamplesLength];
        for (int i = 0; i < hitSamplesLength; i++) {
            float t = (float)i / sampleRate;
            float noise = Random.Range(-1f, 1f);
            float freq = 250f - (250f - 60f) * (t / hitDuration);
            float phase = 2f * Mathf.PI * (250f * t - 0.5f * (250f - 60f) * t * t / hitDuration);
            float tone = Mathf.Sin(phase);
            float envelope = Mathf.Clamp01(1f - t / hitDuration);
            hitSamples[i] = (noise * 0.6f + tone * 0.4f) * envelope * 0.4f;
        }
        hitClip = AudioClip.Create("Hit", hitSamplesLength, 1, sampleRate, false);
        hitClip.SetData(hitSamples, 0);

        // 4. Background Music (Simple looping 8-bit chiptune arpeggio)
        float noteDuration = 0.25f;
        float[] melody = {
            261.63f, 329.63f, 392.00f, 523.25f, // C4, E4, G4, C5
            293.66f, 349.23f, 392.00f, 587.33f, // D4, F4, G4, D5
            329.63f, 392.00f, 493.88f, 659.25f, // E4, G4, B4, E5
            349.23f, 440.00f, 523.25f, 698.46f  // F4, A4, C5, F5
        };
        int numNotes = melody.Length;
        float bgmDuration = noteDuration * numNotes; // 4.0 seconds
        int bgmSamplesLength = (int)(sampleRate * bgmDuration);
        float[] bgmSamples = new float[bgmSamplesLength];
        for (int i = 0; i < bgmSamplesLength; i++) {
            float t = (float)i / sampleRate;
            int noteIndex = (int)(t / noteDuration) % numNotes;
            float freq = melody[noteIndex];
            
            float noteT = t % noteDuration;
            // Short decay envelope for staccato feel
            float noteEnvelope = Mathf.Clamp01(1f - (noteT / noteDuration) * 1.4f);
            
            // Triangle-like wave: (abs(t % 1 - 0.5) - 0.25) * 4
            float phase = freq * t;
            float triValue = (Mathf.Abs((phase % 1f) - 0.5f) - 0.25f) * 4f;
            
            bgmSamples[i] = triValue * noteEnvelope * 0.05f; // Low volume BGM
        }
        bgmClip = AudioClip.Create("BGM", bgmSamplesLength, 1, sampleRate, false);
        bgmClip.SetData(bgmSamples, 0);
    }

    public void PlayFlap()
    {
        sfxSource.PlayOneShot(flapClip);
    }

    public void PlayScore()
    {
        sfxSource.PlayOneShot(scoreClip);
    }

    public void PlayHit()
    {
        sfxSource.PlayOneShot(hitClip);
    }

    public void PlayBGM()
    {
        if (bgmSource.clip == null) {
            bgmSource.clip = bgmClip;
        }
        if (!bgmSource.isPlaying) {
            bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public bool IsMuted()
    {
        return isMuted;
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        PlayerPrefs.SetInt("Muted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
        UpdateMuteState();
    }

    private void UpdateMuteState()
    {
        bgmSource.mute = isMuted;
        sfxSource.mute = isMuted;
    }
}
