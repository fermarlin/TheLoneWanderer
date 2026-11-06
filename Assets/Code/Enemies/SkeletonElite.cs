using UnityEngine;

public class SkeletonElite : EnemyBehaviour
{
    [Header("Special Attack Settings")]
    [SerializeField] private float dashDistance = 12f;
    [SerializeField] private float specialAttackCooldown = 5f;
    [SerializeField] private int specialDamage = 3;
    [SerializeField] private float specialAttackDistanceX = 6f;
    [SerializeField] private float specialAttackDistanceY = 2f;
    [SerializeField] private float specialAttackOffsetY = 1f;

    [Header("Animator")]
    [SerializeField] private Animator anim;

    private float nextSpecialAttackTime = 0f;
    private bool isDashing = false;
    private bool hasHitPlayerThisDash = false;
    private bool isPlayerInSpecialAttackRange = false;

    protected override void Update()
    {
        if (isDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isHit)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        LookForPlayer();

        if (!hasSpawned)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            if (isPlayerInRange && !isSpawning)
            {
                isSpawning = true;
                spriteRenderer.enabled = true;
                Spawn();
            }
            return;
        }

        if (isAttacking)
        {
            if (isDashing)
            {
                rb.linearVelocity = new Vector2(facingDirection * dashDistance, rb.linearVelocity.y);

                if (!hasHitPlayerThisDash)
                {
                    CheckDashHit();
                }
            }
            else
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
            return;
        }

        if (anim != null)
        {
            anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        }

        if (isPlayerInSpecialAttackRange && Time.time >= nextSpecialAttackTime)
        {
            if (isPatrolling) EndPatrol();
            ExecuteSpecialAttack();
        }
        else if (isPlayerInAttackRange)
        {
            if (isPatrolling) EndPatrol();
            ExecuteNormalAttack();
        }
        else if (isPlayerInMovementRange)
        {
            if (isPatrolling) EndPatrol();
            MoveTowardsPlayer();
        }
        else
        {
            if (!isPatrolling) BeginPatrol();
            if (isPatrolling) Patrol();
        }
    }

    public override void LookForPlayer()
    {
        base.LookForPlayer();

        if (player != null)
        {
            float spAttackDistX = Mathf.Abs(player.transform.position.x - transform.position.x);
            float spAttackDistY = Mathf.Abs(player.transform.position.y - (transform.position.y + specialAttackOffsetY));

            float directionToPlayer = Mathf.Sign(player.transform.position.x - transform.position.x);
            bool isFacingPlayer = (directionToPlayer == facingDirection);

            if (spAttackDistX <= specialAttackDistanceX && spAttackDistY <= specialAttackDistanceY && isFacingPlayer)
            {
                isPlayerInSpecialAttackRange = true;
            }
            else
            {
                isPlayerInSpecialAttackRange = false;
            }
        }
        else
        {
            isPlayerInSpecialAttackRange = false;
        }
    }

    private void ExecuteSpecialAttack()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        isAttacking = true;
        nextSpecialAttackTime = Time.time + specialAttackCooldown;
        if (anim != null) anim.SetTrigger("SpecialAttack");
    }

    private void ExecuteNormalAttack()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (Time.time >= e_nextAttackTime)
        {
            isAttacking = true;
            e_nextAttackTime = Time.time + e_attackCooldown;
            if (anim != null) anim.SetTrigger("Attack");
        }
    }

    private void CheckDashHit()
    {
        if (player == null) return;

        if (isPlayerInAttackRange)
        {
            Health playerHealth = player.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(specialDamage);
                hasHitPlayerThisDash = true;
            }
        }
    }

    protected override void Spawn()
    {
        base.Spawn();
        if (anim != null) anim.SetTrigger("Spawn");
    }

    public void StartDash()
    {
        isDashing = true;
        hasHitPlayerThisDash = false;
    }

    public void StopDash()
    {
        isDashing = false;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    protected override void FinishAttack()
    {
        isDashing = false;
        base.FinishAttack();
    }

    protected override void HandleDamaged(int damageAmount)
    {
        if (isDead || !hasSpawned || isSpawning || isAttacking || isHit) return;

        isDashing = false;
        base.HandleDamaged(damageAmount);
        if (anim != null) anim.SetTrigger("Hit");
    }

    protected override void HandleDied()
    {
        isDashing = false;
        base.HandleDied();

        if (anim != null)
        {
            anim.ResetTrigger("Hit");
            anim.ResetTrigger("Attack");
            anim.ResetTrigger("SpecialAttack");
            anim.SetTrigger("Death");
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.cyan;
        int drawDirection = Application.isPlaying ? facingDirection : (int)Mathf.Sign(transform.localScale.x);
        Vector3 specialCenter = transform.position + new Vector3((specialAttackDistanceX / 2f) * drawDirection, specialAttackOffsetY, 0);
        Vector3 specialSize = new Vector3(specialAttackDistanceX, specialAttackDistanceY, 0);

        Gizmos.DrawWireCube(specialCenter, specialSize);
    }
}