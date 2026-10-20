using System.Collections;
using UnityEngine;
using UnityEngine.UI;

//Este script lo he creado porque lo tenia en el scenetransition pero como queria que fuera mas modular
//lo puse asi, asi se puede usar en el respawn
[DefaultExecutionOrder(-250)]
public sealed class ScreenFader : MonoBehaviour
{
    private static ScreenFader instance;

    public static ScreenFader Instance
    {
        get
        {
            if (instance == null && Application.isPlaying)
            {
                GameObject go = new GameObject("ScreenFader");
                instance = go.AddComponent<ScreenFader>();
            }
            return instance;
        }
    }

    public static ScreenFader Current => instance;

    public bool IsBusy => reserved;

    private CanvasGroup fadeGroup;
    private Object currentOwner;
    private bool reserved;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateCanvas();
    }


    public bool TryAcquire(Object requester)
    {
        if (requester == null || !isActiveAndEnabled || fadeGroup == null)
            return false;

        if (reserved)
        {
            if (currentOwner != null)
                return false;

            ClearState();
        }

        reserved = true;
        currentOwner = requester;
        fadeGroup.blocksRaycasts = true;
        return true;
    }

    public IEnumerator FadeOut(Object requester, float seconds)
    {
        if (!IsOwner(requester))
            yield break;

        yield return FadeTo(requester, 1f, seconds);
    }

    public IEnumerator FadeIn(Object requester, float seconds)
    {
        if (!IsOwner(requester))
            yield break;

        yield return FadeTo(requester, 0f, seconds);
    }


    public void Release(Object requester, bool clearImmediately = false)
    {
        if (!IsOwner(requester))
            return;

        if (clearImmediately)
            fadeGroup.alpha = 0f;

        currentOwner = null;
        reserved = false;
        fadeGroup.blocksRaycasts = false;
    }

    private bool IsOwner(Object requester)
    {
        return requester != null && reserved && currentOwner == requester;
    }

    private IEnumerator FadeTo(Object requester, float target, float seconds)
    {
        float start = fadeGroup.alpha;
        seconds = Mathf.Max(0f, seconds);

        if (seconds <= 0f)
        {
            if (IsOwner(requester))
                fadeGroup.alpha = target;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < seconds && IsOwner(requester))
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / seconds);
            fadeGroup.alpha = Mathf.Lerp(start, target, progress);
            yield return null;
        }

        if (IsOwner(requester))
            fadeGroup.alpha = target;
    }

    private void ClearState()
    {
        currentOwner = null;
        reserved = false;
        if (fadeGroup != null)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
        }
    }

    private void CreateCanvas()
    {
        GameObject canvasObject = new GameObject("FadeCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        fadeGroup = canvasObject.GetComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable = false;

        GameObject imageObject = new GameObject("BlackFade", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        ClearState();
        instance = null;
    }
}
