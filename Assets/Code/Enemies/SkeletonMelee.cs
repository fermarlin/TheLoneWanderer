using UnityEngine;

public class SkeletonMelee : EnemyBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator anim;

    protected override void Update()
    {
        base.Update();
        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        
    }

    protected override void Spawn()
    {
        base.Spawn();
         anim.SetTrigger("Spawn");
    }

    public override void AttackPlayer()
    {
        if (Time.time >= e_nextAttackTime && !isAttacking)
        {
            anim.SetTrigger("Attack");
        }

        base.AttackPlayer();
    }

    protected override void HandleDamaged(int damageAmount)
    {
        if (isDead || isAttacking || isHit || isSpawning || !hasSpawned) return;

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