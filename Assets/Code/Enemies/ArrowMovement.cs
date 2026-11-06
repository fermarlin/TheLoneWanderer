using UnityEngine;

public class ArrowMovement : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private float stickTime = 5f;

    [Header("Impact Visuals")]
    [SerializeField] private Sprite[] impactSprites;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    private bool hasImpacted = false;
    private bool hasHitPlayer = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    private void Start()
    {
        Invoke(nameof(DestroyArrow), lifeTime);

        float direction = Mathf.Sign(transform.localScale.x);
        rb.linearVelocity = new Vector2(direction * speed, 0f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasImpacted) return;

        if (collision.CompareTag("Player") && !hasHitPlayer)
        {
            hasHitPlayer = true;

            Health playerHealth = collision.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }

            if (anim != null) anim.enabled = false;

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3.5f;
            rb.linearVelocity = Vector2.zero;
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            StickArrow(collision.transform);
        }
    }

    private void StickArrow(Transform target)
    {
        hasImpacted = true;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.enabled = false;

        transform.SetParent(target);

        if (anim != null) anim.enabled = false;

        if (impactSprites != null && impactSprites.Length > 0)
        {
            int randomIndex = Random.Range(0, impactSprites.Length);
            spriteRenderer.sprite = impactSprites[randomIndex];
        }

        CancelInvoke(nameof(DestroyArrow));
        Invoke(nameof(DestroyArrow), stickTime);
    }

    private void DestroyArrow()
    {
        Destroy(gameObject);
    }
}