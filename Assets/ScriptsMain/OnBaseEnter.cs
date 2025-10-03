using UnityEngine;
using UnityEngine.UI;
using System;

public class BaseTrigger : MonoBehaviour
{
    [SerializeField] private BaseDefinition baseDef;
    [SerializeField] private Slider healthBar;
    [SerializeField] private float damagePerEnter = 15f;

    private void Start()
    {
        if (!baseDef) baseDef = GetComponent<BaseDefinition>();
        if (healthBar) healthBar.value = baseDef.GetHealthPercentage() / 100f;
        baseDef.OnHealthChanged += h => { if (healthBar) healthBar.value = baseDef.GetHealthPercentage() / 100f; };
        baseDef.OnBaseDestroyed += () => Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        
        if (!collision.CompareTag("Unit")) return;

        var unitDef = collision.GetComponent<UnitsDefinition>();
        if (!unitDef) return;

        Debug.Log($"Triggered by: {collision.name}, tag: {collision.tag}");

        // Проверка на противоположную команду (пример)
        if ((int)unitDef.GetTeam() != (int)baseDef.GetTeam())
            baseDef.TakeDamage(damagePerEnter);
    }
}
