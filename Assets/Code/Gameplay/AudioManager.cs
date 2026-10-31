
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public sealed class AudioManager : MonoBehaviour
{
    private static AudioManager instance;

    public static AudioManager Instance
    {
        get
        {
            if (instance == null && Application.isPlaying)
            {
                instance = FindFirstObjectByType<AudioManager>();

                if (instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    instance = go.AddComponent<AudioManager>();
                }
            }

            return instance;
        }
    }

    [Header("Biblioteca de sonidos")]
    [SerializeField] private AudioLibrarySO audioLibrary;

    [Header("Volumen global")]
    [SerializeField, Range(0f, 1f)]
    private float masterVolume = 1f;

    [Header("Variacion de pitch")]
    [SerializeField, Range(0.1f, 3f)]
    private float minPitch = 0.85f;

    [SerializeField, Range(0.1f, 3f)]
    private float maxPitch = 1.10f;

    private readonly List<AudioSource> audioSources =
        new List<AudioSource>();

    public float MasterVolume => masterVolume;

    private void Awake()
    {
        if (instance != null && instance != this)
        {

            if (instance.audioLibrary == null && audioLibrary != null)
                instance.audioLibrary = audioLibrary;

            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SetMasterVolume(masterVolume);
    }

    public void Play(string soundId)
    {
        if (audioLibrary == null)
        {
            Debug.LogWarning(
                "[AudioManager] No hay ninguna AudioLibrary asignada.");
            return;
        }

        AudioLibrarySO.Sound sound =
            audioLibrary.GetSound(soundId);

        if (sound == null || sound.clip == null)
        {
            Debug.LogWarning(
                "[AudioManager] No existe el sonido: " + soundId);
            return;
        }

        Play(sound.clip, sound.volume);
    }

    public void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
            return;

        AudioSource source = GetAvailableSource();

        source.Stop();
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);

        source.pitch = Random.Range(
            Mathf.Min(minPitch, maxPitch),
            Mathf.Max(minPitch, maxPitch));

        source.Play();
    }

    private AudioSource GetAvailableSource()
    {
        foreach (AudioSource source in audioSources)
        {
            if (source != null && !source.isPlaying)
                return source;
        }

        AudioSource newSource = gameObject.AddComponent<AudioSource>();

        newSource.playOnAwake = false;
        newSource.loop = false;

        newSource.spatialBlend = 0f;

        audioSources.Add(newSource);

        return newSource;
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        AudioListener.volume = masterVolume;
    }

    public void Mute()
    {
        SetMasterVolume(0f);
    }

    public void Unmute()
    {
        SetMasterVolume(1f);
    }

    private void OnValidate()
    {
        minPitch = Mathf.Max(0.1f, minPitch);
        maxPitch = Mathf.Max(minPitch, maxPitch);

        if (Application.isPlaying && instance == this)
            SetMasterVolume(masterVolume);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
