
using System.Collections;
using UnityEngine;

//Script para que parpadee un enemigo al recibir dano
[RequireComponent(typeof(Health))]
public sealed class DamageFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] sprites;
    [SerializeField, Min(1)] private int flashCount = 2;
    [SerializeField, Min(0.01f)] private float flashDuration = 0.08f;
    [SerializeField, Min(0.01f)] private float intervalDuration = 0.08f;

    private static readonly int FlashAmount =
        Shader.PropertyToID("_FlashAmount");

    private Health health;
    private MaterialPropertyBlock propertyBlock;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        health = GetComponent<Health>();
        propertyBlock = new MaterialPropertyBlock();

        if (sprites == null || sprites.Length == 0)
            sprites = GetComponentsInChildren<SpriteRenderer>(true);

        SetFlash(0f);
    }

    private void OnEnable()
    {
        health.OnDamaged += HandleDamage;
    }

    private void OnDisable()
    {
        health.OnDamaged -= HandleDamage;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        SetFlash(0f);
    }

    private void HandleDamage(int damage)
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < flashCount; i++)
        {
            SetFlash(1f);
            yield return new WaitForSeconds(flashDuration);

            SetFlash(0f);

            if (i < flashCount - 1)
                yield return new WaitForSeconds(intervalDuration);
        }

        flashCoroutine = null;
    }

    private void SetFlash(float amount)
    {
        foreach (SpriteRenderer sprite in sprites)
        {
            if (sprite == null)
                continue;

            sprite.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(FlashAmount, amount);
            sprite.SetPropertyBlock(propertyBlock);
        }
    }
}
