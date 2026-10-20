using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

//El sistema de cambio de escena, tambien sirve para cerrar el jeugo
[DefaultExecutionOrder(-150)]
public sealed class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager instance;

    public static SceneTransitionManager Instance
    {
        get
        {
            if (instance == null && Application.isPlaying)
            {
                GameObject go = new GameObject("SceneTransitionManager");
                instance = go.AddComponent<SceneTransitionManager>();
            }
            return instance;
        }
    }

    [SerializeField, Min(0f)] private float fadeOutSeconds = 0.35f;
    [SerializeField, Min(0f)] private float fadeInSeconds = 0.35f;

    public bool IsTransitioning { get; private set; }
    public static event Action<string> OnTransitionStarted;
    public static event Action<string> OnTransitionFinished;

    private PlayerLock outgoingPlayer;
    private PlayerLock incomingPlayer;

    private sealed class PlayerLock
    {
        public PlayerController controller;
        public PlayerInput input;
        public Rigidbody2D body;
        public bool controllerEnabled;
        public bool inputWasActive;
        public bool bodyWasSimulated;
        public string actionMapName;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public void QuitGame()
    {
        Application.Quit();
    }


    public void ChangeScene(string sceneName)
    {
        if (!isActiveAndEnabled || IsTransitioning)
            return;


        GameOrchestrator orchestrator = GameOrchestrator.Instance;

        ScreenFader fader = ScreenFader.Instance;


        if (orchestrator != null && orchestrator.GamePaused)
            orchestrator.ResumeGame();

        IsTransitioning = true;
        StartCoroutine(ChangeSceneRoutine(sceneName, fader));
    }


    public bool FadeThroughBlack(Action whenBlack, Action whenFinished = null)
    {
        if (!isActiveAndEnabled || IsTransitioning)
            return false;

        ScreenFader fader = ScreenFader.Instance;
        if (fader == null || !fader.TryAcquire(this))
            return false;

        IsTransitioning = true;
        StartCoroutine(FadeThroughBlackRoutine(fader, whenBlack, whenFinished));
        return true;
    }

    private IEnumerator FadeThroughBlackRoutine(ScreenFader fader, Action whenBlack, Action whenFinished)
    {
        yield return fader.FadeOut(this, fadeOutSeconds);

        try
        {
            whenBlack?.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }

        yield return null;
        yield return fader.FadeIn(this, fadeInSeconds);
        EndTransition();

        whenFinished?.Invoke();

    }

    private IEnumerator ChangeSceneRoutine(string sceneName, ScreenFader fader)
    {
        outgoingPlayer = LockPlayer(FindFirstObjectByType<PlayerController>());
        OnTransitionStarted?.Invoke(sceneName);

        yield return fader.FadeOut(this, fadeOutSeconds);

        AsyncOperation operation = null;

        operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        

        if (operation == null)
        {
            UnlockPlayer(outgoingPlayer);
            outgoingPlayer = null;
            yield return fader.FadeIn(this, fadeInSeconds);
            EndTransition();
            yield break;
        }

        while (!operation.isDone)
            yield return null;

        yield return null;
        incomingPlayer = LockPlayer(FindFirstObjectByType<PlayerController>());

        yield return fader.FadeIn(this, fadeInSeconds);

        UnlockPlayer(incomingPlayer);
        incomingPlayer = null;
        outgoingPlayer = null; 
        EndTransition();
        OnTransitionFinished?.Invoke(sceneName);
    }

    private void EndTransition()
    {
        ScreenFader fader = ScreenFader.Current;
        if (fader != null)
            fader.Release(this);
        IsTransitioning = false;
    }

    private static PlayerLock LockPlayer(PlayerController player)
    {
        if (player == null)
            return null;

        PlayerLock state = new PlayerLock
        {
            controller = player,
            input = player.GetComponent<PlayerInput>(),
            body = player.GetComponent<Rigidbody2D>(),
            controllerEnabled = player.enabled
        };

        if (state.input != null)
        {
            state.inputWasActive = state.input.inputIsActive;
            state.actionMapName = state.input.currentActionMap != null
                ? state.input.currentActionMap.name : null;
            state.input.DeactivateInput();
        }

        player.enabled = false;
        if (state.body != null)
        {
            state.bodyWasSimulated = state.body.simulated;
            state.body.linearVelocity = Vector2.zero;
            state.body.angularVelocity = 0f;
            state.body.simulated = false;
        }
        return state;
    }

    private static void UnlockPlayer(PlayerLock state)
    {
        if (state == null)
            return;

        if (state.body != null)
        {
            state.body.linearVelocity = Vector2.zero;
            state.body.angularVelocity = 0f;
            state.body.simulated = state.bodyWasSimulated;
        }
        if (state.controller != null)
            state.controller.enabled = state.controllerEnabled;
        if (state.input != null && state.inputWasActive)
        {
            state.input.ActivateInput();
            if (!string.IsNullOrEmpty(state.actionMapName) &&
                (state.input.currentActionMap == null ||
                 state.input.currentActionMap.name != state.actionMapName))
                state.input.SwitchCurrentActionMap(state.actionMapName);
        }
    }

    private void OnDisable()
    {
        if (instance != this)
            return;

        StopAllCoroutines();
        ScreenFader fader = ScreenFader.Current;
        if (fader != null)
            fader.Release(this, true);

        UnlockPlayer(incomingPlayer);
        UnlockPlayer(outgoingPlayer);
        incomingPlayer = null;
        outgoingPlayer = null;
        IsTransitioning = false;
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        ScreenFader fader = ScreenFader.Current;
        if (fader != null)
            fader.Release(this, true);
        UnlockPlayer(incomingPlayer);
        UnlockPlayer(outgoingPlayer);
        instance = null;
    }
}
