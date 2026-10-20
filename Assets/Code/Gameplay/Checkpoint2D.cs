
using UnityEngine;
using UnityEngine.Events;


// Este script es para que se lo coloquemos a los checkpoints del nivel

public sealed class Checkpoint2D : MonoBehaviour
{
    [SerializeField] private CheckpointManager manager;
    [SerializeField] private Transform spawnPoint;

    [SerializeField] private UnityEvent onActivated;
    [SerializeField] private UnityEvent onDeactivated;

    public bool IsActive { get; private set; }

    public Vector3 RespawnPosition => spawnPoint.position;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        manager?.ActivateCheckpoint(this);
    }

    public void SetActive(bool active)
    {
        if (IsActive == active)
            return;

        IsActive = active;

        if (active)
            onActivated?.Invoke();
        else
            onDeactivated?.Invoke();
    }

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
            col.isTrigger = true;
    }
}
