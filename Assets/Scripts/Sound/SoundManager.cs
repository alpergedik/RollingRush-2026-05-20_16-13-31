using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    // --- SES ID SABITLERI ---
    public const string MUSIC_ID = "music";
    public const string ROLLING_ID = "rolling";
    public const string DRIFT_ID = "drift";
    public const string WIND_ID = "wind";
    public const string JUMP_ID = "jump";
    public const string LANDING_ID = "landing";
    public const string BOUNDARY_ID = "boundary";
    public const string BUTTON_ID = "button";
    public const string COLLECTIBLE_ID = "collectible";
    public const string GAMEOVER_ID = "gameover";

    [Serializable]
    public class Sound
    {
        public string id;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;
    }

    private class LoopAudio
    {
        public Sound sound;
        public AudioSource source;
        public float volumeScale;
    }

    [Header("General")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Sounds")]
    [SerializeField] private List<Sound> sounds = new List<Sound>();

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float loopVolume = 1f;

    private bool isMuted;
    public bool IsMuted => isMuted;

    private AudioSource sfxSource;
    private AudioSource musicSource;

    private readonly Dictionary<string, Sound> soundMap = new Dictionary<string, Sound>();
    private readonly Dictionary<string, LoopAudio> activeLoops = new Dictionary<string, LoopAudio>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        // Saved mute state from PlayerPrefs
        isMuted = PlayerPrefs.GetInt("AudioMuted", 0) == 1;

        CreateAudioSources();
        BuildSoundMap();
        ApplyAllSettings();
    }

    private void OnValidate()
    {
        masterVolume = Mathf.Clamp01(masterVolume);
        musicVolume = Mathf.Clamp01(musicVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
        loopVolume = Mathf.Clamp01(loopVolume);

        if (sounds != null)
        {
            foreach (var sound in sounds)
            {
                if (sound != null)
                {
                    sound.volume = Mathf.Clamp01(sound.volume);
                    // Eğer yeni eklendiğinde volume 0 ve clip varsa default 1 yapalım
                    if (sound.volume == 0f && sound.clip != null)
                    {
                        sound.volume = 1f;
                    }
                }
            }
        }

        if (Application.isPlaying)
        {
            BuildSoundMap();
            ApplyAllSettings();
        }
    }

    private void CreateAudioSources()
    {
        // SFX Source
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;

        // Music Source
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
    }

    private void BuildSoundMap()
    {
        soundMap.Clear();

        if (sounds == null) return;

        foreach (Sound sound in sounds)
        {
            if (sound == null)
                continue;

            if (string.IsNullOrWhiteSpace(sound.id))
            {
                Debug.LogWarning("[SoundManager] ID boş olan bir ses var.");
                continue;
            }

            if (sound.clip == null)
            {
                Debug.LogWarning("[SoundManager] AudioClip atanmamış ses: " + sound.id);
                continue;
            }

            if (soundMap.ContainsKey(sound.id))
            {
                Debug.LogWarning("[SoundManager] Aynı ID birden fazla kez kullanılmış: " + sound.id);
                continue;
            }

            soundMap.Add(sound.id, sound);
        }
    }

    // =========================
    // MUTE SYSTEM
    // =========================

    /// <summary>
    /// Oyundaki tüm sesleri açıp kapatır ve durumu kaydeder.
    /// </summary>
    public void SetMuted(bool muted)
    {
        isMuted = muted;
        PlayerPrefs.SetInt("AudioMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyAllSettings();
    }

    /// <summary>
    /// Mute durumunu tersine çevirir (Açıksa kapatır, kapalıysa açar).
    /// </summary>
    public void ToggleMute()
    {
        SetMuted(!isMuted);
    }

    // =========================
    // VOLUME SYSTEM
    // =========================

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyAllSettings();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyAllSettings();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplyAllSettings();
    }

    public void SetLoopVolume(float volume)
    {
        loopVolume = Mathf.Clamp01(volume);
        ApplyAllSettings();
    }

    public float GetMasterVolume() => masterVolume;
    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
    public float GetLoopVolume() => loopVolume;

    // =========================
    // MUSIC
    // =========================

    /// <summary>
    /// Main_Music sesini döngüsel (loop) olarak çalmaya başlar.
    /// </summary>
    public void PlayMusic()
    {
        Sound sound = GetSound(MUSIC_ID);
        if (sound == null) return;

        if (musicSource.clip == sound.clip && musicSource.isPlaying)
        {
            ApplyMusicSettings(sound);
            return;
        }

        musicSource.clip = sound.clip;
        musicSource.Play();
        ApplyMusicSettings(sound);
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    // =========================
    // SFX WRAPPERS
    // =========================

    public void PlayButton() => PlaySFX(BUTTON_ID);
    public void PlayJump() => PlaySFX(JUMP_ID);
    public void PlayLanding() => PlaySFX(LANDING_ID);
    public void PlayBoundaryHit() => PlaySFX(BOUNDARY_ID);
    public void PlayCollectiblePickup() => PlaySFX(COLLECTIBLE_ID);
    public void PlayGameOver() => PlaySFX(GAMEOVER_ID);

    // =========================
    // SFX CORE
    // =========================

    public void PlaySFX(string id, float volumeScale = 1f)
    {
        if (isMuted) return;

        Sound sound = GetSound(id);
        if (sound == null) return;

        if (sfxSource != null)
        {
            float finalVolumeScale = sound.volume * Mathf.Clamp01(volumeScale) * masterVolume * sfxVolume;
            sfxSource.PlayOneShot(sound.clip, finalVolumeScale);
        }
    }

    public void StopAllSFX()
    {
        if (sfxSource != null)
        {
            sfxSource.Stop();
        }
    }

    // =========================
    // GAMEPLAY LOOPS (Rolling, Drift, Wind)
    // =========================

    /// <summary>
    /// Toprak/Rolling loop sesinin intensity (şiddet) değerini ayarlar (0 - 1 arası).
    /// </summary>
    public void SetRollingIntensity(float intensity) => UpdateGameplayLoop(ROLLING_ID, intensity);

    /// <summary>
    /// Drift loop sesinin intensity (şiddet) değerini ayarlar (0 - 1 arası).
    /// </summary>
    public void SetDriftIntensity(float intensity) => UpdateGameplayLoop(DRIFT_ID, intensity);

    /// <summary>
    /// Rüzgar loop sesinin intensity (şiddet) değerini ayarlar (0 - 1 arası).
    /// </summary>
    public void SetWindIntensity(float intensity) => UpdateGameplayLoop(WIND_ID, intensity);

    private void UpdateGameplayLoop(string id, float intensity)
    {
        intensity = Mathf.Clamp01(intensity);
        if (intensity > 0)
        {
            if (!IsLoopPlaying(id))
            {
                PlayLoop(id, intensity);
            }
            else
            {
                SetLoopVolumeScale(id, intensity);
            }
        }
        else
        {
            if (IsLoopPlaying(id))
            {
                SetLoopVolumeScale(id, 0f);
            }
        }
    }

    /// <summary>
    /// Oyun içindeki dinamik loop seslerinin (rolling, drift, wind) tamamını durdurur.
    /// </summary>
    public void StopGameplayLoops()
    {
        StopLoop(ROLLING_ID);
        StopLoop(DRIFT_ID);
        StopLoop(WIND_ID);
    }

    // =========================
    // LOOP CORE
    // =========================

    public void PlayLoop(string id, float volumeScale = 1f)
    {
        Sound sound = GetSound(id);
        if (sound == null) return;

        if (activeLoops.TryGetValue(id, out LoopAudio activeLoop))
        {
            SetLoopVolumeScale(id, volumeScale);
            if (activeLoop.source != null && !activeLoop.source.isPlaying)
                activeLoop.source.Play();
            return;
        }

        GameObject loopObject = new GameObject("Loop Audio - " + id);
        loopObject.transform.SetParent(transform);

        AudioSource source = loopObject.AddComponent<AudioSource>();
        source.clip = sound.clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;

        LoopAudio loopAudio = new LoopAudio
        {
            sound = sound,
            source = source,
            volumeScale = Mathf.Clamp01(volumeScale)
        };

        activeLoops.Add(id, loopAudio);
        ApplyLoopSettings(loopAudio);
        source.Play();
    }

    public void StopLoop(string id)
    {
        if (!activeLoops.TryGetValue(id, out LoopAudio loopAudio))
            return;

        if (loopAudio.source != null)
            Destroy(loopAudio.source.gameObject);

        activeLoops.Remove(id);
    }

    public void StopAllLoops()
    {
        foreach (LoopAudio loopAudio in activeLoops.Values)
        {
            if (loopAudio.source != null)
                Destroy(loopAudio.source.gameObject);
        }
        activeLoops.Clear();
    }

    public bool IsLoopPlaying(string id)
    {
        if (!activeLoops.TryGetValue(id, out LoopAudio loopAudio))
            return false;

        return loopAudio.source != null && loopAudio.source.isPlaying;
    }

    public void SetLoopVolumeScale(string id, float volumeScale)
    {
        if (!activeLoops.TryGetValue(id, out LoopAudio loopAudio))
            return;

        loopAudio.volumeScale = Mathf.Clamp01(volumeScale);
        ApplyLoopSettings(loopAudio);
    }

    // =========================
    // INTERNAL SETTINGS APPLY
    // =========================

    private Sound GetSound(string id)
    {
        if (soundMap.TryGetValue(id, out Sound sound))
            return sound;

        Debug.LogWarning("[SoundManager] Ses bulunamadı: " + id);
        return null;
    }

    private void ApplyAllSettings()
    {
        ApplySfxSettings();
        
        Sound musicSound = GetSound(MUSIC_ID);
        if (musicSound != null)
        {
            ApplyMusicSettings(musicSound);
        }

        foreach (LoopAudio loopAudio in activeLoops.Values)
        {
            ApplyLoopSettings(loopAudio);
        }
    }

    private void ApplySfxSettings()
    {
        if (sfxSource == null) return;
        
        sfxSource.mute = isMuted;
    }

    private void ApplyMusicSettings(Sound sound)
    {
        if (musicSource == null) return;
        
        float soundVolume = sound != null ? sound.volume : 1f;
        musicSource.volume = masterVolume * musicVolume * soundVolume;
        musicSource.mute = isMuted;
    }

    private void ApplyLoopSettings(LoopAudio loopAudio)
    {
        if (loopAudio == null || loopAudio.source == null)
            return;

        loopAudio.source.volume =
            masterVolume *
            loopVolume *
            loopAudio.sound.volume *
            loopAudio.volumeScale;

        loopAudio.source.mute = isMuted;
    }
}