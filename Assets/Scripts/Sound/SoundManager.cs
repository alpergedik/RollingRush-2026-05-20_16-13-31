using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

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

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float loopVolume = 1f;

    private bool masterMuted;
    private bool sfxMuted;
    private bool loopMuted;

    private AudioSource sfxSource;

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

        CreateSfxSource();
        BuildSoundMap();
        ApplyAllSettings();
    }

    private void OnValidate()
    {
        masterVolume = Mathf.Clamp01(masterVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
        loopVolume = Mathf.Clamp01(loopVolume);

        if (Application.isPlaying)
            ApplyAllSettings();
    }

    private void CreateSfxSource()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    private void BuildSoundMap()
    {
        soundMap.Clear();

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
    // SFX
    // =========================

    public void PlaySFX(string id)
    {
        PlaySFX(id, 1f);
    }

    public void PlaySFX(string id, float volumeScale)
    {
        if (masterMuted || sfxMuted)
            return;

        Sound sound = GetSound(id);

        if (sound == null)
            return;

        float finalVolumeScale = sound.volume * Mathf.Clamp01(volumeScale);

        sfxSource.PlayOneShot(sound.clip, finalVolumeScale);
    }

    public void StopAllSFX()
    {
        sfxSource.Stop();
    }

    // =========================
    // LOOP
    // =========================

    public void PlayLoop(string id)
    {
        PlayLoop(id, 1f);
    }

    public void PlayLoop(string id, float volumeScale)
    {
        Sound sound = GetSound(id);

        if (sound == null)
            return;

        if (activeLoops.ContainsKey(id))
        {
            SetLoopVolumeScale(id, volumeScale);
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
    // VOLUME
    // =========================

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
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

    public float GetMasterVolume()
    {
        return masterVolume;
    }

    public float GetSFXVolume()
    {
        return sfxVolume;
    }

    public float GetLoopVolume()
    {
        return loopVolume;
    }

    // =========================
    // MUTE
    // =========================

    public void MuteAll()
    {
        masterMuted = true;
        ApplyAllSettings();
    }

    public void UnmuteAll()
    {
        masterMuted = false;
        ApplyAllSettings();
    }

    public void ToggleMuteAll()
    {
        masterMuted = !masterMuted;
        ApplyAllSettings();
    }

    public void MuteSFX()
    {
        sfxMuted = true;
        ApplyAllSettings();
    }

    public void UnmuteSFX()
    {
        sfxMuted = false;
        ApplyAllSettings();
    }

    public void ToggleMuteSFX()
    {
        sfxMuted = !sfxMuted;
        ApplyAllSettings();
    }

    public void MuteLoops()
    {
        loopMuted = true;
        ApplyAllSettings();
    }

    public void UnmuteLoops()
    {
        loopMuted = false;
        ApplyAllSettings();
    }

    public void ToggleMuteLoops()
    {
        loopMuted = !loopMuted;
        ApplyAllSettings();
    }

    // =========================
    // INTERNAL
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

        foreach (LoopAudio loopAudio in activeLoops.Values)
        {
            ApplyLoopSettings(loopAudio);
        }
    }

    private void ApplySfxSettings()
    {
        if (sfxSource == null)
            return;

        sfxSource.volume = masterVolume * sfxVolume;
        sfxSource.mute = masterMuted || sfxMuted;
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

        loopAudio.source.mute = masterMuted || loopMuted;
    }
}