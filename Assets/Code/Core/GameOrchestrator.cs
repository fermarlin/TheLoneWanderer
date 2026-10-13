using UnityEngine;
using UnityEngine.InputSystem;


//Este script se encarga de gestionar el juego entero, entre otras cosas la pausa y los eventos para pedir que se desbloqueen cosas
public class GameOrchestrator : MonoBehaviour
{
    public static GameOrchestrator Instance { get; private set; }

    [SerializeField] private GameEventsSO gameEvents;
    [SerializeField] private InputActionReference pauseAction;

    public bool GamePaused { get; private set; }
    public GameEventsSO Events => gameEvents;

    private bool runtimeEvents;
    private float timeScaleBeforePause = 1f;
    private InputAction pauseInputAction;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (Instance != this)
            return;

        //Esto lo he puesto porque el input action lo tiene el player, 
        // cuando cambie de mapa es posible que lo pierda, asi que para curarnos en salud lo clona
        pauseInputAction = pauseAction.action.Clone();

        pauseInputAction.performed += PauseInput;
        pauseInputAction.Enable();
    }

    private void OnDisable()
    {
        if (pauseInputAction == null)
            return;

        pauseInputAction.performed -= PauseInput;
        pauseInputAction.Disable();
    }

    private void PauseInput(InputAction.CallbackContext context)
    {
        PauseChange();
    }

    public void PauseGame(bool paused)
    {
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

    //Control desde otros scripts o botones UI
    public void PauseGame() => PauseGame(true);
    public void ResumeGame() => PauseGame(false);
    public void PauseChange() => PauseGame(!GamePaused);

    // Solicitudes de desbloqueo
    public void UnlockDoubleJump() =>
        gameEvents?.RequestDoubleJumpUnlock();

    public void UnlockWallJump() =>
        gameEvents?.RequestWallJumpUnlock();

    public void UnlockDash() =>
        gameEvents?.RequestDashUnlock();

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        if (GamePaused)
            PauseGame(false);

        Instance = null;

        if (runtimeEvents && gameEvents != null)
            Destroy(gameEvents);
    }
}
