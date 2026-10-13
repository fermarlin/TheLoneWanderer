using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
//Este script bebe los datos que le ponemos en comboConfig
[DefaultExecutionOrder(50)] //Esto es para que se ejecute mas tarde y no nos de errores
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerCombo : MonoBehaviour
{
    public enum ComboType { Normal, Running, AirUp, AirDown, AirSide }
    [SerializeField] private ComboConfigSO config;
    [SerializeField] private GameEventsSO gameEvents;
    public event Action<int> OnAttackStarted;
    public event Action<int, ComboType> OnAttackStartedWithType;
    public event Action OnComboEnded;
    public event Action<bool> OnComboFinished;
    public bool IsAttacking => attackInProgress;
    public bool IsInCombo => attackIndex >= 0;
    public int CurrentAttack => attackIndex >= 0 ? attackIndex + 1 : 0;
    public ComboType CurrentCombo => activeComboType;
    private PlayerController player;
    private PlayerInput playerInput;
    private InputAction attackAction;
    private bool actionsInitialized;
    private GameEventsSO subscribedEvents;
    private ComboType activeComboType = ComboType.Normal;
    private int attackIndex = -1;
    private int attackDirection = 1;
    private float attackElapsedTime;
    private float remainingWindow;
    private bool attackInProgress;
    private bool nextAttackQueued;
    private bool hitApplied;
    private bool isPaused;
    private bool pendingAttack;
    private readonly HashSet<ITakeDamage> hitTargets = new HashSet<ITakeDamage>();
    private bool IsAirCombo =>
        activeComboType == ComboType.AirUp ||
        activeComboType == ComboType.AirDown ||
        activeComboType == ComboType.AirSide;
    private ComboConfigSO.Golpe[] CurrentSequence
    {
        get
        {
            switch (activeComboType)
            {
                case ComboType.Running: return config.golpesCorriendo;
                case ComboType.AirUp: return upSequence;
                case ComboType.AirDown: return downSequence;
                case ComboType.AirSide: return config.golpesAereosLaterales;
                default: return config.golpes;
            }
        }
    }
//Evita crear arrays nuevas
    private ComboConfigSO.Golpe[] upSequence;
    private ComboConfigSO.Golpe[] downSequence;
    private GameEventsSO Events
    {
        get
        {
            GameOrchestrator orchestrator = GameOrchestrator.Instance;
            return orchestrator != null ? orchestrator.Events : gameEvents;
        }
    }
    private void Awake()
    {
        player = GetComponent<PlayerController>();
        playerInput = GetComponent<PlayerInput>();
        upSequence = new[] { config.ataqueAereoArriba };
        downSequence = new[] { config.ataqueAereoAbajo };
    }
    private static bool IsValidSequence(ComboConfigSO.Golpe[] sequence, int length)
    {
        if (sequence == null || sequence.Length != length)
            return false;
        foreach (ComboConfigSO.Golpe attack in sequence)
            if (attack == null) return false;
        return true;
    }
    private void Start()
    {
        actionsInitialized = true;
        SubscribeToAttack();
    }
    private void SubscribeToAttack()
    {
        if (!isActiveAndEnabled)
            return;
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();
        attackAction = playerInput.actions.FindAction("Player/Attack", true);
        attackAction.performed += HandleAttackInput;
    }
    private void OnEnable()
    {
        if (player == null)
            player = GetComponent<PlayerController>();
        if (actionsInitialized)
            SubscribeToAttack();
        if (player != null)
        {
            player.OnJump += InterruptCombo;
            player.OnWallJump += InterruptCombo;
            player.OnDash += InterruptCombo;
        }
        subscribedEvents = Events;
        if (subscribedEvents != null)
            subscribedEvents.OnPauseChanged += HandlePauseChanged;
        GameOrchestrator orchestrator = GameOrchestrator.Instance;
        HandlePauseChanged(orchestrator != null && orchestrator.GamePaused);
    }
    private void OnDisable()
    {
        if (attackAction != null)
            attackAction.performed -= HandleAttackInput;
        attackAction = null;
        pendingAttack = false;
        if (player != null)
        {
            player.OnJump -= InterruptCombo;
            player.OnWallJump -= InterruptCombo;
            player.OnDash -= InterruptCombo;
        }
        if (subscribedEvents != null)
            subscribedEvents.OnPauseChanged -= HandlePauseChanged;
        subscribedEvents = null;
        InterruptCombo();
    }
    private void Update()
    {
        if (config == null || isPaused || player == null)
            return;
        if (player.IsShieldInFlight)
        {
            pendingAttack = false;
            FinishCombo(true);
            return;
        }
        if (player.IsDashing)
        {
            pendingAttack = false;
            InterruptCombo();
            return;
        }
        if (IsInCombo)
        {
            if ((IsAirCombo && player.IsGrounded) || (!IsAirCombo && !player.IsGrounded))
                FinishCombo(false);
        }
        if (attackInProgress)
        {
            ComboConfigSO.Golpe attack = CurrentSequence[attackIndex];
            if (attack == null)
            {
                FinishCombo(false);
                return;
            }
            attackElapsedTime += Time.deltaTime;
            if (!hitApplied && attackElapsedTime >= attack.momentoImpacto)
            {
                hitApplied = true;
                ApplyHit(attack.danio);

                if (!attackInProgress)
                    return;
            }
            if (attackElapsedTime >= Mathf.Max(0.05f, attack.duracion))
                FinishAttack();
        }
        else if (IsInCombo)
        {
            remainingWindow -= Time.deltaTime;
            if (remainingWindow <= 0f)
                FinishCombo(false);
        }
        if (pendingAttack)
        {
            pendingAttack = false;
            RequestAttack();
        }
    }
    private void RequestAttack()
    {
        if (attackInProgress)
        {
            ComboConfigSO.Golpe[] sequence = CurrentSequence;
            if (attackIndex >= sequence.Length - 1)
                return;
            ComboConfigSO.Golpe attack = sequence[attackIndex];
            if (attack != null &&
                Mathf.Max(0.05f, attack.duracion) - attackElapsedTime <= config.anticipacionEntrada)
                nextAttackQueued = true;
            return;
        }
        if (IsInCombo && attackIndex < CurrentSequence.Length - 1 && remainingWindow > 0f)
        {
            StartAttack(attackIndex + 1);
            return;
        }
        ComboType type = DetectComboType();
        if (!IsComboTypeAvailable(type))
            return;
        activeComboType = type;
        attackDirection = player.FacingDirection >= 0 ? 1 : -1;
        StartAttack(0);
    }
    private bool IsComboTypeAvailable(ComboType type)
    {
        switch (type)
        {
            case ComboType.Normal: return IsValidSequence(config.golpes, 3);
            case ComboType.Running: return IsValidSequence(config.golpesCorriendo, 2);
            case ComboType.AirUp: return config.ataqueAereoArriba != null;
            case ComboType.AirDown: return config.ataqueAereoAbajo != null;
            case ComboType.AirSide: return IsValidSequence(config.golpesAereosLaterales, 2);
            default: return false;
        }
    }
    private ComboType DetectComboType()
    {
        if (!player.IsGrounded)
        {
    //Esto lo voy a tener que cambiar, porque lee dos veces el mando del player pero de momento se queda asi
            float verticalAxis = player.InputDirection.y;
            if (verticalAxis >= config.umbralApuntadoVertical)
                return ComboType.AirUp;
            if (verticalAxis <= -config.umbralApuntadoVertical)
                return ComboType.AirDown;
            return ComboType.AirSide;
        }
        if (IsValidSequence(config.golpesCorriendo, 2) &&
            Mathf.Abs(player.HorizontalMovement) >= config.umbralMovimientoParaCorrer)
            return ComboType.Running;
        return ComboType.Normal;
    }
    private void StartAttack(int index)
    {
        attackIndex = index;
        attackInProgress = true;
        attackElapsedTime = 0f;
        remainingWindow = 0f;
        nextAttackQueued = false;
        hitApplied = false;
        int attackNumber = index + 1;
        OnAttackStartedWithType?.Invoke(attackNumber, activeComboType);
        OnAttackStarted?.Invoke(attackNumber);
        Events?.NotifyPlayerComboAttack(attackNumber);
    }
    private void FinishAttack()
    {
        attackInProgress = false;
        if (nextAttackQueued && attackIndex < CurrentSequence.Length - 1)
        {
            StartAttack(attackIndex + 1);
            return;
        }
        nextAttackQueued = false;
        if (attackIndex >= CurrentSequence.Length - 1 || config.ventanaEntreGolpes <= 0f)
            FinishCombo(false);
        else
            remainingWindow = config.ventanaEntreGolpes;
    }
//El escudo cancela el combo.
    public void CancelForShield() => FinishCombo(true);
    private void InterruptCombo() => FinishCombo(true);
    private void FinishCombo(bool interrupted)
    {
        if (!IsInCombo)
            return;
        attackIndex = -1;
        attackElapsedTime = 0f;
        remainingWindow = 0f;
        attackInProgress = false;
        nextAttackQueued = false;
        hitApplied = false;
        activeComboType = ComboType.Normal;
        OnComboFinished?.Invoke(interrupted);
        OnComboEnded?.Invoke();
        Events?.NotifyPlayerComboEnded();
    }
    private void HandlePauseChanged(bool paused)
    {
        isPaused = paused;
        if (isPaused) pendingAttack = false;
    }
    private void ApplyHit(int damage)
    {
        if (damage <= 0)
            return;
        Vector2 size;
        Vector2 center = GetHitboxCenter(out size);
        Collider2D[] collisions = Physics2D.OverlapBoxAll(
            center, size, 0f, config.capasEnemigas);
        hitTargets.Clear();
        bool enemyHit = false;
        foreach (Collider2D collision in collisions)
        {
            if (collision == null) continue;
            MonoBehaviour[] components = collision.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
            {
                if (component is ITakeDamage target && hitTargets.Add(target))
                {
                    target.TakeDamage(damage);
                    enemyHit = true;
                    Events?.NotifyPlayerComboHit(attackIndex + 1);
                    break;
                }
            }
        }
// Solo el down air puede hacer pogo
        if (enemyHit && activeComboType == ComboType.AirDown &&
            !player.IsGrounded && !player.IsDashing && config.fuerzaPogo > 0f)
        {
            player.Bounce(config.fuerzaPogo);
        }
    }
    private Vector2 GetHitboxCenter(out Vector2 size)
    {
        Vector2 offset;
        if (IsInCombo)
        {
            ComboConfigSO.Golpe attack = CurrentSequence[attackIndex];
        }
        switch (activeComboType)
        {
            case ComboType.AirUp:
                size = config.tamanoHitboxAereoArriba;
                offset = config.desplazamientoHitboxAereoArriba;
                break;
            case ComboType.AirDown:
                size = config.tamanoHitboxAereoAbajo;
                offset = config.desplazamientoHitboxAereoAbajo;
                break;
            case ComboType.AirSide:
                size = config.tamanoHitboxAereoLateral;
                offset = config.desplazamientoHitboxAereoLateral;
                offset.x *= attackDirection;
                break;
            default:
                size = config.tamanoHitbox;
                offset = config.desplazamientoHitbox;
                offset.x *= attackDirection;
                break;
        }
        return (Vector2)transform.position + offset;
    }
    private void HandleAttackInput(InputAction.CallbackContext context)
    {
        if (config != null && !isPaused && player != null &&
            !player.IsShieldInFlight && !player.IsDashing)
            pendingAttack = true;
    }
    private void OnDrawGizmosSelected()//Esto es para saber la zona de dano de los ataques
    {
        if (config == null) return;
        Vector2 size;
        Vector2 center = GetHitboxCenter(out size);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }
}
