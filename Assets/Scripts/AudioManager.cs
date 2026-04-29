using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Centralized singleton AudioManager.
/// 
/// Four AudioSources — assign each in the Inspector:
///   bgMusicSource   → looping background music (never one-shots)
///   uiSource        → button / UI sound one-shots
///   specialSource   → scatter, wild, freespin, win popup one-shots
///   reserveSource   → fallback when another source is already busy
///
/// Music and SFX can be toggled independently (persisted in PlayerPrefs).
/// The UIManager wires music/sfx Toggle components to SetMusicEnabled /
/// SetSfxEnabled exactly the same way the spin-speed toggles work.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────
    // Singleton
    // ─────────────────────────────────────────────────────────────────

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

        // Load persisted toggle state
        _musicEnabled = PlayerPrefs.GetInt(PrefKeyMusic, 1) == 1;
        _sfxEnabled   = PlayerPrefs.GetInt(PrefKeysfx,   1) == 1;

        // Sync AudioSource volumes to restored state
        ApplyMusicVolume();
        ApplySfxVolume();
    }

    // ─────────────────────────────────────────────────────────────────
    // PlayerPrefs keys
    // ─────────────────────────────────────────────────────────────────

    private const string PrefKeyMusic = "audio_music_enabled";
    private const string PrefKeysfx   = "audio_sfx_enabled";

    // ─────────────────────────────────────────────────────────────────
    // Four AudioSources
    // ─────────────────────────────────────────────────────────────────

    [Header("Audio Sources")]
    [Tooltip("Plays background music in a loop.")]
    [SerializeField] private AudioSource bgMusicSource;

    [Tooltip("Plays button and UI-related one-shot sounds.")]
    [SerializeField] private AudioSource uiSource;

    [Tooltip("Plays special sounds: scatter, wild, freespin popups, win popups.")]
    [SerializeField] private AudioSource specialSource;

    [Tooltip("Reserve source — used when uiSource or specialSource is already playing and an extra sound is needed.")]
    [SerializeField] private AudioSource reserveSource;

    // ─────────────────────────────────────────────────────────────────
    // Audio Clips — drag assets in Inspector
    // ─────────────────────────────────────────────────────────────────

    [Header("Game Start / BG")]
    [Tooltip("Played when the intro animation object is enabled.")]
    [SerializeField] private AudioClip clipGameStart;

    [Tooltip("Background music — loops continuously while game screen is open.")]
    [SerializeField] private AudioClip clipBgMusic;

    [Header("UI / Button Sounds")]
    [Tooltip("Generic button click — buy free spin, autoplay open, settings open, history, exit, game rules open.")]
    [SerializeField] private AudioClip clipButtonGeneric;

    [Tooltip("Played whenever a popup or panel closes with animation.")]
    [SerializeField] private AudioClip clipPopupClose;

    [Tooltip("Played when a page swipe arrow is pressed in Game Rules or History.")]
    [SerializeField] private AudioClip clipPageSwipe;

    [Header("Bet Sounds")]
    [Tooltip("Bet + button pressed.")]
    [SerializeField] private AudioClip clipBetPlus;

    [Tooltip("Bet − button pressed.")]
    [SerializeField] private AudioClip clipBetMinus;

    [Tooltip("Max bet indicator shown.")]
    [SerializeField] private AudioClip clipMaxBet;

    [Header("Spin Sounds")]
    [Tooltip("Spin started.")]
    [SerializeField] private AudioClip clipSpinStart;

    [Tooltip("Spin stop sequence complete.")]
    [SerializeField] private AudioClip clipSpinStop;

    [Tooltip("Played for each reel just as it snaps into place.")]
    [SerializeField] private AudioClip clipReelStop;

    [Header("Reel Hit Sounds")]
    [Tooltip("Played when a reel snaps and reveals a scatter symbol.")]
    [SerializeField] private AudioClip clipScatterHit;

    [Tooltip("Played when a reel snaps and reveals a wild symbol.")]
    [SerializeField] private AudioClip clipWildHit;

    [Tooltip("Played when 3 or more scatters are confirmed across stopped reels.")]
    [SerializeField] private AudioClip clip3ScatterHit;

    [Tooltip("Played when the 5th reel enters anticipation / fast-spin mode.")]
    [SerializeField] private AudioClip clipAnticipationFastSpin;

    [Header("Free Spin Sounds")]
    [Tooltip("Played when the Free Spin start popup appears.")]
    [SerializeField] private AudioClip clipFreeSpinPopup;

    [Tooltip("Played during the Free Spin intro animation.")]
    [SerializeField] private AudioClip clipFreeSpinIntro;

    [Tooltip("Played when the Free Spin total-win popup appears.")]
    [SerializeField] private AudioClip clipFreeSpinTotalWin;

    [Header("Win Sounds")]
    [Tooltip("Played on any normal win (multiplier < 5x).")]
    [SerializeField] private AudioClip clipWinNormal;

    [Tooltip("Nice Win popup (5x – 9x).")]
    [SerializeField] private AudioClip clipWinNice;

    [Tooltip("Big Win popup (10x – 24x).")]
    [SerializeField] private AudioClip clipWinBig;

    [Tooltip("Mega Win popup (25x – 49x).")]
    [SerializeField] private AudioClip clipWinMega;

    [Tooltip("Super Win popup (50x – 99x).")]
    [SerializeField] private AudioClip clipWinSuper;

    [Tooltip("Ultimate Win popup (100x+).")]
    [SerializeField] private AudioClip clipWinUltimate;

    [Tooltip("Win line animation loop sound — looped while win lines cycle.")]
    [SerializeField] private AudioClip clipWinLine;

    // ─────────────────────────────────────────────────────────────────
    // Toggle State
    // ─────────────────────────────────────────────────────────────────

    private bool _musicEnabled = true;
    private bool _sfxEnabled   = true;

    public bool MusicEnabled => _musicEnabled;
    public bool SfxEnabled   => _sfxEnabled;

    /// <summary>Call from UIManager when the music toggle changes.</summary>
    public void SetMusicEnabled(bool on)
    {
        _musicEnabled = on;
        PlayerPrefs.SetInt(PrefKeyMusic, on ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMusicVolume();
    }

    /// <summary>Call from UIManager when the SFX toggle changes.</summary>
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
    }

    // ─────────────────────────────────────────────────────────────────
    // Internal helpers
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Play a one-shot on the preferred source.
    /// If the preferred source is currently playing, fall back to reserveSource.
    /// </summary>
    private void PlayOneShot(AudioSource preferred, AudioClip clip)
    {
        if (clip == null) return;
        if (preferred == null) return;

        // Use the preferred source directly via PlayOneShot so it doesn't
        // interrupt the current clip but plays on top.
        preferred.PlayOneShot(clip);
    }

    /// <summary>
    /// Play a clip in a loop on the given source (stops any previous loop).
    /// </summary>
    private void PlayLoop(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null) return;
        source.clip  = clip;
        source.loop  = true;
        source.Play();
    }

    /// <summary>Stop a looping source.</summary>
    private void StopSource(AudioSource source)
    {
        if (source == null) return;
        source.Stop();
        source.loop = false;
    }

    // ─────────────────────────────────────────────────────────────────
    // Public API — Game Start / BG Music
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Called when the intro animation object is enabled.</summary>
    public void PlayGameStart()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(specialSource, clipGameStart);
    }

    /// <summary>Start the looping background music.</summary>
    public void PlayBgMusic()
    {
        if (bgMusicSource == null || clipBgMusic == null) return;
        if (bgMusicSource.isPlaying && bgMusicSource.clip == clipBgMusic) return;
        bgMusicSource.clip   = clipBgMusic;
        bgMusicSource.loop   = true;
        bgMusicSource.volume = _musicEnabled ? 0.5f : 0f;
        bgMusicSource.Play();
    }

    /// <summary>Stop the background music.</summary>
    public void StopBgMusic()
    {
        StopSource(bgMusicSource);
    }

    // ─────────────────────────────────────────────────────────────────
    // Public API — UI / Button
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Generic button press sound.</summary>
    public void PlayButton()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(uiSource, clipButtonGeneric);
    }

    /// <summary>Popup / panel close sound.</summary>
    public void PlayPopupClose()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(uiSource, clipPopupClose);
    }

    /// <summary>Page swipe arrow in Game Rules or History.</summary>
    public void PlayPageSwipe()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(uiSource, clipPageSwipe);
    }

    // ─────────────────────────────────────────────────────────────────
    // Public API — Bet
    // ─────────────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────────
    // Public API — Spin
    // ─────────────────────────────────────────────────────────────────

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

    /// <summary>Called once per reel as it snaps into its final position.</summary>
    public void PlayReelStop()
    {
        if (!_sfxEnabled) return;
        // Reel stops can overlap — use reserve if special is busy
        if (specialSource != null && !specialSource.isPlaying)
            PlayOneShot(specialSource, clipReelStop);
        else
            PlayOneShot(reserveSource, clipReelStop);
    }

    // ─────────────────────────────────────────────────────────────────
    // Public API — Reel Hit Sounds
    // ─────────────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────────
    // Public API — Free Spins
    // ─────────────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────────
    // Public API — Win Sounds
    // ─────────────────────────────────────────────────────────────────

    public void PlayWinNormal()
    {
        if (!_sfxEnabled) return;
        PlayOneShot(specialSource, clipWinNormal);
    }

    /// <summary>
    /// Selects and plays the correct win-tier sound based on the win multiplier.
    ///   < 5x  → PlayWinNormal (caller should call this instead)
    ///  5 – 9x  → Nice
    /// 10 – 24x → Big
    /// 25 – 49x → Mega
    /// 50 – 99x → Super
    /// 100x+    → Ultimate
    /// </summary>
    public void PlayWinByMultiplier(double multiplier)
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

    // ─────────────────────────────────────────────────────────────────
    // Public API — Win Line Loop
    // ─────────────────────────────────────────────────────────────────

    /// <summary>Start looping the win-line sound. Call when win line animation begins.</summary>
    public void PlayWinLine()
    {
        if (!_sfxEnabled) return;
        PlayLoop(uiSource, clipWinLine);
    }

    /// <summary>Stop the win-line loop sound.</summary>
    public void StopWinLine()
    {
        // Only stop if we were looping the win line clip
        if (uiSource != null && uiSource.loop && uiSource.clip == clipWinLine)
        {
            StopSource(uiSource);
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Application Focus / Pause Handling
    // ─────────────────────────────────────────────────────────────────

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
            AudioListener.volume = 0f;
        }
        else
        {
            if (bgMusicSource != null) bgMusicSource.UnPause();
            AudioListener.volume = 1f;
        }
    }
}
