
using System.Collections.Generic;
using UnityEngine;


//Zona de dano al player
public sealed class DamageArea2D : MonoBehaviour
{
    public enum DamageMode
    {
        OncePerContact,
        Repeated
    }

    [SerializeField, Min(1)] private int damage = 1;

    [SerializeField] private bool onlyPlayer = true;

    [SerializeField] private DamageMode damageMode =
        DamageMode.Repeated;

    [SerializeField, Min(0.05f)]
    private float damageInterval = 1f;

    private sealed class ContactState
    {
        public readonly HashSet<Collider2D> colliders = new();
        public float nextDamageTime;
    }

    private readonly Dictionary<ITakeDamage, ContactState>
        contacts = new();

    private ITakeDamage ownerDamageable;

    private void Awake()
    {
        ownerDamageable = GetComponentInParent<ITakeDamage>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ProcessContact(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        ProcessContact(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        ITakeDamage target =
            other.GetComponentInParent<ITakeDamage>();

        if (target == null)
            return;

        if (!contacts.TryGetValue(target, out ContactState state))
            return;

        state.colliders.Remove(other);

        if (state.colliders.Count == 0)
            contacts.Remove(target);
    }

    private void ProcessContact(Collider2D other)
    {
        if (onlyPlayer &&
            other.GetComponentInParent<PlayerHealth>() == null)
            return;

        ITakeDamage target =
            other.GetComponentInParent<ITakeDamage>();

        if (target == null)
            return;

        if (ReferenceEquals(target, ownerDamageable))
            return;

        bool firstContact = !contacts.TryGetValue(
            target, out ContactState state);

        if (firstContact)
        {
            state = new ContactState();
            contacts.Add(target, state);
        }

        state.colliders.Add(other);

        if (firstContact)
        {
            ApplyDamage(target, state);
            return;
        }

        if (damageMode == DamageMode.Repeated &&
            Time.time >= state.nextDamageTime)
        {
            ApplyDamage(target, state);
        }
    }

    private void ApplyDamage(
        ITakeDamage target,
        ContactState state)
    {
        target.TakeDamage(damage);

        state.nextDamageTime =
            Time.time + Mathf.Max(0.05f, damageInterval);
    }

    private void OnDisable()
    {
        contacts.Clear();
    }
}
