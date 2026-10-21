using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Gestiona checkpoints y la reaparicion
public sealed class CheckpointManager : MonoBehaviour
{
    [SerializeField] private GameEventsSO gameEvents;
    [SerializeField] private PlayerHealth player;

    [SerializeField] private Transform initialSpawnPoint;
    [SerializeField, Min(0f)] private float respawnDelay = 0f;

    [SerializeField, Min(0f)] private float fadeOutSeconds = 0.35f;
    [SerializeField, Min(0f)] private float fadeInSeconds = 0.35f;

    private Health health;
    private Rigidbody2D playerRigidbody;
    private PlayerController controller;
    private PlayerInput playerInput;
    private PlayerAnimator playerAnimator;
    private PlayerCombo playerCombo;
    private PlayerShieldThrow playerShield;

    private Checkpoint2D activeCheckpoint;
    private Vector3 initialPosition;
    private Coroutine respawnCoroutine;
    private Coroutine fadeInCoroutine;
    private bool dying;


    private const float RestoreInputAtFadeProgress = 0.5f;
    private bool physicsRestored;
    private bool controlsRestored;

    private bool controllerWasEnabled;
    private bool inputWasActive;
    private string actionMapBeforeDeath;
    private bool bodyWasSimulated;
    private bool comboShouldBeEnabled;
    private bool shieldShouldBeEnabled;
    private bool comboInitiallyEnabled;
    private bool shieldInitiallyEnabled;
    private GameOrchestrator deathPauseLockOwner;

    public PlayerHealth Player => player;
    public Checkpoint2D ActiveCheckpoint => activeCheckpoint;

    private void Awake()
    {
        if (player == null || gameEvents == null)
        {
            Debug.LogError("[CheckpointManager] Asigna PlayerHealth y GameEventsSO.", this);
            enabled = false;
            return;
        }

        health = player.GetComponent<Health>();
        playerRigidbody = player.GetComponent<Rigidbody2D>();
        controller = player.GetComponent<PlayerController>();
        playerInput = player.GetComponent<PlayerInput>();
        playerAnimator = player.GetComponentInChildren<PlayerAnimator>(true);
        playerCombo = player.GetComponent<PlayerCombo>();
        playerShield = player.GetComponent<PlayerShieldThrow>();

        if (health == null || playerRigidbody == null || controller == null)
        {
            enabled = false;
            return;
        }


        comboInitiallyEnabled = playerCombo != null && playerCombo.enabled;
        shieldInitiallyEnabled = playerShield != null && playerShield.enabled;

        initialPosition = initialSpawnPoint != null
            ? initialSpawnPoint.position
            : player.transform.position;
    }

    private void OnEnable()
    {
        if (gameEvents != null)
            gameEvents.OnPlayerDied += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        if (gameEvents != null)
            gameEvents.OnPlayerDied -= HandlePlayerDeath;

        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }

        if (fadeInCoroutine != null)
        {
            StopCoroutine(fadeInCoroutine);
            fadeInCoroutine = null;
        }

        ScreenFader fader = ScreenFader.Current;
        if (fader != null)
            fader.Release(this, true);

