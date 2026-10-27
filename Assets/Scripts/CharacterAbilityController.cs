using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
        StartCoroutine(Cast(ability, target));
        return true;
    }

    private IEnumerator Cast(AbilityDefinition ability, EnemyTarget target)
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null && !string.IsNullOrEmpty(ability.AnimationTrigger))
            animator.SetTrigger(ability.AnimationTrigger);
        if (ability.CastTime > 0f)
            yield return new WaitForSeconds(ability.CastTime);
        if (!health.IsAlive || target == null || !target.IsAlive)
            yield break;

        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(direction);

        switch (ability.Effect)
        {
            case AbilityDefinition.EffectType.HeavySlash:
                CombatVfx.Play(ability.VisualEffect, transform.position + transform.forward, transform.rotation);
                DamageArea(transform.position, ability.Radius, ability.Damage, true, ability.ImpactEffect);
                break;
            case AbilityDefinition.EffectType.BattleCharge:
                CombatVfx.Play(ability.VisualEffect, transform.position, transform.rotation);
                yield return Charge(target, ability);
                break;
            case AbilityDefinition.EffectType.PiercingArrow:
                CombatVfx.Play(ability.VisualEffect, AttackOrigin("ArrowSpawnOrigin"), transform.rotation);
                AbilityProjectile.Spawn(AttackOrigin("ArrowSpawnOrigin"), target, health, ability, 0f, true);
                break;
            case AbilityDefinition.EffectType.MultiShot:
                CombatVfx.Play(ability.VisualEffect, AttackOrigin("ArrowSpawnOrigin"), transform.rotation);
                for (int i = -1; i <= 1; i++)
                    AbilityProjectile.Spawn(AttackOrigin("ArrowSpawnOrigin"), target, health, ability, i * 9f, false);
                break;
            case AbilityDefinition.EffectType.ArcaneBolt:
                CombatVfx.Play(ability.VisualEffect, AttackOrigin("StaffCastOrigin"), transform.rotation);
                CombatProjectile.Spawn(AttackOrigin("StaffCastOrigin"), health, target, ability.Damage, true, ability.ImpactEffect);
                break;
            case AbilityDefinition.EffectType.ArcaneExplosion:
                CombatVfx.Play(ability.VisualEffect, target.transform.position, Quaternion.identity);
                DamageArea(target.transform.position, ability.Radius, ability.Damage, false, ability.ImpactEffect);
                break;
        }
    }

    private Vector3 AttackOrigin(string name)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child.name == name)
                return child.position;
        return transform.position + Vector3.up;
    }

    private void DamageArea(Vector3 center, float radius, int damage, bool inFront, GameObject impactEffect)
    {
        foreach (EnemyTarget enemy in FindObjectsByType<EnemyTarget>(FindObjectsSortMode.None))
        {
            if (!enemy.IsAlive)
                continue;
            Vector3 offset = enemy.transform.position - center;
            offset.y = 0f;
            if (offset.sqrMagnitude > radius * radius ||
                (inFront && offset.sqrMagnitude > 0.01f &&
                 Vector3.Dot(transform.forward, offset.normalized) < 0.4f))
                continue;
            if (CombatDamage.TryApply(health, enemy.GetComponent<Health>(), damage))
                CombatVfx.Play(impactEffect, enemy.transform.position + Vector3.up, Quaternion.identity);
        }
    }

    private IEnumerator Charge(EnemyTarget target, AbilityDefinition ability)
    {
        CharacterCombat combat = GetComponent<CharacterCombat>();
        if (combat != null)
            combat.SetTarget(null);
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        Vector3 start = transform.position;
        Vector3 toward = target.transform.position - start;
        toward.y = 0f;
        Vector3 destination = start + toward.normalized * Mathf.Max(0f, toward.magnitude - 0.8f);
        if (agent != null && agent.isOnNavMesh && agent.Raycast(destination, out NavMeshHit hit))
            destination = hit.position;

        HashSet<EnemyTarget> struck = new HashSet<EnemyTarget>();
        DamageChargeSegment(start, start, ability.Damage, ability.ImpactEffect, struck);
        float speed = Mathf.Max(8f, ability.Range / 0.25f);
        while (health.IsAlive && target != null && target.IsAlive &&
               Vector3.Distance(transform.position, destination) > 0.05f)
        {
            Vector3 before = transform.position;
            Vector3 after = Vector3.MoveTowards(before, destination, speed * Time.deltaTime);
            if (agent != null && agent.isOnNavMesh)
                agent.Move(after - before);
            else
                transform.position = after;
            DamageChargeSegment(before, transform.position, ability.Damage, ability.ImpactEffect, struck);
            yield return null;
        }
        if (combat != null)
            combat.SetTarget(target);
    }

    private void DamageChargeSegment(Vector3 start, Vector3 end, int damage, GameObject impactEffect, HashSet<EnemyTarget> struck)
    {
        Vector3 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        foreach (EnemyTarget enemy in FindObjectsByType<EnemyTarget>(FindObjectsSortMode.None))
        {
            if (!enemy.IsAlive || struck.Contains(enemy))
                continue;
            float t = lengthSquared > 0f ?
                Mathf.Clamp01(Vector3.Dot(enemy.transform.position - start, segment) / lengthSquared) : 0f;
            Vector3 nearest = start + segment * t;
            if (Vector3.Distance(enemy.transform.position, nearest) > 1f)
                continue;
            struck.Add(enemy);
            if (CombatDamage.TryApply(health, enemy.GetComponent<Health>(), damage))
                CombatVfx.Play(impactEffect, enemy.transform.position + Vector3.up, Quaternion.identity);
        }
    }

}
