using UnityEngine;

public abstract class EnemyBehaviour : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] protected int e_damage;
    [SerializeField] protected float e_attackCooldown = 1.5f;
    [SerializeField] protected float e_nextAttackTime = 0f;
    [SerializeField] protected float e_movementSpeed = 3f;
    [SerializeField] protected float e_movementRangeX = 8f;
    [SerializeField] protected float e_movementRangeY = 4f;
    [SerializeField] protected float e_movementOffsetY = 1f;
    [SerializeField] protected int e_leechDamage;
    [SerializeField] protected float e_detectionRange = 10f;
    [SerializeField] protected float e_attackDistanceX = 2f;
    [SerializeField] protected float e_attackDistanceY = 2f;
    [SerializeField] protected float e_attackOffsetY = 1f;

    [Header("Patrol Settings")]
    [SerializeField] protected Transform wallCheck;      
    [SerializeField] protected float wallCheckDistance = 1f; 
    [SerializeField] protected Transform ledgeCheck;    
    [SerializeField] protected float ledgeCheckDistance = 0.5f;  
    [SerializeField] protected LayerMask groundLayer;
    [SerializeField] protected float minPatrolTime = 2f;
    [SerializeField] protected float maxPatrolTime = 5f;
    [SerializeField] protected float minWaitTime = 1f;
    [SerializeField] protected float maxWaitTime = 3f;

    protected GameObject player;
    protected bool isPlayerInRange;
    protected bool isPlayerInAttackRange;
    protected bool isPlayerInMovementRange;
    protected int facingDirection = -1; // 1 derecha -1 izquierda

    protected bool isPatrolling = true;
    protected bool hasSpawned = false;
    protected float patrolTimer;
    protected float waitTimer;
    protected bool isWaiting;
    protected bool isObstacleWait;

    protected Rigidbody2D rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        facingDirection = (int)Mathf.Sign(transform.localScale.x);
    }

    protected virtual void Update()
    {
        LookForPlayer();

        if (!hasSpawned)
        {
            if (isPlayerInRange)
            {
                Spawn();
            }
            return; 
        }

        if (isPlayerInAttackRange)
        {
            if (isPatrolling) EndPatrol();
            AttackPlayer();
        }
        else if (isPlayerInMovementRange && !isObstacleWait)
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

    protected virtual void Spawn()
    {
        // animator
        hasSpawned = true; 
        Debug.Log("Spawnea");
    }

    public virtual void LookForPlayer()
    {
        if (player == null)
        {
            player = GameObject.FindWithTag("Player");
        }

        if (player != null)
        {
            float distanciaTotal = Vector2.Distance(transform.position, player.transform.position);
            isPlayerInRange = distanciaTotal <= e_detectionRange;

            float moveDistX = Mathf.Abs(player.transform.position.x - transform.position.x);
            float moveDistY = Mathf.Abs(player.transform.position.y - (transform.position.y + e_movementOffsetY));

            float attackDistX = Mathf.Abs(player.transform.position.x - transform.position.x);
            float attackDistY = Mathf.Abs(player.transform.position.y - (transform.position.y + e_attackOffsetY));

            isPlayerInMovementRange = (moveDistX <= e_movementRangeX && moveDistY <= e_movementRangeY);

            float directionToPlayer = Mathf.Sign(player.transform.position.x - transform.position.x);
            bool isFacingPlayer = (directionToPlayer == facingDirection);

            if (attackDistX <= e_attackDistanceX && attackDistY <= e_attackDistanceY && isFacingPlayer)
            {
                isPlayerInAttackRange = true;
            }
            else
            {
                isPlayerInAttackRange = false;
            }
        }
    }

    public virtual void MoveTowardsPlayer()
    {
        if (player == null) return;

        float directionToPlayer = Mathf.Sign(player.transform.position.x - transform.position.x);
        if (directionToPlayer != facingDirection)
        {
            Flip();
        }

        if (wallCheck == null || ledgeCheck == null) return;

        bool isWallAhead = Physics2D.Raycast(wallCheck.position, Vector2.right * facingDirection, wallCheckDistance, groundLayer);
        bool isLedgeAhead = !Physics2D.Raycast(ledgeCheck.position, Vector2.down, ledgeCheckDistance, groundLayer);

        if (isWallAhead || isLedgeAhead)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(facingDirection * e_movementSpeed, rb.linearVelocity.y);
            Debug.Log("Voy a por ti");
        }
    }

    protected virtual void Flip()
    {
        facingDirection *= -1;

        Vector3 currentScale = transform.localScale;
        currentScale.x *= -1;
        transform.localScale = currentScale;
    }

    public virtual void AttackPlayer()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (Time.time >= e_nextAttackTime)
        {
            Health playerHealth = player.GetComponent<Health>();
            //animator
            Debug.Log("Te pega un mascazo");

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(e_damage);
                Debug.Log("Recives " + e_damage + " de daño");
                Debug.Log("Vida restante " + playerHealth.CurrentHealth);
            }

            e_nextAttackTime = Time.time + e_attackCooldown;
        }
    }

    protected virtual void Patrol()
    {
        if (isWaiting)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0)
            {
                isWaiting = false; 

                if (!isObstacleWait)
                {
                    if (Random.value > 0.5f)
                    {
                        Flip();
                    }
                }

                isObstacleWait = false;
                patrolTimer = Random.Range(minPatrolTime, maxPatrolTime);
            }
            return;
        }

        rb.linearVelocity = new Vector2(facingDirection * e_movementSpeed, rb.linearVelocity.y);

        if (wallCheck == null || ledgeCheck == null) return;

        bool isWallAhead = Physics2D.Raycast(wallCheck.position, Vector2.right * facingDirection, wallCheckDistance, groundLayer);
        bool isLedgeAhead = !Physics2D.Raycast(ledgeCheck.position, Vector2.down, ledgeCheckDistance, groundLayer);

        if (isWallAhead || isLedgeAhead)
        {
            Flip();
            isWaiting = true;
            isObstacleWait = true;
            waitTimer = Random.Range(minWaitTime, maxWaitTime);
        }
        else
        {
            patrolTimer -= Time.deltaTime;
            if (patrolTimer <= 0)
            {
                isWaiting = true;
                isObstacleWait = false;
                waitTimer = Random.Range(minWaitTime, maxWaitTime);
            }
        }
    }

    public virtual void BeginPatrol()
    {
        isPatrolling = true;
        isWaiting = false;
        isObstacleWait = false;
        patrolTimer = Random.Range(minPatrolTime, maxPatrolTime);
    }

    public virtual void EndPatrol()
    {
        isPatrolling = false;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, e_detectionRange);

        Gizmos.color = Color.magenta;
        Vector3 movementCenter = transform.position + new Vector3(0, e_movementOffsetY, 0);
        Vector3 movementSize = new Vector3(e_movementRangeX * 2, e_movementRangeY * 2, 0);
        Gizmos.DrawWireCube(movementCenter, movementSize);


        Gizmos.color = Color.red;
        Vector3 attackCenter = transform.position + new Vector3((e_attackDistanceX / 2f) * facingDirection, e_attackOffsetY, 0);
        Vector3 attackSize = new Vector3(e_attackDistanceX, e_attackDistanceY, 0);
        Gizmos.DrawWireCube(attackCenter, attackSize);

        Gizmos.color = Color.cyan;
        int drawDirection = Application.isPlaying ? facingDirection : 1;

        if (wallCheck != null)
        {
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + Vector3.right * drawDirection * wallCheckDistance);
        }

        if (ledgeCheck != null)
        {
            Gizmos.DrawLine(ledgeCheck.position, ledgeCheck.position + Vector3.down * ledgeCheckDistance);
        }
    }
}
