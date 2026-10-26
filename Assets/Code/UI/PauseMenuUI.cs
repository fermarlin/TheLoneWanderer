
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//Este script sirve para el menu de pausa, para que se active y desactive
public sealed class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;

    [SerializeField] private string mainMenuScene = "MainMenu";

    private GameEventsSO subscribedEvents;
    private bool leavingScene;

    private void Awake()
    {
        if (menuRoot != null)
            menuRoot.SetActive(false);
    }

    private void OnEnable()
    {
        leavingScene = false;

        if (menuRoot == null ||
            resumeButton == null ||
            mainMenuButton == null)
        {
            return;
        }

        resumeButton.onClick.RemoveListener(Resume);
        mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);

        resumeButton.onClick.AddListener(Resume);
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        SceneTransitionManager.OnTransitionStarted -= HandleTransitionStarted;
        SceneTransitionManager.OnTransitionStarted += HandleTransitionStarted;

        SubscribeToEvents();
    }

    private void Start()
    {
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        GameOrchestrator orchestrator = GameOrchestrator.Instance;

        if (orchestrator == null || orchestrator.Events == null)
        {
            return;
        }

        if (subscribedEvents != orchestrator.Events)
        {
            if (subscribedEvents != null)
                subscribedEvents.OnPauseChanged -= HandlePauseChanged;

            subscribedEvents = orchestrator.Events;
        }

        subscribedEvents.OnPauseChanged -= HandlePauseChanged;
        subscribedEvents.OnPauseChanged += HandlePauseChanged;

        HandlePauseChanged(orchestrator.GamePaused);
    }

    private void OnDisable()
    {
        if (subscribedEvents != null)
        {
            subscribedEvents.OnPauseChanged -= HandlePauseChanged;
            subscribedEvents = null;
        }

        SceneTransitionManager.OnTransitionStarted -= HandleTransitionStarted;

        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(Resume);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);

        if (menuRoot != null)
            menuRoot.SetActive(false);
    }

    private void HandlePauseChanged(bool paused)
    {
        GameOrchestrator orchestrator = GameOrchestrator.Instance;

        bool show = paused &&
                    !leavingScene &&
                    orchestrator != null &&
                    !orchestrator.IsPauseLocked;

        if (menuRoot != null)
            menuRoot.SetActive(show);

        if (show && resumeButton != null &&
            EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(
                resumeButton.gameObject);
        }
    }

    private void HandleTransitionStarted(string sceneName)
    {
        leavingScene = true;

        if (menuRoot != null)
            menuRoot.SetActive(false);
    }

    public void Resume()
    {
        GameOrchestrator orchestrator = GameOrchestrator.Instance;

        if (orchestrator == null ||
            orchestrator.IsPauseLocked ||
            leavingScene)
            return;

        orchestrator.ResumeGame();
    }

    public void ReturnToMainMenu()
    {
        if (leavingScene)
            return;

        if (string.IsNullOrWhiteSpace(mainMenuScene) ||
            !Application.CanStreamedLevelBeLoaded(mainMenuScene))
        {
            return;
        }

        GameOrchestrator orchestrator = GameOrchestrator.Instance;

        if (orchestrator != null &&
            orchestrator.IsPauseLocked)
            return;

        SceneTransitionManager transitions =
            SceneTransitionManager.Instance;

        if (transitions == null ||
            transitions.IsTransitioning)
            return;

        transitions.ChangeScene(mainMenuScene);
    }
}
