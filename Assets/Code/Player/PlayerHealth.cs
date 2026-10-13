
using UnityEngine;


//Este script sirve principalmente para que el juego sepa que el health que esta cambiando es el del jugador y no el de otro enemigo
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private PlayerConfigSO config;
    [SerializeField] private GameEventsSO gameEvents;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();

        health.Initialize(config.vidaMaxima, false);
    }

    private void OnEnable()
    {
        if (health == null || config == null)
            return;

        health.OnHealthChanged += HandleHealthChanged;
        health.OnDamaged += HandleDamaged;
        health.OnHealed += HandleHealed;
        health.OnDied += HandleDeath;
    }

    private void Start()
    {
        if (config != null)
            health.NotifyHealthChanged();
    }

    private void OnDisable()
    {
        if (health == null)
            return;

        health.OnHealthChanged -= HandleHealthChanged;
        health.OnDamaged -= HandleDamaged;
        health.OnHealed -= HandleHealed;
        health.OnDied -= HandleDeath;
    }

    private void HandleHealthChanged(int current, int maximum)
    {
        gameEvents?.NotifyPlayerHealthChanged(current, maximum);
    }

    private void HandleDamaged(int amount)
    {
        gameEvents?.NotifyPlayerDamaged(amount);
    }

    private void HandleHealed(int amount)
    {
        gameEvents?.NotifyPlayerHealed(amount);
    }

    private void HandleDeath()
    {
        gameEvents?.NotifyPlayerDied();
    }
}
