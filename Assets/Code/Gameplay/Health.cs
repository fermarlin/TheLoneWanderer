
using System;
using UnityEngine;


//Este codigo es general, tanto para el player como para los enemigos
public sealed class Health : MonoBehaviour, ITakeDamage
{
    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = 6;
    [SerializeField] private PlayerVFX hitEffect;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;


    public bool HasDamageSource { get; private set; }
    public Vector2 DamageSourcePosition { get; private set; }

    public event Action<int, int> OnHealthChanged;
    public event Action<int> OnDamaged;
    public event Action<int> OnHealed;
    public event Action OnDied;

    private bool initialized;

    private void Awake()
    {
        if (!initialized)
            Initialize(maxHealth);
    }

    public void Initialize(int health)
    {
        maxHealth = Mathf.Max(1, health);
        CurrentHealth = maxHealth;
        initialized = true;
    }

    public void TakeDamage(int amount)
    {
        ApplyDamage(amount, false, Vector2.zero);
    }

    public void TakeDamage(int amount, Vector2 attackerPosition)
    {
        ApplyDamage(amount, true, attackerPosition);
    }

    private void ApplyDamage(int amount, bool hasSource, Vector2 sourcePosition)
    {
        if (amount <= 0 || IsDead)
            return;

        int actualDamage = Mathf.Min(amount, CurrentHealth);
        CurrentHealth -= actualDamage;

        HasDamageSource = hasSource;
        DamageSourcePosition = sourcePosition;

        OnDamaged?.Invoke(actualDamage);
        NotifyHealthChanged();
        HitVFX();
        if (IsDead)
        {
            OnDied?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead)
            return;

        int previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);

        int actualHealing = CurrentHealth - previousHealth;

        if (actualHealing <= 0)
            return;

        OnHealed?.Invoke(actualHealing);
        NotifyHealthChanged();
    }

    public void RestoreHealth()
    {
        CurrentHealth = maxHealth;
        NotifyHealthChanged();
    }

    private void HitVFX(){
        if (hitEffect != null){
            hitEffect.PlayVFX();
        }
            
    }

    public void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
