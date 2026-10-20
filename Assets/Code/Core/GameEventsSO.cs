using System;
using UnityEngine;


//Este codigo comunica a los distintos sistemas

[CreateAssetMenu(fileName = "GameEvents", menuName = "Game/Events/Game Events")]
public sealed class GameEventsSO : ScriptableObject
{
    // Evento para cuando se pausa el juego
    public event Action<bool> OnPauseChanged;

    // Estado del jugador
    public event Action<int, int> OnPlayerHealthChanged;
    public event Action<int> OnPlayerDamaged;
    public event Action<int> OnPlayerHealed;
    public event Action OnPlayerDied;

    // Movimiento
    public event Action OnPlayerJumped;
    public event Action OnPlayerWallJumped;
    public event Action OnPlayerDashed;

    // Combate
    public event Action<int> OnPlayerComboAttack;
    public event Action<int> OnPlayerComboHit;
    public event Action OnPlayerComboEnded;

    //Estos eventos sirven para solicitar el desbloqueo de habilidades del jugador.

    public event Action OnDoubleJumpUnlockRequested;
    public event Action OnWallJumpUnlockRequested;
    public event Action OnDashUnlockRequested;

    // Se ejecutan cuando el jugador ha recibido la habilidad.
    public event Action OnDoubleJumpUnlocked;
    public event Action OnWallJumpUnlocked;
    public event Action OnDashUnlocked;

    public void NotifyPauseChanged(bool paused) => OnPauseChanged?.Invoke(paused);

    public void NotifyPlayerHealthChanged(int currentHealth, int maxHealth) =>
        OnPlayerHealthChanged?.Invoke(currentHealth, maxHealth);

    public void NotifyPlayerDamaged(int amount) => OnPlayerDamaged?.Invoke(amount);
    public void NotifyPlayerHealed(int amount) => OnPlayerHealed?.Invoke(amount);
    public void NotifyPlayerDied() => OnPlayerDied?.Invoke();

    public void NotifyPlayerJumped() => OnPlayerJumped?.Invoke();
    public void NotifyPlayerWallJumped() => OnPlayerWallJumped?.Invoke();
    public void NotifyPlayerDashed() => OnPlayerDashed?.Invoke();

    public void NotifyPlayerComboAttack(int step) => OnPlayerComboAttack?.Invoke(step);
    public void NotifyPlayerComboHit(int step) => OnPlayerComboHit?.Invoke(step);
    public void NotifyPlayerComboEnded() => OnPlayerComboEnded?.Invoke();

    public void RequestDoubleJumpUnlock() => OnDoubleJumpUnlockRequested?.Invoke();
    public void RequestWallJumpUnlock() => OnWallJumpUnlockRequested?.Invoke();
    public void RequestDashUnlock() => OnDashUnlockRequested?.Invoke();

    public void NotifyDoubleJumpUnlocked() => OnDoubleJumpUnlocked?.Invoke();
    public void NotifyWallJumpUnlocked() => OnWallJumpUnlocked?.Invoke();
    public void NotifyDashUnlocked() => OnDashUnlocked?.Invoke();
}