        if (dying)
            ReleasePlayerAfterRespawn();
    }

    public void ActivateCheckpoint(Checkpoint2D checkpoint)
    {
        if (checkpoint == null || checkpoint == activeCheckpoint)
            return;

        if (activeCheckpoint != null)
            activeCheckpoint.SetActive(false);

        activeCheckpoint = checkpoint;
        activeCheckpoint.SetActive(true);
    }

    private void HandlePlayerDeath()
    {
        if (dying || !isActiveAndEnabled)
            return;

        dying = true;
        physicsRestored = false;
        controlsRestored = false;
        deathPauseLockOwner = GameOrchestrator.Instance;
        if (deathPauseLockOwner != null)
            deathPauseLockOwner.AcquirePauseLock();
        LockPlayerForDeath();
        respawnCoroutine = StartCoroutine(RespawnRoutine());
    }

    private void LockPlayerForDeath()
    {
        controllerWasEnabled = controller != null && controller.enabled;
        inputWasActive = playerInput != null && playerInput.inputIsActive;
        actionMapBeforeDeath = playerInput != null && playerInput.currentActionMap != null
            ? playerInput.currentActionMap.name : null;
        bodyWasSimulated = playerRigidbody != null && playerRigidbody.simulated;

        comboShouldBeEnabled = playerCombo != null && (playerCombo.enabled || comboInitiallyEnabled);
        shieldShouldBeEnabled = playerShield != null && (playerShield.enabled || shieldInitiallyEnabled);

        if (playerInput != null)
            playerInput.DeactivateInput();
        if (controller != null)
            controller.enabled = false;
        if (playerCombo != null)
            playerCombo.enabled = false;
        if (playerShield != null)
            playerShield.enabled = false;

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
            playerRigidbody.simulated = false;
        }
    }

    private IEnumerator RespawnRoutine()
    {
        float deathSeconds = playerAnimator != null
            ? playerAnimator.DeathAnimationDuration : 1f;
        yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, deathSeconds));

        if (respawnDelay > 0f)
            yield return new WaitForSecondsRealtime(respawnDelay);

        ScreenFader fader = ScreenFader.Instance;
        if (fader == null)
        {
            ReleasePlayerAfterRespawn();
            respawnCoroutine = null;
            yield break;
        }

        while (!fader.TryAcquire(this))
            yield return null;

        yield return fader.FadeOut(this, fadeOutSeconds);

        RespawnAtBlack();

        RestorePhysicsWhileBlack();
        if (Time.timeScale > 0f && bodyWasSimulated && controllerWasEnabled)
            yield return new WaitForFixedUpdate();
        else
            yield return null;

        if (playerAnimator != null)
            playerAnimator.ResetAfterRespawn();

        yield return null;

        if (fadeInSeconds > 0f)
        {

            fadeInCoroutine = StartCoroutine(fader.FadeIn(this, fadeInSeconds));
            yield return new WaitForSecondsRealtime(fadeInSeconds * RestoreInputAtFadeProgress);
            RestoreControlsDuringFadeIn();
            yield return fadeInCoroutine;
            fadeInCoroutine = null;
        }
        else
        {
            yield return fader.FadeIn(this, 0f);
            RestoreControlsDuringFadeIn();
        }

        fader.Release(this);
        CompleteRespawn();
        respawnCoroutine = null;
    }

    private void RespawnAtBlack()
    {
        if (player == null || health == null)
            return;

        Vector3 destination = activeCheckpoint != null
            ? activeCheckpoint.RespawnPosition : initialPosition;
        destination.z = player.transform.position.z;

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
            playerRigidbody.position = new Vector2(destination.x, destination.y);
        }

        player.transform.position = destination;
        Physics2D.SyncTransforms();
        health.RestoreHealth();

    }

    private void RestorePhysicsWhileBlack()
    {
        if (!dying || physicsRestored)
            return;

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
            playerRigidbody.simulated = bodyWasSimulated;
        }

        if (controller != null)
            controller.enabled = controllerWasEnabled;

        physicsRestored = true;
    }


    private void RestoreControlsDuringFadeIn()
    {
        if (!dying || controlsRestored)
            return;

        RestorePhysicsWhileBlack();

        if (playerCombo != null)
            playerCombo.enabled = comboShouldBeEnabled;
        if (playerShield != null)
            playerShield.enabled = shieldShouldBeEnabled;

        if (playerInput != null && inputWasActive)
        {
            playerInput.ActivateInput();
            if (!string.IsNullOrEmpty(actionMapBeforeDeath) &&
                (playerInput.currentActionMap == null ||
                 playerInput.currentActionMap.name != actionMapBeforeDeath))
                playerInput.SwitchCurrentActionMap(actionMapBeforeDeath);
        }

        controlsRestored = true;
    }

    private void CompleteRespawn()
    {
        if (!dying)
            return;

        RestoreControlsDuringFadeIn();
        dying = false;
        physicsRestored = false;
        controlsRestored = false;

        if (deathPauseLockOwner != null)
        {
            deathPauseLockOwner.ReleasePauseLock();
            deathPauseLockOwner = null;
        }
    }

    private void ReleasePlayerAfterRespawn() => CompleteRespawn();
}
