
using UnityEngine;

// Sonidos de enemigos


public class EnemyAudio : MonoBehaviour
{
    [SerializeField] private string spawnSound = "EnemySpawn";
    [SerializeField] private string attackSound = "EnemyAttack";
    [SerializeField] private string hurtSound = "EnemyHurt";
    [SerializeField] private string deathSound = "EnemyDeath";

    [SerializeField] private bool detectAttackByAnimatorTag = false;

    private Health health;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private bool wasVisible;
    private int previousAnimationHash;

    private void Awake()
    {
        health = GetComponentInParent<Health>();

        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnDamaged += HandleDamage;
            health.OnDied += HandleDeath;
        }

        previousAnimationHash = 0;
    }

    private void Start()
    {
        if (spriteRenderer != null)
            wasVisible = spriteRenderer.enabled;
    }

    private void Update()
    {
        CheckSpawn();

        if (detectAttackByAnimatorTag)
            CheckAttackAnimation();
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleDamage;
            health.OnDied -= HandleDeath;
        }
    }

    private void CheckSpawn()
    {
        if (spriteRenderer == null)
            return;

        bool visible = spriteRenderer.enabled;

        if (!wasVisible && visible &&
            (health == null || !health.IsDead))
        {
            PlaySound(spawnSound);
        }

        wasVisible = visible;
    }

    private void CheckAttackAnimation()
    {
        if (animator == null ||
            animator.runtimeAnimatorController == null ||
            animator.layerCount == 0 ||
            (health != null && health.IsDead))
        {
            return;
        }

        AnimatorStateInfo state =
            animator.GetCurrentAnimatorStateInfo(0);

        if (state.fullPathHash != previousAnimationHash)
        {
            previousAnimationHash = state.fullPathHash;

            if (state.IsTag("Attack"))
                PlayAttackSound();
        }
    }

    public void PlayAttackSound()
    {
        if (health != null && health.IsDead)
            return;

        PlaySound(attackSound);
    }

    private void HandleDamage(int amount)
    {
        if (amount <= 0)
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

        AudioManager manager = AudioManager.Instance;

        if (manager != null)
            manager.Play(soundId);
    }
}
