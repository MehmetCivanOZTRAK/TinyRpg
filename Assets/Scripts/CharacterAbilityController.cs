using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class CharacterAbilityController : MonoBehaviour
{
    public enum Slot { Q, E }

    [SerializeField] private AbilityDefinition qAbility;
    [SerializeField] private AbilityDefinition eAbility;

    private Health health;
    private float nextQTime;
    private float nextETime;

    public event Action<Slot, AbilityDefinition, EnemyTarget> Used;
    public AbilityDefinition QAbility => qAbility;
    public AbilityDefinition EAbility => eAbility;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    public float CooldownRemaining(Slot slot)
    {
        return Mathf.Max(0f, (slot == Slot.Q ? nextQTime : nextETime) - Time.time);
    }

    public bool TryUse(Slot slot, EnemyTarget target)
    {
        AbilityDefinition ability = slot == Slot.Q ? qAbility : eAbility;
        if (!isActiveAndEnabled || ability == null || health == null || !health.IsAlive ||
            CooldownRemaining(slot) > 0f)
            return false;

        if (ability.Targeting == AbilityDefinition.TargetingType.Enemy &&
            (target == null || !target.IsAlive ||
             (ability.Range > 0f && Vector3.Distance(transform.position, target.transform.position) > ability.Range)))
            return false;

        if (slot == Slot.Q)
            nextQTime = Time.time + ability.Cooldown;
        else
            nextETime = Time.time + ability.Cooldown;
        Used?.Invoke(slot, ability, target);
        return true;
    }
}
