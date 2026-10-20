using UnityEngine;
using UnityEngine.InputSystem;

//Gestiona el estado de pausa y las peticiones de desbloqueo.

[DefaultExecutionOrder(-200)]
public class GameOrchestrator : MonoBehaviour
{
    public static GameOrchestrator Instance { get; private set; }

    [SerializeField] private GameEventsSO gameEvents;
    [SerializeField] private InputActionReference pauseAction;

    public bool GamePaused { get; private set; }
    public bool IsPauseLocked => pauseLocks > 0;
    public GameEventsSO Events => gameEvents;

    private bool runtimeEvents;
    private float timeScaleBeforePause = 1f;
    private InputAction pauseInputAction;
    private int pauseLocks;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (gameEvents == null)
        {
            gameEvents = ScriptableObject.CreateInstance<GameEventsSO>();
            runtimeEvents = true;
        }

        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (Instance != this)
            return;

        if (pauseAction != null && pauseAction.action != null)
        {
            pauseInputAction = pauseAction.action.Clone();
            pauseInputAction.performed += PauseInput;
            pauseInputAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (Instance != this)
            return;

        if (pauseInputAction != null)
        {
            pauseInputAction.performed -= PauseInput;
            pauseInputAction.Disable();
            pauseInputAction.Dispose();
            pauseInputAction = null;
        }

        pauseLocks = 0;
        if (GamePaused)
            PauseGame(false);
    }

    private void PauseInput(InputAction.CallbackContext context)
    {
        PauseChange();
    }

    public void AcquirePauseLock()
    {
        pauseLocks++;
    }

    public void ReleasePauseLock()
    {
        pauseLocks = Mathf.Max(0, pauseLocks - 1);
    }

    public void PauseGame(bool paused)
    {
        if (!paused && IsPauseLocked)
            return;

        if (GamePaused == paused)
            return;

        GamePaused = paused;

        if (paused)
        {
            timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = timeScaleBeforePause;
        }

        gameEvents?.NotifyPauseChanged(paused);
    }

    public void PauseGame() => PauseGame(true);
    public void ResumeGame() => PauseGame(false);
    public void PauseChange()
    {
        if (!IsPauseLocked)
            PauseGame(!GamePaused);
    }

    public void UnlockDoubleJump() => gameEvents?.RequestDoubleJumpUnlock();
    public void UnlockWallJump() => gameEvents?.RequestWallJumpUnlock();
    public void UnlockDash() => gameEvents?.RequestDashUnlock();

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        pauseLocks = 0;
        if (GamePaused)
            PauseGame(false);

        Instance = null;

        if (runtimeEvents && gameEvents != null)
            Destroy(gameEvents);
    }
}
