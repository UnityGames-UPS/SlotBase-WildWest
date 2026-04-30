using UnityEngine;
using UnityEngine.UI;


public class AudioManager : MonoBehaviour
{

    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _musicEnabled = PlayerPrefs.GetInt(PrefKeyMusic, 1) == 1;
        _sfxEnabled   = PlayerPrefs.GetInt(PrefKeysfx,   1) == 1;

        ApplyMusicVolume();
        ApplySfxVolume();
    }

    private const string PrefKeyMusic = "audio_music_enabled";
    private const string PrefKeysfx   = "audio_sfx_enabled";

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgMusicSource;
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioSource specialSource;
    [SerializeField] private AudioSource reserveSource;
    [SerializeField] private AudioSource winBgSource;

    [Header("Game Start / BG")]
    [SerializeField] private AudioClip clipGameStart;
    [SerializeField] private AudioClip clipBgMusic;

    [Header("UI / Button Sounds")]
    [SerializeField] private AudioClip clipButtonGeneric;
    [SerializeField] private AudioClip clipPopupClose;
    [SerializeField] private AudioClip clipPageSwipe;

    [Header("Bet Sounds")]
    [SerializeField] private AudioClip clipBetPlus;
    [SerializeField] private AudioClip clipBetMinus;
    [SerializeField] private AudioClip clipMaxBet;

    [Header("Buy Free Spin Sounds")]
    [SerializeField] private AudioClip clipBuyFreeSpinOpen;
    [SerializeField] private AudioClip clipBuyConfirmClose;
    [SerializeField] private AudioClip clipBuyBetPlusMinus;

    [Header("Spin Sounds")]
    [SerializeField] private AudioClip clipSpinStart;
    [SerializeField] private AudioClip clipSpinStop;
    [SerializeField] private AudioClip clipReelStop;

    [Header("Reel Hit Sounds")]
    [SerializeField] private AudioClip clipScatterHit;
    [SerializeField] private AudioClip clipWildHit;
    [SerializeField] private AudioClip clip3ScatterHit;
    [SerializeField] private AudioClip clipAnticipationFastSpin;

    [Header("Free Spin Sounds")]
    [SerializeField] private AudioClip clipFreeSpinPopup;
    [SerializeField] private AudioClip clipFreeSpinIntro;
    [SerializeField] private AudioClip clipFreeSpinTotalWin;

    [Header("Win Sounds")]
    [SerializeField] private AudioClip clipWinNormal;

    [Header("Win Popup Opening Jingles (play once)")]
    [SerializeField] private AudioClip clipWinNice;
    [SerializeField] private AudioClip clipWinBig;
    [SerializeField] private AudioClip clipWinMega;
    [SerializeField] private AudioClip clipWinSuper;
    [SerializeField] private AudioClip clipWinUltimate;

    [Header("Win Popup Background Loops (loop until popup closes)")]
    [SerializeField] private AudioClip clipWinNiceBg;
    [SerializeField] private AudioClip clipWinBigBg;
    [SerializeField] private AudioClip clipWinMegaBg;
    [SerializeField] private AudioClip clipWinSuperBg;
    [SerializeField] private AudioClip clipWinUltimateBg;
    [SerializeField] private AudioClip clipWinLine;

    private bool _musicEnabled = true;
    private bool _sfxEnabled   = true;

    public bool MusicEnabled => _musicEnabled;
    public bool SfxEnabled   => _sfxEnabled;


    public void SetMusicEnabled(bool on)
    {
        _musicEnabled = on;
        PlayerPrefs.SetInt(PrefKeyMusic, on ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMusicVolume();
    }


    public void SetSfxEnabled(bool on)
    {
        _sfxEnabled = on;
        PlayerPrefs.SetInt(PrefKeysfx, on ? 1 : 0);
        PlayerPrefs.Save();
        ApplySfxVolume();
    }

    private void ApplyMusicVolume()
    {
        if (bgMusicSource == null) return;
        bgMusicSource.volume = _musicEnabled ? 0.5f : 0f;
    }

    private void ApplySfxVolume()
    {
        float v = _sfxEnabled ? 1f : 0f;
        if (uiSource      != null) uiSource.volume      = v;
        if (specialSource != null) specialSource.volume  = v;
        if (reserveSource != null) reserveSource.volume  = v;
        if (winBgSource   != null) winBgSource.volume    = v;
    }

   
    private void PlayOneShot(AudioSource preferred, AudioClip clip)
    {
        if (clip == null) return;
        if (preferred == null) return;

       
        preferred.PlayOneShot(clip);
    }

  
    private void PlayLoop(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null) return;
        source.clip  = clip;
        source.loop  = true;
        source.Play();
    }

    private void StopSource(AudioSource source)
    {
        if (source == null) return;
        source.Stop();
        source.loop = false;
    }

    public void PlayGameStart()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(specialSource, clipGameStart);
    }

 
    public void PlayBgMusic()
    {
        if (bgMusicSource == null || clipBgMusic == null) return;
        if (bgMusicSource.isPlaying && bgMusicSource.clip == clipBgMusic) return;
        bgMusicSource.clip   = clipBgMusic;
        bgMusicSource.loop   = true;
        bgMusicSource.volume = _musicEnabled ? 0.5f : 0f;
        bgMusicSource.Play();
    }

    public void StopBgMusic()
    {
        StopSource(bgMusicSource);
    }

    public void PlayButton()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(uiSource, clipButtonGeneric);
    }

    public void PlayPopupClose()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(uiSource, clipPopupClose);
    }

    public void PlayPageSwipe()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(uiSource, clipPageSwipe);
    }


    public void PlayBetPlus()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(uiSource, clipBetPlus);
    }

    public void PlayBetMinus()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(uiSource, clipBetMinus);
    }

    public void PlayMaxBet()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(uiSource, clipMaxBet);
    }

    public void PlayBuyFreeSpinOpen()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(uiSource, clipBuyFreeSpinOpen);
    }

    public void PlayBuyConfirmClose()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(uiSource, clipBuyConfirmClose);
    }

    public void PlayBuyBetPlusMinus()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(uiSource, clipBuyBetPlusMinus);
    }



    public void PlaySpinStart()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(uiSource, clipSpinStart);
    }

    public void PlaySpinStop()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(uiSource, clipSpinStop);
    }

    public void PlayReelStop()
    {
        if (!_sfxEnabled) return;
        if (specialSource != null && !specialSource.isPlaying)
            PlayOneShot(specialSource, clipReelStop);
        else
            PlayOneShot(reserveSource, clipReelStop);
    }

    public void PlayScatterHit()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(specialSource, clipScatterHit);
    }

    public void PlayWildHit()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(specialSource, clipWildHit);
    }

    public void Play3ScatterHit()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(specialSource, clip3ScatterHit);
    }

    public void PlayAnticipationFastSpin()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(specialSource, clipAnticipationFastSpin);
    }

    public void PlayFreeSpinPopup()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(specialSource, clipFreeSpinPopup);
    }

    public void PlayFreeSpinIntro()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(specialSource, clipFreeSpinIntro);
    }

    public void PlayFreeSpinTotalWin()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(specialSource, clipFreeSpinTotalWin);
    }

    public void PlayWinNormal()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(specialSource, clipWinNormal);
    }

  
    public void PlayWinOpeningJingle(double multiplier)
    {
        if (!_sfxEnabled) return;

        AudioClip clip;
        if      (multiplier >= 100) clip = clipWinUltimate;
        else if (multiplier >=  50) clip = clipWinSuper;
        else if (multiplier >=  25) clip = clipWinMega;
        else if (multiplier >=  10) clip = clipWinBig;
        else                        clip = clipWinNice;

        PlayOneShot(specialSource, clip);
    }

    public void PlayWinPopupBg(double multiplier)
    {
        if (!_sfxEnabled) return;
        if (winBgSource == null) return;

        AudioClip bgClip;
        if      (multiplier >= 100) bgClip = clipWinUltimateBg;
        else if (multiplier >=  50) bgClip = clipWinSuperBg;
        else if (multiplier >=  25) bgClip = clipWinMegaBg;
        else if (multiplier >=  10) bgClip = clipWinBigBg;
        else                        bgClip = clipWinNiceBg;

        if (bgClip == null) return;
        winBgSource.clip   = bgClip;
        winBgSource.loop   = true;
        winBgSource.volume = _sfxEnabled ? 1f : 0f;
        winBgSource.Play();
    }

    public void StopWinPopupBg()
    {
        StopSource(winBgSource);
    }

    public void PlayWinByMultiplier(double multiplier)
    {
        PlayWinOpeningJingle(multiplier);
    }

    public void PlayWinLine()
    {
        if (!_sfxEnabled) return;
        PlayLoop(uiSource, clipWinLine);
    }

    public void StopWinLine()
    {
        if (uiSource != null && uiSource.loop && uiSource.clip == clipWinLine)
        {
            StopSource(uiSource);
        }
    }


    private void OnApplicationFocus(bool hasFocus)
    {
        HandleFocus(hasFocus);
    }

    private void OnApplicationPause(bool isPaused)
    {
        HandleFocus(!isPaused);
    }

    private void HandleFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            if (bgMusicSource != null) bgMusicSource.Pause();
            if (winBgSource   != null) winBgSource.Pause();
            AudioListener.volume = 0f;
        }
        else
        {
            if (bgMusicSource != null) bgMusicSource.UnPause();
            if (winBgSource   != null) winBgSource.UnPause();
            AudioListener.volume = 1f;
        }
    }
}
