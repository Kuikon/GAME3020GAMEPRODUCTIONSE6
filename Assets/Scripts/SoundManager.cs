using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [Header("BGM")]
    [SerializeField] private AudioSource bgmAudioSource;

    [Header("SE")]
    [SerializeField] private int seSourceCount = 10;
    [SerializeField] private Transform seRoot;

    [Header("Sound Data")]
    [SerializeField] private List<BGMSoundData> bgmSoundDatas;
    [SerializeField] private List<SESoundData> seSoundDatas;

    [Header("Volume")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmMasterVolume = 1f;
    [Range(0f, 1f)] public float seMasterVolume = 1f;

    private readonly List<AudioSource> seAudioSources = new List<AudioSource>();
    private int seIndex = 0;

    public static SoundManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSESources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSESources()
    {
        if (seRoot == null)
        {
            GameObject root = new GameObject("SE_AudioSources");
            root.transform.SetParent(transform);
            seRoot = root.transform;
        }

        for (int i = 0; i < seSourceCount; i++)
        {
            GameObject obj = new GameObject($"SE_Source_{i}");
            obj.transform.SetParent(seRoot);

            AudioSource source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;

            seAudioSources.Add(source);
        }
    }

    public void PlayBGM(BGMSoundData.BGM bgm)
    {
        BGMSoundData data = bgmSoundDatas.Find(x => x.bgm == bgm);
        if (data == null || data.audioClip == null)
        {
            Debug.LogWarning($"BGM data not found: {bgm}");
            return;
        }

        if (bgmAudioSource.clip == data.audioClip && bgmAudioSource.isPlaying)
            return;

        bgmAudioSource.clip = data.audioClip;
        bgmAudioSource.volume = data.volume * bgmMasterVolume * masterVolume;
        bgmAudioSource.loop = true;
        bgmAudioSource.Play();
    }

    public void StopBGM()
    {
        if (bgmAudioSource.isPlaying)
            bgmAudioSource.Stop();
    }

    public void PlaySE(SESoundData.SE se)
    {
        SESoundData data = seSoundDatas.Find(x => x.se == se);
        if (data == null || data.audioClip == null)
        {
            Debug.LogWarning($"SE data not found: {se}");
            return;
        }

        AudioSource source = GetAvailableSEAudioSource();
        source.volume = data.volume * seMasterVolume * masterVolume;
        source.pitch = 1f;
        source.PlayOneShot(data.audioClip);
    }

    private AudioSource GetAvailableSEAudioSource()
    {

        for (int i = 0; i < seAudioSources.Count; i++)
        {
            if (!seAudioSources[i].isPlaying)
                return seAudioSources[i];
        }


        AudioSource source = seAudioSources[seIndex];
        seIndex = (seIndex + 1) % seAudioSources.Count;
        return source;
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        RefreshVolumes();
    }

    public void SetBGMVolume(float value)
    {
        bgmMasterVolume = Mathf.Clamp01(value);
        RefreshVolumes();
    }

    public void SetSEVolume(float value)
    {
        seMasterVolume = Mathf.Clamp01(value);
        RefreshVolumes();
    }

    private void RefreshVolumes()
    {
        if (bgmAudioSource != null && bgmAudioSource.clip != null)
        {
            BGMSoundData currentData = bgmSoundDatas.Find(x => x.audioClip == bgmAudioSource.clip);
            float baseVolume = currentData != null ? currentData.volume : 1f;
            bgmAudioSource.volume = baseVolume * bgmMasterVolume * masterVolume;
        }
    }
}

[System.Serializable]
public class BGMSoundData
{
    public enum BGM
    {
        Title,
        Dungeon,
        Hoge,
    }

    public BGM bgm;
    public AudioClip audioClip;

    [Range(0, 1)]
    public float volume = 1f;
}

[System.Serializable]
public class SESoundData
{
    public enum SE
    {
        Jump,
        Land,
        Footstep,
        Dash,
        Place,
        Remove,
        Pick,
        Drop
    }

    public SE se;
    public AudioClip audioClip;

    [Range(0, 1)]
    public float volume = 1f;
}