using System.Collections.Generic;
using UnityEngine;

public class AbilityProjectile : MonoBehaviour
{
    private readonly HashSet<EnemyTarget> struck = new HashSet<EnemyTarget>();
    private Health attacker;
    private Vector3 direction;
    private Material material;
    private int damage;
    private float remaining;
    private bool pierces;

    public static void Spawn(Vector3 origin, EnemyTarget target, Health attacker,
        AbilityDefinition ability, float spreadDegrees, bool pierces)
    {
        GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arrow.name = pierces ? "Piercing Arrow" : "Multi Shot Arrow";
        Destroy(arrow.GetComponent<Collider>());
        arrow.transform.position = origin;
        arrow.transform.localScale = new Vector3(0.07f, 0.25f, 0.07f);
        AbilityProjectile projectile = arrow.AddComponent<AbilityProjectile>();
        Collider hitbox = target.GetComponent<Collider>();
        Vector3 destination = hitbox != null ? hitbox.bounds.center : target.transform.position + Vector3.up;
        Vector3 aim = destination - origin;
        aim.y = 0f;
        projectile.direction = Quaternion.Euler(0f, spreadDegrees, 0f) * aim.normalized;
        arrow.transform.rotation = Quaternion.FromToRotation(Vector3.up, projectile.direction);
        projectile.attacker = attacker;
        projectile.damage = ability.Damage;
        projectile.remaining = ability.Range;
        projectile.pierces = pierces;
        projectile.material = arrow.GetComponent<Renderer>().material;
        projectile.material.color = new Color(0.85f, 0.55f, 0.2f);
    }

    private void Update()
    {
        if (attacker == null || !attacker.IsAlive || remaining <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 before = transform.position;
        float step = Mathf.Min(22f * Time.deltaTime, remaining);
        Vector3 after = before + direction * step;
        foreach (EnemyTarget enemy in FindObjectsByType<EnemyTarget>(FindObjectsSortMode.None))
        {
            if (!enemy.IsAlive || struck.Contains(enemy))
                continue;
            Collider hitbox = enemy.GetComponent<Collider>();
            Vector3 center = hitbox != null ? hitbox.bounds.center : enemy.transform.position + Vector3.up;
            Vector3 segment = after - before;
            float t = segment.sqrMagnitude > 0f ?
                Mathf.Clamp01(Vector3.Dot(center - before, segment) / segment.sqrMagnitude) : 0f;
            Vector3 offset = center - (before + segment * t);
            offset.y = 0f;
            if (offset.sqrMagnitude > 0.65f * 0.65f)
                continue;
            struck.Add(enemy);
            CombatDamage.TryApply(attacker, enemy.GetComponent<Health>(), damage);
            if (!pierces)
            {
                Destroy(gameObject);
                return;
            }
        }
        transform.position = after;
        remaining -= step;
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }
}
