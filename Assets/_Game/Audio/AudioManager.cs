using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private const string PrefBgmOn = "opt_bgm_on";
    private const string PrefSeOn  = "opt_se_on";

    [Header("Sources")]
    public AudioSource bgmSource;
    public AudioSource seSource;

    [Header("BGM")]
    public AudioClip bgmClip;
    [Range(0f, 1f)] public float bgmVolume = 0.6f;
    public bool playBgmOnStart = true;

    [Header("SFX Clips")]
    public AudioClip spinButtonClip;
    public AudioClip lineWinClip;
    public AudioClip jackpotClip;

    [Header("UI SFX")]
    public AudioClip uiClickClip;
    [Range(0f, 1f)] public float uiClickVolume = 0.6f;

    [Header("SFX Volume")]
    [Range(0f, 1f)] public float spinButtonVolume = 0.8f;
    [Range(0f, 1f)] public float lineWinVolume = 0.9f;
    [Range(0f, 1f)] public float jackpotVolume = 1.0f;

    // --- ON/OFF状態 ---
    public bool BgmOn { get; private set; } = true;
    public bool SeOn  { get; private set; } = true;

    void Awake()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
        }

        if (seSource == null)
        {
            seSource = gameObject.AddComponent<AudioSource>();
            seSource.playOnAwake = false;
        }

        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;

        LoadOptions();
        ApplyOptionsToSources();
    }

    void Start()
    {
        if (playBgmOnStart)
        {
            PlayBgm();
        }
    }

    // ---- Public API ----

    public void SetBgmOn(bool on)
    {
        BgmOn = on;
        PlayerPrefs.SetInt(PrefBgmOn, on ? 1 : 0);
        PlayerPrefs.Save();

        ApplyOptionsToSources();

        if (BgmOn) PlayBgm();
        else StopBgm();
    }

    public void SetSeOn(bool on)
    {
        SeOn = on;
        PlayerPrefs.SetInt(PrefSeOn, on ? 1 : 0);
        PlayerPrefs.Save();

        ApplyOptionsToSources();
    }

    public void PlayBgm()
    {
        if (!BgmOn) return;
        if (bgmSource == null || bgmClip == null) return;

        bgmSource.clip = bgmClip;
        bgmSource.volume = bgmVolume;

        if (!bgmSource.isPlaying)
            bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Stop();
    }

    public void PlaySpinButton() => PlaySe(spinButtonClip, spinButtonVolume);
    public void PlayLineWin()     => PlaySe(lineWinClip, lineWinVolume);
    public void PlayJackpot()     => PlaySe(jackpotClip, jackpotVolume);

    public void PlaySe(AudioClip clip, float volume = 1f)
    {
        if (!SeOn) return;
        if (seSource == null || clip == null) return;
        seSource.PlayOneShot(clip, volume);
    }

    public void PlayUiClick()
    {
        PlaySe(uiClickClip, uiClickVolume);
    }

    // ---- Internal ----

    private void LoadOptions()
    {
        // デフォルトON（キー未存在なら 1）
        BgmOn = PlayerPrefs.GetInt(PrefBgmOn, 1) == 1;
        SeOn  = PlayerPrefs.GetInt(PrefSeOn,  1) == 1;
    }

    private void ApplyOptionsToSources()
    {
        if (bgmSource != null)
        {
            bgmSource.mute = !BgmOn;
            bgmSource.volume = bgmVolume;
        }

        if (seSource != null)
        {
            seSource.mute = !SeOn;
        }
    }
}
