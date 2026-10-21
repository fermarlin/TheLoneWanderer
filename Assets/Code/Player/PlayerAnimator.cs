using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine;
/*Este script se encarga de gestionar las animaciones del player
*/
[DefaultExecutionOrder(100)]
public class PlayerAnimator : MonoBehaviour
{
    private PlayerController player;
    private PlayerCombo combo;
    private PlayerShieldThrow shield;
    private Health health;
    private Animator animator;
    private bool wasOnWall;
    private bool previousForcedFall;
    [SerializeField] private PlayerVFX jumpEffect;
    [SerializeField] private PlayerVFX wallJumpEffect;
    [SerializeField] private PlayerVFX dashEffect;

    [SerializeField] private GameEventsSO gameEvents;
    [SerializeField, Min(0.05f)] private float doubleJumpUnlockDuration = 2f;
    [SerializeField, Min(0.05f)] private float wallJumpUnlockDuration = 2f;
    [SerializeField, Min(0.05f)] private float dashUnlockDuration = 2f;

    private enum UnlockType { DoubleJump, WallJump, Dash }
    private readonly Queue<UnlockType> pendingUnlocks = new Queue<UnlockType>();
    private GameEventsSO subscribedEvents;
    private Coroutine unlockCoroutine;
    private bool showingUnlock;
    private AnimatorUpdateMode previousUpdateMode;
    private AnimatorCullingMode previousCullingMode;
    private GameOrchestrator pauseOwner;

    [SerializeField] private string hurtTriggerName = "Hurt";
    private int hurtTriggerHash;


    [SerializeField, Min(0.05f)] private float deathAnimationDuration = 1.2f;

    [SerializeField] private string respawnIdleState = "Base Layer.MainChar_Idle";

    private static readonly int Death = Animator.StringToHash("Death");
    private bool isPlayingDeath;
    private AnimatorUpdateMode animatorModeBeforeDeath;
    public float DeathAnimationDuration => Mathf.Max(0.05f, deathAnimationDuration);
    public bool IsPlayingDeath => isPlayingDeath;

    private static readonly int UnlockDoubleJump = Animator.StringToHash("UnlockDoubleJump");
    private static readonly int UnlockWallJump = Animator.StringToHash("UnlockWallJump");
    private static readonly int UnlockDash = Animator.StringToHash("UnlockDash");

    private static readonly int ShieldThrow = Animator.StringToHash("ShieldThrow");
    private static readonly int ShieldCatch = Animator.StringToHash("ShieldCatch");
    private static readonly int ShieldAway = Animator.StringToHash("ShieldAway");

    private static readonly int Attack1 = Animator.StringToHash("Attack1");
    private static readonly int Attack2 = Animator.StringToHash("Attack2");
    private static readonly int Attack3 = Animator.StringToHash("Attack3");
    private static readonly int AttackEnd = Animator.StringToHash("AttackEnd");
    private static readonly int RunAttack1 = Animator.StringToHash("RunAttack1");
    private static readonly int RunAttack2 = Animator.StringToHash("RunAttack2");
    private static readonly int AirAttackUp = Animator.StringToHash("AirAttackUp");
    private static readonly int AirAttackDown = Animator.StringToHash("AirAttackDown");
    private static readonly int AirAttack1 = Animator.StringToHash("AirAttack1");
    private static readonly int AirAttack2 = Animator.StringToHash("AirAttack2");

    private static readonly int FallingState = Animator.StringToHash("Base Layer.MainChar_JumpFall");

    private static readonly int Speed =
        Animator.StringToHash("Speed");

    private static readonly int Falling =
        Animator.StringToHash("Falling");

    private static readonly int Grounded =
        Animator.StringToHash("Grounded");

    private static readonly int IsOnWall =
        Animator.StringToHash("IsOnWall");

    private static readonly int Jump =
        Animator.StringToHash("Jump");

    private static readonly int Dash =
        Animator.StringToHash("Dash");

    void Awake()
    {
        player = GetComponent<PlayerController>();

        if (player == null)
            player = GetComponentInParent<PlayerController>();

        combo = GetComponent<PlayerCombo>();
        if (combo == null)
            combo = GetComponentInParent<PlayerCombo>();

        shield = GetComponent<PlayerShieldThrow>();
        if (shield == null)
            shield = GetComponentInParent<PlayerShieldThrow>();

        animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();


        animator.updateMode = AnimatorUpdateMode.Normal;
        hurtTriggerHash = Animator.StringToHash(hurtTriggerName);

        health = GetComponentInParent<Health>();
        if (health == null)
            health = GetComponent<Health>();
    }

