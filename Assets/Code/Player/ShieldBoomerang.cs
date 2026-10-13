using System.Collections.Generic;
using UnityEngine;

//Este script se encarga de la logica del escudo
public  class ShieldBoomerang : MonoBehaviour
{
    private PlayerShieldThrow owner;
    private ShieldConfigSO config;
    private Rigidbody2D rb;
    private Collider2D shieldCollider;
    private int direction;
    private Vector2 origin;
    private bool isReturning;
    private bool isInitialized;
    private float flightTime;
    private readonly HashSet<ITakeDamage> hitTargets = new HashSet<ITakeDamage>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        shieldCollider = GetComponent<Collider2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        shieldCollider.isTrigger = true;
    }

    public void Initialize(PlayerShieldThrow shieldOwner, ShieldConfigSO shieldConfig,
                           int throwDirection, Vector2 startPosition)
    {
        owner = shieldOwner;
        config = shieldConfig;
        direction = throwDirection >= 0 ? 1 : -1;
        origin = startPosition;
        rb.position = startPosition;
        isInitialized = true;
    }

    private void FixedUpdate()
    {
        if (!isInitialized || config == null) return;
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        GameOrchestrator orchestrator = GameOrchestrator.Instance;
        if (orchestrator != null && orchestrator.GamePaused) return;

        float deltaTime = Time.fixedDeltaTime;
        flightTime += deltaTime;
        Vector2 position = rb.position;

        if (!isReturning)
        {
            Vector2 destination = origin + Vector2.right * direction * config.distanciaMaxima;
            Vector2 nextPosition = Vector2.MoveTowards(position, destination,
                config.velocidadIda * deltaTime);
            rb.MovePosition(nextPosition);
            if (Vector2.Distance(nextPosition, destination) <= 0.01f)
                StartReturn();
        }
        else
        {
            Vector2 destination = owner.CatchPoint;
            Vector2 nextPosition = Vector2.MoveTowards(position, destination,
                config.velocidadRegreso * deltaTime);
            rb.MovePosition(nextPosition);
            if (Vector2.Distance(nextPosition, destination) <= config.radioRecogida)
            {
                ReturnShield();
                return;
            }
        }

        //Evita bloquear al personaje  si se pierde el objeto. 
        if (flightTime >= config.tiempoMaximoVuelo)
        {
            ReturnShield();
            return;
        }
    }

    private void StartReturn()
    {
        if (isReturning) return;
        isReturning = true;
        if (config.golpearTambienAlVolver)
            hitTargets.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized || config == null || other == null || owner == null)
            return;
        if ((config.capasEnemigas.value & (1 << other.gameObject.layer)) == 0)
            return;
        if (other.transform.IsChildOf(owner.transform)) return;
        if (isReturning && !config.golpearTambienAlVolver) return;

        foreach (MonoBehaviour component in other.GetComponentsInParent<MonoBehaviour>())
        {
            if (component is ITakeDamage enemy && hitTargets.Add(enemy))
            {
                enemy.TakeDamage(config.danio);
                break;
            }
        }
    }

    private void ReturnShield()
    {
        if (owner != null)
            owner.RecoverShield(this);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Si lo destruye otra cosa que recupere el movimiento el player
        if (owner != null)
            owner.RecoverShield(this);
    }
}
