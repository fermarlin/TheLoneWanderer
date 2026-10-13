using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

//E ste script gestiona  lanzamiento del escudo. Ademas bloquea el movimiento hasta recuperar el escudo.

[DefaultExecutionOrder(-60)]
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerShieldThrow : MonoBehaviour
{
    [SerializeField] private ShieldConfigSO config;
    [SerializeField] private ShieldBoomerang shieldPrefab;
    [SerializeField, Min(0f)] private float throwDelay = 0.2f;


    public event Action OnShieldPreparing;
    public event Action OnShieldThrown;
    public event Action OnShieldRecovered;

    public bool IsShieldAway => activeShield != null;
    public bool IsPreparingThrow => isPreparingThrow;

    private PlayerController player;
    private PlayerInput playerInput;
    private InputAction specialAction;
    private bool actionsInitialized;
    private PlayerCombo combo;
    private ShieldBoomerang activeShield;
    private Coroutine waitCoroutine;
    private bool isPreparingThrow;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        playerInput = GetComponent<PlayerInput>();
        combo = GetComponent<PlayerCombo>();
    }

    private void Start()
    {
        actionsInitialized = true;
        SubscribeToSpecial();
    }

    private void SubscribeToSpecial()
    {
        if (!isActiveAndEnabled)
            return;
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();
        if (playerInput == null || playerInput.actions == null)
            return;

        specialAction = playerInput.actions.FindAction("Player/Special", true);
        specialAction.performed += HandleSpecialInput;
    }

    private void OnEnable()
    {
        if (actionsInitialized)
            SubscribeToSpecial();
    }

    private void HandleSpecialInput(InputAction.CallbackContext context)
    {
        TryThrow();
    }

    public void TryThrow()
    {
        GameOrchestrator orchestrator = GameOrchestrator.Instance;
        if (!isActiveAndEnabled || player == null || !player.enabled || config == null ||
            shieldPrefab == null || isPreparingThrow || IsShieldAway ||
            player.IsShieldInFlight || !player.IsGrounded || player.IsDashing ||
            Time.timeScale <= 0f || (orchestrator != null && orchestrator.GamePaused))
            return;

        int direction = player.FacingDirection < 0 ? -1 : 1;

        if (combo != null)
            combo.CancelForShield();

        isPreparingThrow = true;
        player.SetShieldInFlight(true);

        OnShieldPreparing?.Invoke();

        if (throwDelay <= 0f)
            CreateShield(direction);
        else
            waitCoroutine = StartCoroutine(WaitAndThrow(direction));
    }

    private IEnumerator WaitAndThrow(int direction)
    {

        yield return new WaitForSeconds(throwDelay);
        waitCoroutine = null;

        if (!isPreparingThrow)
            yield break;

        if (!isActiveAndEnabled || player == null || !player.enabled ||
            config == null || shieldPrefab == null)
        {
            CancelPreparation();
            yield break;
        }

        CreateShield(direction);
    }

    private void CreateShield(int direction)
    {
        if (!isPreparingThrow)
            return;

        Vector3 origin = transform.position + new Vector3(direction * 0.65f, 0.3f, 0f);

        ShieldBoomerang newShield = Instantiate(shieldPrefab, origin, Quaternion.identity);
        if (newShield == null)
        {
            CancelPreparation();
            return;
        }

        activeShield = newShield;
        isPreparingThrow = false;

        newShield.Initialize(this, config, direction, origin);

        //Solo se notifica que el escudo ha salido
        OnShieldThrown?.Invoke();
    }

    private void CancelPreparation()
    {
        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }

        bool wasPreparing = isPreparingThrow;
        isPreparingThrow = false;

        if (player != null && player.IsShieldInFlight)
            player.SetShieldInFlight(false);

        if (wasPreparing)
            OnShieldRecovered?.Invoke();
    }

    public void RecoverShield(ShieldBoomerang shield)
    {
        if (activeShield != shield)
            return;

        activeShield = null;
        if (player != null)
            player.SetShieldInFlight(false);
        OnShieldRecovered?.Invoke();
    }

    //Esto es para que coja el escudo en la posicion del jugador
    public Vector2 CatchPoint => transform.position;


    private void OnDisable()
    {
        if (specialAction != null)
            specialAction.performed -= HandleSpecialInput;
        specialAction = null;
        if (isPreparingThrow)
            CancelPreparation();

        ShieldBoomerang shield = activeShield;
        if (shield != null)
        {
            RecoverShield(shield);
            Destroy(shield.gameObject);
        }
        else if (player != null && player.IsShieldInFlight)
        {
            player.SetShieldInFlight(false);
        }
    }
}