    void OnEnable()
    {
        if (player == null)
            player = GetComponentInParent<PlayerController>();

        if (player != null)
        {
            player.OnJump += HandleJump;
            player.OnWallJump += HandleWallJump;
            player.OnDash += HandleDash;
        }

        if (combo == null)
            combo = GetComponentInParent<PlayerCombo>();

        if (combo != null)
        {
            // si es un combo normal o corriendo.
            combo.OnAttackStartedWithType += HandleAttack;
            combo.OnComboFinished += FinishComboAnimation;
        }

        if (shield == null)
            shield = GetComponentInParent<PlayerShieldThrow>();
        if (shield != null)
        {
            shield.OnShieldPreparing += HandleShieldThrow;
            shield.OnShieldRecovered += HandleShieldCatch;
        }

        if (health == null)
            health = GetComponentInParent<Health>();
        if (health != null)
        {
            health.OnDamaged += HandleDamage;
            health.OnDied += HandleDeath;
        }

        GameOrchestrator orchestrator = GameOrchestrator.Instance;
        subscribedEvents = orchestrator != null ? orchestrator.Events : gameEvents;
        if (subscribedEvents != null)
        {
            subscribedEvents.OnDoubleJumpUnlocked += HandleDoubleJumpUnlocked;
            subscribedEvents.OnWallJumpUnlocked += HandleWallJumpUnlocked;
            subscribedEvents.OnDashUnlocked += HandleDashUnlocked;
        }
    }

    void OnDisable()
    {
        if (player != null)
        {
            player.OnJump -= HandleJump;
            player.OnWallJump -= HandleWallJump;
            player.OnDash -= HandleDash;
        }

        if (combo != null)
        {
            combo.OnAttackStartedWithType -= HandleAttack;
            combo.OnComboFinished -= FinishComboAnimation;
        }

        if (shield != null)
        {
            shield.OnShieldPreparing -= HandleShieldThrow;
            shield.OnShieldRecovered -= HandleShieldCatch;
        }

        if (health != null)
        {
            health.OnDamaged -= HandleDamage;
            health.OnDied -= HandleDeath;
        }

        if (subscribedEvents != null)
        {
            subscribedEvents.OnDoubleJumpUnlocked -= HandleDoubleJumpUnlocked;
            subscribedEvents.OnWallJumpUnlocked -= HandleWallJumpUnlocked;
            subscribedEvents.OnDashUnlocked -= HandleDashUnlocked;
            subscribedEvents = null;
        }

        if (unlockCoroutine != null)
        {
            StopCoroutine(unlockCoroutine);
            unlockCoroutine = null;
        }
        pendingUnlocks.Clear();
        EndUnlockPresentation();
    }

