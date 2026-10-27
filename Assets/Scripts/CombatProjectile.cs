using UnityEngine;

public class CombatProjectile : MonoBehaviour
{
    private Health attacker;
    private EnemyTarget target;
    private int damage;
    private float speed;
    private bool magic;
    private Material material;

    public static void Spawn(Vector3 origin, Health attacker, EnemyTarget target, int damage, bool magic)
    {
        // shortcut: Primitive projectiles allocate each shot; pool them when combat density grows.
        GameObject visual = GameObject.CreatePrimitive(magic ? PrimitiveType.Sphere : PrimitiveType.Cylinder);
        visual.name = magic ? "Arcane Bolt" : "Arrow";
        Destroy(visual.GetComponent<Collider>());
        visual.transform.position = origin;
        visual.transform.localScale = magic ? Vector3.one * 0.24f : new Vector3(0.055f, 0.18f, 0.055f);
        CombatProjectile projectile = visual.AddComponent<CombatProjectile>();
        projectile.attacker = attacker;
        projectile.target = target;
        projectile.damage = damage;
        projectile.speed = magic ? 11f : 18f;
        projectile.magic = magic;
        projectile.material = visual.GetComponent<Renderer>().material;
        projectile.material.color = magic ? new Color(0.45f, 0.3f, 1f) : new Color(0.75f, 0.5f, 0.2f);
    }

    private void Update()
    {
        if (attacker == null || !attacker.IsAlive || target == null || !target.IsAlive)
        {
            Destroy(gameObject);
            return;
        }

        Collider hitbox = target.GetComponent<Collider>();
        Vector3 destination = hitbox != null ? hitbox.bounds.center : target.transform.position + Vector3.up;
        Vector3 direction = destination - transform.position;
        if (direction.magnitude <= speed * Time.deltaTime + 0.1f)
        {
            CombatDamage.TryApply(attacker, target.GetComponent<Health>(), damage);
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
        if (!magic)
            transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }
}
