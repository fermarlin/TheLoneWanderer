using UnityEngine;

public class SkeletonNecrotic : EnemyBehaviour
{
    [Header("Necrotic Settings")]
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
        FinishSpawn();
    }

    public override void AttackPlayer()
    {
        base.AttackPlayer();
        if (isAttacking)
        {
            anim.SetTrigger("Attack");
        }
    }

    protected override void ExecuteAttackHit()
    {
        base.ExecuteAttackHit();
    }

    protected override void FinishAttack()
    {
        base.FinishAttack();
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