    private bool HasParameter(int hash, AnimatorControllerParameterType comboType)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.nameHash == hash && p.type == comboType) return true;
        return false;
    }

    private void HandleDamage(int amount)
    {
        if (animator == null || showingUnlock || isPlayingDeath || (health != null && health.IsDead) || amount <= 0)
            return;


        ClearAttackTriggers();
        animator.ResetTrigger(AttackEnd);
        animator.ResetTrigger(Jump);
        animator.ResetTrigger(Dash);
        animator.SetBool(IsOnWall, false);
        animator.SetBool(Falling, false);
        animator.SetFloat(Speed, 0f);
        animator.ResetTrigger(hurtTriggerHash);
        animator.SetTrigger(hurtTriggerHash);
    }

    private void HandleDeath()
    {
        if (isPlayingDeath || animator == null)
            return;

        if (unlockCoroutine != null)
        {
            StopCoroutine(unlockCoroutine);
            unlockCoroutine = null;
        }
        pendingUnlocks.Clear();
        EndUnlockPresentation();

        isPlayingDeath = true;
        animatorModeBeforeDeath = animator.updateMode;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        ClearAttackTriggers();
        animator.ResetTrigger(AttackEnd);
        animator.ResetTrigger(Jump);
        animator.ResetTrigger(Dash);
        animator.ResetTrigger(ShieldThrow);
        animator.ResetTrigger(ShieldCatch);
        animator.ResetTrigger(UnlockDoubleJump);
        animator.ResetTrigger(UnlockWallJump);
        animator.ResetTrigger(UnlockDash);
        if (HasParameter(hurtTriggerHash, AnimatorControllerParameterType.Trigger))
            animator.ResetTrigger(hurtTriggerHash);
        animator.SetFloat(Speed, 0f);
        animator.SetBool(IsOnWall, false);
        animator.SetBool(Falling, false);

        if (HasParameter(Death, AnimatorControllerParameterType.Trigger))
        {
            animator.ResetTrigger(Death);
            animator.SetTrigger(Death);
        }
    }


    public void ResetAfterRespawn()
    {
        if (animator == null)
        {
            isPlayingDeath = false;
            return;
        }

        animator.ResetTrigger(Death);
        if (HasParameter(hurtTriggerHash, AnimatorControllerParameterType.Trigger))
            animator.ResetTrigger(hurtTriggerHash);
        ClearAttackTriggers();
        animator.ResetTrigger(AttackEnd);
        animator.ResetTrigger(Jump);
        animator.ResetTrigger(Dash);
        animator.ResetTrigger(ShieldThrow);
        animator.ResetTrigger(ShieldCatch);
        animator.ResetTrigger(UnlockDoubleJump);
        animator.ResetTrigger(UnlockWallJump);
        animator.ResetTrigger(UnlockDash);

        //Esto evita que quede activa alguna condicion de antes de morir.
        animator.SetBool(ShieldAway, false);
        animator.SetFloat(Speed, 0f);
        animator.SetBool(IsOnWall, false);
        animator.SetBool(Falling, false);
        animator.SetBool(Grounded, player != null && player.IsGrounded);

        wasOnWall = false;
        previousForcedFall = false;
        isPlayingDeath = false;
        animator.updateMode = animatorModeBeforeDeath;

        string stateName = string.IsNullOrWhiteSpace(respawnIdleState)
            ? "Base Layer.MainChar_Idle"
            : respawnIdleState.Trim();

        int idleStateHash = Animator.StringToHash(stateName);
        if (animator.HasState(0, idleStateHash))
        {
            animator.Play(idleStateHash, 0, 0f);
        }
    }

    private void HandleShieldThrow()
    {
        if (showingUnlock || isPlayingDeath)
            return;

            animator.SetBool(ShieldAway, true);
            animator.ResetTrigger(ShieldCatch);
            animator.ResetTrigger(ShieldThrow);
            animator.SetTrigger(ShieldThrow);
        
    }

    private void HandleShieldCatch()
    {
        if (showingUnlock || isPlayingDeath)
            return;

            animator.SetBool(ShieldAway, false);
            animator.ResetTrigger(ShieldThrow);
            animator.ResetTrigger(ShieldCatch);
            animator.SetTrigger(ShieldCatch);
    }

    void Update()
    {
        TryStartUnlockPresentation();

        if (showingUnlock || isPlayingDeath || player == null || animator == null)
            return;

        if (player.IsInDamageKnockback)
        {
            animator.SetFloat(Speed, 0f);
            animator.SetBool(IsOnWall, false);
            animator.SetBool(Falling, false);
            return;
        }

        if (player.IsShieldInFlight)
        {
            //Esto es para que el jugador no pueda moverse si el escudo esta en el aire
            animator.SetFloat(Speed, 0f);
            animator.SetBool(Grounded, true);
            animator.SetBool(IsOnWall, false);
            animator.SetBool(Falling, false);
            wasOnWall = false;
            previousForcedFall = false;
            return;
        }

        if (player.IsDashing)
        {
            animator.SetFloat(Speed, Mathf.Abs(player.Velocity.x));
            animator.SetBool(Grounded, false);
            animator.SetBool(IsOnWall, false);
            animator.SetBool(Falling, false);
            wasOnWall = false;
            previousForcedFall = false;
            return;
        }

        // Movimiento horizontal
        animator.SetFloat(
            Speed,
            Mathf.Abs(player.HorizontalMovement)
        );

        // Estados actual
        animator.SetBool(
            Grounded,
            player.IsGrounded
        );


        bool holdingAttack = combo != null && combo.IsInCombo;
        animator.SetBool(
            IsOnWall,
            !holdingAttack && player.IsTouchingWall && !player.IsJumping
        );

        bool isFalling = player.IsFalling && !holdingAttack;
        animator.SetBool(Falling, isFalling);

        
        bool leftWall = wasOnWall && !player.IsTouchingWall;
        bool newForcedFall = player.IsForcedWallFall && !previousForcedFall;
        if (!holdingAttack && (leftWall || newForcedFall) && player.IsForcedWallFall &&
            !player.IsGrounded && animator.HasState(0, FallingState))
        {
            animator.Play(FallingState, 0, 0f);
        }

        wasOnWall = player.IsTouchingWall;
        previousForcedFall = player.IsForcedWallFall;
    }

    private void ClearAttackTriggers()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(Attack1);
        animator.ResetTrigger(Attack2);
        animator.ResetTrigger(Attack3);
        animator.ResetTrigger(RunAttack1);
        animator.ResetTrigger(RunAttack2);
        animator.ResetTrigger(AirAttackUp);
        animator.ResetTrigger(AirAttackDown);
        animator.ResetTrigger(AirAttack1);
        animator.ResetTrigger(AirAttack2);
    }

    private void FinishComboAnimation(bool interrupted)
    {
        if (showingUnlock || isPlayingDeath)
            return;

        if (animator == null)
            return;

        ClearAttackTriggers();
        if (interrupted)
            return;

        if (player != null)
        {
            animator.SetBool(Grounded, player.IsGrounded);
            animator.SetBool(IsOnWall, false);
            animator.SetBool(Falling, false);
        }

        animator.ResetTrigger(AttackEnd);
        animator.SetTrigger(AttackEnd);
    }

    private void HandleAttack(int attackNumber, PlayerCombo.ComboType comboType)
    {
        if (showingUnlock || isPlayingDeath)
            return;

        if (animator == null)
            return;

        animator.ResetTrigger(AttackEnd);
        ClearAttackTriggers();
        animator.ResetTrigger(Jump);
        animator.ResetTrigger(Dash);

        bool isAirAttack = comboType == PlayerCombo.ComboType.AirUp ||
                        comboType == PlayerCombo.ComboType.AirDown ||
                        comboType == PlayerCombo.ComboType.AirSide;
        animator.SetBool(Grounded, !isAirAttack);
        animator.SetBool(IsOnWall, false);
        animator.SetBool(Falling, false);

        switch (comboType)
        {
            case PlayerCombo.ComboType.Running:
                if (attackNumber == 1) animator.SetTrigger(RunAttack1);
                else if (attackNumber == 2) animator.SetTrigger(RunAttack2);
                break;

            case PlayerCombo.ComboType.AirUp:
                if (attackNumber == 1) animator.SetTrigger(AirAttackUp);
                break;

            case PlayerCombo.ComboType.AirDown:
                if (attackNumber == 1) animator.SetTrigger(AirAttackDown);
                break;

            case PlayerCombo.ComboType.AirSide:
                if (attackNumber == 1) animator.SetTrigger(AirAttack1);
                else if (attackNumber == 2) animator.SetTrigger(AirAttack2);
                break;

            default:
                if (attackNumber == 1) animator.SetTrigger(Attack1);
                else if (attackNumber == 2) animator.SetTrigger(Attack2);
                else if (attackNumber == 3) animator.SetTrigger(Attack3);
                break;
        }
    }

    private void HandleDash()
    {
        if (showingUnlock || isPlayingDeath)
            return;

        animator.ResetTrigger(AttackEnd);
        ClearAttackTriggers();
        animator.ResetTrigger(Jump);
        animator.SetBool(IsOnWall, false);
        animator.SetBool(Falling, false);
        animator.SetBool(Grounded, false);
        animator.ResetTrigger(Dash);
        animator.SetTrigger(Dash);
        if (dashEffect != null)
            dashEffect.PlayVFX();
    }

    void HandleJump()
    {
        if (showingUnlock || isPlayingDeath)
            return;

        animator.SetBool(Grounded, false);
        animator.SetBool(IsOnWall, false);
        animator.SetBool(Falling, false);

        animator.ResetTrigger(AttackEnd);
        ClearAttackTriggers();
        animator.ResetTrigger(Jump);
        animator.SetTrigger(Jump);

        if (jumpEffect != null)
            jumpEffect.PlayVFX();
    }

    void HandleWallJump()
    {
        if (showingUnlock || isPlayingDeath)
            return;

        if (animator == null)
            return;

        //Esto es por si acaso para que no haya problemas con otras variables
        animator.SetBool(Grounded, false);
        animator.SetBool(IsOnWall, false);
        animator.SetBool(Falling, false);

        animator.ResetTrigger(AttackEnd);
        ClearAttackTriggers();
        animator.ResetTrigger(Jump);
        animator.SetTrigger(Jump);

        if (wallJumpEffect != null)
        {
            //Aplico la velocidad contraria a la posicion de la pared
            int wallSide = player.Velocity.x > 0f ? -1 : 1;
            wallJumpEffect.PlayVFX(wallSide);
        }
    }

    private void HandleDoubleJumpUnlocked() => QueueUnlock(UnlockType.DoubleJump);
    private void HandleWallJumpUnlocked() => QueueUnlock(UnlockType.WallJump);
    private void HandleDashUnlocked() => QueueUnlock(UnlockType.Dash);

    private int UnlockTrigger(UnlockType ability)
    {
        switch (ability)
        {
            case UnlockType.DoubleJump: return UnlockDoubleJump;
            case UnlockType.WallJump: return UnlockWallJump;
            default: return UnlockDash;
        }
    }

    private float UnlockDuration(UnlockType ability)
    {
        switch (ability)
        {
            case UnlockType.DoubleJump: return doubleJumpUnlockDuration;
            case UnlockType.WallJump: return wallJumpUnlockDuration;
            default: return dashUnlockDuration;
        }
    }

    private void QueueUnlock(UnlockType ability)
    {
        if (isPlayingDeath) return;
        int trigger = UnlockTrigger(ability);


        pendingUnlocks.Enqueue(ability);
        TryStartUnlockPresentation();
    }

    private void TryStartUnlockPresentation()
    {
        if (!isActiveAndEnabled || isPlayingDeath || animator == null ||
            unlockCoroutine != null || showingUnlock || pendingUnlocks.Count == 0)
            return;

        GameOrchestrator orchestrator = GameOrchestrator.Instance;
        if (orchestrator == null || orchestrator.GamePaused)
            return;

        unlockCoroutine = StartCoroutine(PlayUnlockQueue(orchestrator));
    }

    private IEnumerator PlayUnlockQueue(GameOrchestrator orchestrator)
    {
        showingUnlock = true;
        previousUpdateMode = animator.updateMode;
        previousCullingMode = animator.cullingMode;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;


        pauseOwner = orchestrator;
        orchestrator.AcquirePauseLock();
        orchestrator.PauseGame(true);

        while (pendingUnlocks.Count > 0)
        {
            UnlockType ability = pendingUnlocks.Dequeue();

            ClearAttackTriggers();
            animator.ResetTrigger(AttackEnd);
            animator.ResetTrigger(Jump);
            animator.ResetTrigger(Dash);
            animator.ResetTrigger(ShieldThrow);
            animator.ResetTrigger(ShieldCatch);
            animator.ResetTrigger(UnlockDoubleJump);
            animator.ResetTrigger(UnlockWallJump);
            animator.ResetTrigger(UnlockDash);
            animator.SetBool(IsOnWall, false);
            animator.SetBool(Falling, false);
            animator.SetFloat(Speed, 0f);

            animator.SetTrigger(UnlockTrigger(ability));

            yield return new WaitForSecondsRealtime(
                Mathf.Max(0.05f, UnlockDuration(ability)));
        }

        EndUnlockPresentation();
        unlockCoroutine = null;
    }

    private void EndUnlockPresentation()
    {
        if (!showingUnlock)
            return;

        showingUnlock = false;

        if (animator != null)
        {
            animator.ResetTrigger(UnlockDoubleJump);
            animator.ResetTrigger(UnlockWallJump);
            animator.ResetTrigger(UnlockDash);
            animator.updateMode = previousUpdateMode;
            animator.cullingMode = previousCullingMode;
        }

        GameOrchestrator orchestrator = pauseOwner;
        pauseOwner = null;
        if (orchestrator != null)
        {
            orchestrator.ReleasePauseLock();
            orchestrator.PauseGame(false);
        }
    }

}
