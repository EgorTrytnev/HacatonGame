using UnityEngine;
using System;

public class BaseDefinition : MonoBehaviour
{
    [SerializeField] private CollorTeam team;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    public Action OnBaseDestroyed;
    public Action<float> OnHealthChanged;

    public CollorTeam GetTeam() => team;
    public float GetCurrentHealth() => currentHealth;
    public float GetHealthPercentage() => (currentHealth / maxHealth) * 100f;
    public bool IsAlive() => currentHealth > 0;

    public void TakeDamage(float amount)
    {
        if (!IsAlive()) return;
        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);
        OnHealthChanged?.Invoke(currentHealth);
        if (currentHealth <= 0) OnBaseDestroyed?.Invoke();
    }
}