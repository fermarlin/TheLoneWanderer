using UnityEngine;

//Script de knockback
[DefaultExecutionOrder(200)]
public sealed class DamageKnockback2D : MonoBehaviour
{
    private const float horizontalSpeed = 10f;
    private const float verticalSpeed = 10f;
    private const float duration = 0.25f;
    private const float maxHorizontalDistance = 1.25f;

    private Health health;
    private Rigidbody2D rb;
    private PlayerController player;
    private PlayerCombo combo;
    private PlayerShieldThrow shield;

    private bool comboWasEnabled;
    private bool comboWasLocked;
    private bool shieldWasEnabled;
    private bool shieldWasLocked;
    private bool activeKnockback;
    private bool kinematicAtStart;

    private float elapsed;
    private float direction;
    private Vector2 startPosition;
    private float previousGravityScale;
    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];

    public bool IsKnockbackActive => activeKnockback;

    private void Awake()
    {
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<PlayerController>();
        combo = GetComponent<PlayerCombo>();
        shield = GetComponent<PlayerShieldThrow>();
    }

    private void OnEnable()
    {
        if (health == null) health = GetComponent<Health>();
        if (health != null)
        {
            health.OnDamaged += OnDamaged;
            health.OnDied += OnDied;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= OnDamaged;
            health.OnDied -= OnDied;
        }
        StopKnockback();
    }

    private void OnDamaged(int amount)
    {
        if (amount <= 0 || !isActiveAndEnabled || rb == null || !rb.simulated)
            return;

        if (activeKnockback) StopKnockback();

        direction = DetermineDirection();
        elapsed = 0f;
        startPosition = rb.position;
        kinematicAtStart = rb.bodyType == RigidbodyType2D.Kinematic;

        if (player != null) player.SetDamageKnockbackActive(true);

        if (combo != null && combo.enabled)
        {
            comboWasEnabled = true;
            comboWasLocked = true;
            combo.enabled = false;
        }
        if (shield != null && shield.enabled && (player == null || !player.IsShieldInFlight))
        {
            shieldWasEnabled = true;
            shieldWasLocked = true;
            shield.enabled = false;
        }

        previousGravityScale = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        if (player != null) player.AuthorizeCurrentVerticalVelocity();
        activeKnockback = true;
    }

    private float DetermineDirection()
    {
        if (health != null && health.HasDamageSource)
        {
            float dx = rb.position.x - health.DamageSourcePosition.x;
            if (Mathf.Abs(dx) > 0.001f)
                return Mathf.Sign(dx);
        }

        if (player == null)
        {

            ShieldBoomerang[] shields = FindObjectsByType<ShieldBoomerang>(FindObjectsSortMode.None);
            float nearestDistanceSqr = 2f * 2f;
            ShieldBoomerang closestShield = null;
            foreach (ShieldBoomerang candidate in shields)
            {
                if (candidate == null || !candidate.isActiveAndEnabled)
                    continue;
                float sqrDistance = ((Vector2)candidate.transform.position - rb.position).sqrMagnitude;
                if (sqrDistance < nearestDistanceSqr)
                {
                    nearestDistanceSqr = sqrDistance;
                    closestShield = candidate;
                }
            }
            if (closestShield != null)
            {
                float dx = rb.position.x - closestShield.transform.position.x;
                if (Mathf.Abs(dx) > 0.001f)
                    return Mathf.Sign(dx);
            }

            PlayerController attacker = FindFirstObjectByType<PlayerController>();
            if (attacker != null)
            {
                float dx = rb.position.x - attacker.transform.position.x;
                if (Mathf.Abs(dx) > 0.001f)
                    return Mathf.Sign(dx);
                return attacker.FacingDirection >= 0 ? 1f : -1f;
            }
        }

        if (player != null)
            return player.FacingDirection < 0 ? 1f : -1f;
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            return -Mathf.Sign(rb.linearVelocity.x);
        return transform.localScale.x < 0f ? 1f : -1f;
    }

    private void FixedUpdate()
    {
        if (!activeKnockback || rb == null || !rb.simulated) return;

        elapsed = Mathf.Min(duration, elapsed + Time.fixedDeltaTime);
        float t = Mathf.Clamp01(elapsed / duration);

        if (kinematicAtStart)
        {
            float xDistance = Mathf.Min(maxHorizontalDistance,
                horizontalSpeed * duration * (t - 0.5f * t * t));
            float height = verticalSpeed * duration * 0.25f;
            float yOffset = 4f * height * t * (1f - t);
            rb.MovePosition(startPosition + new Vector2(direction * xDistance, yOffset));
        }
        else
        {
            float profile = 1f - t;
            float x = direction * horizontalSpeed * profile;
            float y = verticalSpeed * profile;
            if (Mathf.Abs(rb.position.x - startPosition.x) >= maxHorizontalDistance)
                x = 0f;

            int count = rb.GetContacts(contacts);
            for (int i = 0; i < count; i++)
            {
                Vector2 normal = contacts[i].normal;
                if (normal.x * direction < -0.65f) x = 0f;
                if (normal.y < -0.65f && y > 0f) y = 0f;
            }
            rb.linearVelocity = new Vector2(x, y);
            if (player != null) player.AuthorizeCurrentVerticalVelocity();
        }

        if (t >= 1f) StopKnockback();
    }

    private void OnDied() => StopKnockback();

    private void StopKnockback()
    {
        if (!activeKnockback && !comboWasLocked && !shieldWasLocked) return;
        activeKnockback = false;

        if (rb != null)
        {
            rb.gravityScale = previousGravityScale;
            rb.linearVelocity = Vector2.zero;
        }
        if (player != null)
        {
            player.AuthorizeCurrentVerticalVelocity();
            player.SetDamageKnockbackActive(false);
        }
        if (comboWasLocked && combo != null && (health == null || !health.IsDead))
            combo.enabled = comboWasEnabled;
        comboWasLocked = false;
        comboWasEnabled = false;
        if (shieldWasLocked && shield != null && (health == null || !health.IsDead))
            shield.enabled = shieldWasEnabled;
        shieldWasLocked = false;
        shieldWasEnabled = false;
    }
}
