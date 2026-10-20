
using UnityEngine;


// Script para activar las habilidades.
[RequireComponent(typeof(Collider2D))]
public sealed class AbilityPickup2D : MonoBehaviour
{
    public enum AbilityType
    {
        DoubleJump,
        WallJump,
        Dash
    }

    [SerializeField] private AbilityType ability = AbilityType.DoubleJump;
    [SerializeField] private bool destroyOnPickup = true;

    private bool collected;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
            return;

        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        GameOrchestrator orchestrator = GameOrchestrator.Instance;


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
