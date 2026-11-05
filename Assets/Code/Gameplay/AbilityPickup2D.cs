using UnityEngine;

// Script para activar habilidades y vida
[RequireComponent(typeof(Collider2D))]
public sealed class AbilityPickup2D : MonoBehaviour
{
    public enum AbilityType
    {
        DoubleJump,
        WallJump,
        Dash,
        HealthPotion
    }

    [SerializeField] private AbilityType ability = AbilityType.DoubleJump;
    [SerializeField] private bool destroyOnPickup = true;

    private bool collected;
    private Health potionPlayerHealth;
    private Collider2D[] pickupColliders;
    private Renderer[] pickupRenderers;
    private bool[] colliderWasEnabled;
    private bool[] rendererWasEnabled;
    private bool waitingForRespawn;

    private void Awake()
    {
        pickupColliders = GetComponentsInChildren<Collider2D>(true);
        pickupRenderers = GetComponentsInChildren<Renderer>(true);
        colliderWasEnabled = new bool[pickupColliders.Length];
        rendererWasEnabled = new bool[pickupRenderers.Length];

        for (int i = 0; i < pickupColliders.Length; i++)
            colliderWasEnabled[i] = pickupColliders[i].enabled;

        for (int i = 0; i < pickupRenderers.Length; i++)
            rendererWasEnabled[i] = pickupRenderers[i].enabled;
    }

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;


        if (ability == AbilityType.HealthPotion)
        {
            Health health = player.GetComponent<Health>();

            if (health == null || health.IsDead ||
                health.CurrentHealth >= health.MaxHealth)
                return;

            PlayerAnimator playerAnimator =
                player.GetComponentInChildren<PlayerAnimator>();

            if (playerAnimator == null ||
                !playerAnimator.PlayHealthPotionAnimation())
                return;

            health.Heal(2);

            collected = true;
            potionPlayerHealth = health;
            potionPlayerHealth.OnDied += HandlePlayerDeath;
            potionPlayerHealth.OnHealthChanged += HandleHealthChanged;

            SetPotionVisible(false);
            return;
        }


        GameOrchestrator orchestrator = GameOrchestrator.Instance;
        if (orchestrator == null)
            return;

        switch (ability)
        {
            case AbilityType.DoubleJump:
                orchestrator.UnlockDoubleJump();
                break;

            case AbilityType.WallJump:
                orchestrator.UnlockWallJump();
                break;

            case AbilityType.Dash:
                orchestrator.UnlockDash();
                break;
        }

        collected = true;
        if (destroyOnPickup)
            Destroy(gameObject);
    }


    private void HandlePlayerDeath()
    {
        waitingForRespawn = true;
    }

    private void HandleHealthChanged(int current, int maximum)
    {
        if (waitingForRespawn && current > 0)
            RestorePotion();
    }

    private void RestorePotion()
    {
        if (potionPlayerHealth != null)
        {
            potionPlayerHealth.OnDied -= HandlePlayerDeath;
            potionPlayerHealth.OnHealthChanged -= HandleHealthChanged;
            potionPlayerHealth = null;
        }

        waitingForRespawn = false;
        collected = false;

        SetPotionVisible(true);
    }

    private void SetPotionVisible(bool visible)
    {
        for (int i = 0; i < pickupColliders.Length; i++)
        {
            if (pickupColliders[i] != null)
                pickupColliders[i].enabled = visible && colliderWasEnabled[i];
        }

        for (int i = 0; i < pickupRenderers.Length; i++)
        {
            if (pickupRenderers[i] != null)
                pickupRenderers[i].enabled = visible && rendererWasEnabled[i];
        }
    }

    private void OnDestroy()
    {
        if (potionPlayerHealth != null)
        {
            potionPlayerHealth.OnDied -= HandlePlayerDeath;
            potionPlayerHealth.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private bool IsAbilityUnlocked(PlayerController player)
    {
        switch (ability)
        {
            case AbilityType.DoubleJump:
                return player.IsDoubleJumpUnlocked;
            case AbilityType.WallJump:
                return player.IsWallJumpUnlocked;
            case AbilityType.Dash:
                return player.IsDashUnlocked;
            default:
                return false;
        }
    }
}
