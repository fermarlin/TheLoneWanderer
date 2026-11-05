
using System.Collections;
using UnityEngine;

public class ZoneMusicParallax2D : MonoBehaviour
{
    [SerializeField] private Transform player;

    [SerializeField] private AudioClip leftMusic;
    [SerializeField] private GameObject leftParallax;

    [SerializeField] private AudioClip rightMusic;
    [SerializeField] private GameObject rightParallax;

    [SerializeField, Min(0f)]
    private float transitionDuration = 2f;

    [SerializeField, Range(0f, 1f)]
    private float musicVolume = 0.7f;

    [Header("Cambio de zona")]
    [SerializeField, Min(0f)]
    private float changeMargin = 0.1f;

    private AudioSource leftSource;
    private AudioSource rightSource;
    private Coroutine transitionCoroutine;

    private bool isRightZone;
    private bool initialized;

    private void Awake()
    {
        leftSource = CreateMusicSource(leftMusic);
        rightSource = CreateMusicSource(rightMusic);
    }

    private void Start()
    {
        FindPlayer();

        if (player != null)
            InitializeZone();
    }

    private void Update()
    {
        if (player == null)
        {
            FindPlayer();

            if (player == null)
                return;

            InitializeZone();
        }

        float distance = player.position.x - transform.position.x;

        if (isRightZone && distance < -changeMargin)
        {
            ChangeZone(false);
        }
        else if (!isRightZone && distance > changeMargin)
        {
            ChangeZone(true);
        }
    }

    private void FindPlayer()
    {
        if (player != null)
            return;

        PlayerController controller =
            FindFirstObjectByType<PlayerController>();

        if (controller != null)
            player = controller.transform;
    }

    private AudioSource CreateMusicSource(AudioClip clip)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();

        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;

        return source;
    }

    private void InitializeZone()
    {
        isRightZone = player.position.x >= transform.position.x;
        initialized = true;

        UpdateParallax();

        AudioSource activeSource = isRightZone
            ? rightSource
            : leftSource;

        if (activeSource.clip != null)
        {
            activeSource.volume = musicVolume;
            activeSource.Play();
        }
    }

    private void ChangeZone(bool right)
    {
        if (!initialized || isRightZone == right)
            return;

        isRightZone = right;

        UpdateParallax();

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine =
            StartCoroutine(CrossfadeMusic());
    }

    private void UpdateParallax()
    {
        if (leftParallax != null)
            leftParallax.SetActive(!isRightZone);

        if (rightParallax != null)
            rightParallax.SetActive(isRightZone);
    }

    private IEnumerator CrossfadeMusic()
    {
        AudioSource activeSource = isRightZone
            ? rightSource
            : leftSource;

        AudioSource inactiveSource = isRightZone
            ? leftSource
            : rightSource;

        if (activeSource.clip != null &&
            !activeSource.isPlaying)
        {
            activeSource.Play();
        }

        float startActiveVolume = activeSource.volume;
        float startInactiveVolume = inactiveSource.volume;

        float elapsed = 0f;

        if (transitionDuration > 0f)
        {
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(
                    elapsed / transitionDuration
                );

                activeSource.volume = Mathf.Lerp(
                    startActiveVolume,
                    activeSource.clip != null ? musicVolume : 0f,
                    t
                );

                inactiveSource.volume = Mathf.Lerp(
                    startInactiveVolume,
                    0f,
                    t
                );

                yield return null;
            }
        }

        activeSource.volume =
            activeSource.clip != null ? musicVolume : 0f;

        inactiveSource.volume = 0f;
        inactiveSource.Stop();

        transitionCoroutine = null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        Vector3 top = transform.position + Vector3.up * 20f;
        Vector3 bottom = transform.position + Vector3.down * 20f;

        Gizmos.DrawLine(top, bottom);
    }
}
