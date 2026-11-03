using UnityEngine;

public class SkeletonArcher : EnemyBehaviour
{
    [Header("Archer Settings")]
    [SerializeField] private GameObject arrowPrefab; 
    [SerializeField] private Transform firePoint;

    [Header("Animator")]
    [SerializeField] private Animator anim;

    protected override void Update()
    {
        base.Update();

        if (anim != null)
        {
            anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        }
    }

    protected override void Spawn()
    {
        base.Spawn();

        anim.SetTrigger("Spawn");
    }

    protected override void ExecuteAttackHit()
    {
        {
            if (player == null) return;
            LookForPlayer();

            if (isPlayerInAttackRange && arrowPrefab != null && firePoint != null)
            {
                GameObject arrow = Instantiate(arrowPrefab, firePoint.position, firePoint.rotation);

                float facingDirection = Mathf.Sign(transform.localScale.x);
                arrow.transform.localScale = new Vector3(facingDirection, 1, 1);
            }
        }
    }

    protected override void FinishAttack()
    {
        base.FinishAttack();
    }

    public override void AttackPlayer()
    {
        base.AttackPlayer();
        if (isAttacking)
        {
            anim.SetTrigger("Attack");
        }
    }

    protected override void HandleDamaged(int damageAmount)
    {
        if (isDead || !hasSpawned || isSpawning || isAttacking || isHit) return;

        base.HandleDamaged(damageAmount);
        anim.SetTrigger("Hit");
    }

    protected override void HandleDied()
    {
        base.HandleDied();

        anim.ResetTrigger("Hit");
        anim.ResetTrigger("Attack");

        anim.SetTrigger("Death");
    }
}
