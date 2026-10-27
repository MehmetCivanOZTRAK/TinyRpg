using System;
using UnityEngine;

public interface IDamageable
{
    bool IsAlive { get; }
    bool TakeDamage(int amount);
}

[DisallowMultipleComponent]
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField, Min(1)] private int maxHealth = 100;

    public int CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0;
    public event Action<Health> Died;

    private void Awake()
    {
        CurrentHealth = Mathf.Max(1, maxHealth);
    }

    public bool TakeDamage(int amount)
    {
        if (amount <= 0 || !IsAlive)
            return false;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        if (!IsAlive)
            Died?.Invoke(this);
        return true;
    }
}

public static class CombatDamage
{
    public static bool TryApply(Health attacker, IDamageable target, int amount)
    {
        return attacker != null && attacker.IsAlive && target != null && target.IsAlive &&
            target.TakeDamage(amount);
    }
}
