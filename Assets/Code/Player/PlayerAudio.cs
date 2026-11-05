
using UnityEngine;

// Gestiona todos los sonidos del jugador mediante AudioManager.

public class PlayerAudio : MonoBehaviour
{
    [SerializeField] private string jumpSound = "Jump";
    [SerializeField] private string wallJumpSound = "Jump";
    [SerializeField] private string dashSound = "Dash";
    [SerializeField] private string attackSound = "Attack";
    [SerializeField] private string shieldSound = "Shield";
    [SerializeField] private string hurtSound = "Hurt";
    [SerializeField] private string deathSound = "Death";

    private PlayerController player;
    private PlayerCombo combo;
    private PlayerShieldThrow shield;
    private Health health;

    private void Awake()
    {
        player = GetComponent<PlayerController>();

        combo = GetComponent<PlayerCombo>();
        shield = GetComponent<PlayerShieldThrow>();
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (player != null)
        {
            player.OnJump += HandleJump;
            player.OnWallJump += HandleWallJump;
            player.OnDash += HandleDash;
        }

        if (combo != null)
            combo.OnAttackStarted += HandleAttack;

        if (shield != null)
            shield.OnShieldPreparing += HandleShield;

        if (health != null)
        {
            health.OnDamaged += HandleDamage;
            health.OnDied += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.OnJump -= HandleJump;
            player.OnWallJump -= HandleWallJump;
            player.OnDash -= HandleDash;
        }

        if (combo != null)
            combo.OnAttackStarted -= HandleAttack;

        if (shield != null)
            shield.OnShieldPreparing -= HandleShield;

        if (health != null)
        {
            health.OnDamaged -= HandleDamage;
            health.OnDied -= HandleDeath;
        }
    }

    private void HandleJump()
    {
        PlaySound(jumpSound);
    }

    private void HandleWallJump()
    {
        PlaySound(wallJumpSound);
    }

    private void HandleDash()
    {
        PlaySound(dashSound);
    }

    private void HandleAttack(int attackNumber)
    {
        PlaySound(attackSound);
    }

    private void HandleShield()
    {
        PlaySound(shieldSound);
    }

    private void HandleDamage(int damage)
    {
        if (damage <= 0)
            return;

        if (health != null && health.IsDead)
            return;

        PlaySound(hurtSound);
    }

    private void HandleDeath()
    {
        PlaySound(deathSound);
    }

    private void PlaySound(string soundId)
    {
        if (string.IsNullOrWhiteSpace(soundId))
            return;

        AudioManager.Instance.Play(soundId);
    }
}
