using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HealthUI : MonoBehaviour
{

    [SerializeField] private Health targetHealth;
    [SerializeField] private GameObject heartPrefab;
    [SerializeField] private Transform heartsContainer;

    private List<GameObject> hearts = new List<GameObject>();

    private void OnEnable()
    {
        if (targetHealth == null)
            return;

        targetHealth.OnHealthChanged += UpdateHearts;
        UpdateHearts(targetHealth.CurrentHealth,targetHealth.MaxHealth);
    }

    private void OnDisable()
    {
        if (targetHealth != null)
            targetHealth.OnHealthChanged -= UpdateHearts;
    }

    public void SetTarget(Health newTarget)
    {
        if (targetHealth != null)
            targetHealth.OnHealthChanged -= UpdateHearts;

        targetHealth = newTarget;

        if (isActiveAndEnabled && targetHealth != null)
            targetHealth.OnHealthChanged += UpdateHearts;

        if (targetHealth != null)
            UpdateHearts(targetHealth.CurrentHealth,targetHealth.MaxHealth);

    }

    private void UpdateHearts(int current, int maximum)
    {
        if (heartPrefab == null || heartsContainer == null)
            return;

        while (hearts.Count < maximum)
        {
            GameObject heart = Instantiate(heartPrefab, heartsContainer);

            hearts.Add(heart);
        }

        for (int i = 0; i < hearts.Count; i++)
        {
            hearts[i].SetActive(i < current);
        }
    }
}
