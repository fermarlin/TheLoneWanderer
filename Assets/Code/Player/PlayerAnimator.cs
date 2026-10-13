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
    private Animator animator;
    private bool wasOnWall;
    private bool previousForcedFall;
    [SerializeField] private PlayerVFX jumpEffect;
    [SerializeField] private PlayerVFX wallJumpEffect;
    [SerializeField] private PlayerVFX dashEffect;

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

        if (player == null || animator == null)
        {
            Debug.LogError(
                "PlayerAnimator: PlayerController or Animator was not found.",
                this
            );

            enabled = false;
            return;
        }

        animator.updateMode = AnimatorUpdateMode.Normal;
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
    }

    private bool HasParameter(int hash, AnimatorControllerParameterType comboType)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.nameHash == hash && p.type == comboType) return true;
        return false;
    }

    //Empieza la animacion al pulsar, no cuando aparece el proyectil. Esto lo hice porque la animacion que tenemos tarda un plein en lanzarlo
    private void HandleShieldThrow()
    {
            animator.SetBool(ShieldAway, true);
            animator.ResetTrigger(ShieldCatch);
            animator.ResetTrigger(ShieldThrow);
            animator.SetTrigger(ShieldThrow);
        
    }

    private void HandleShieldCatch()
    {
            animator.SetBool(ShieldAway, false);
            animator.ResetTrigger(ShieldThrow);
            animator.ResetTrigger(ShieldCatch);
            animator.SetTrigger(ShieldCatch);
    }

    void Update()
    {
        if (player == null || animator == null)
            return;

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

        // El primer golpe de cada rama parte de Any State.
        // Los siguientes SOLO se alcanzan desde el golpe previo.
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
        animator.ResetTrigger(AttackEnd);
        ClearAttackTriggers();
        animator.ResetTrigger(Jump);
        animator.SetBool(IsOnWall, false);
        animator.SetBool(Falling, false);
        animator.SetBool(Grounded, false);
        animator.ResetTrigger(Dash);
        animator.SetTrigger(Dash);
        dashEffect.PlayVFX();
    }

    void HandleJump()
    {
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
}
