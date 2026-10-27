using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health), typeof(EnemyTarget), typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField, Min(1f)] private float detectionRange = 7f;
    [SerializeField, Min(0.1f)] private float attackRange = 1.6f;
    [SerializeField, Min(1)] private int damage = 10;
    [SerializeField, Min(0.1f)] private float attackInterval = 1.2f;

    private NavMeshPath path;
    private NavMeshAgent agent;
    private Health health;
    private Health target;
    private Vector3 home;
    private float nextScanTime;
    private float nextAttackTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        path = new NavMeshPath();
        health = GetComponent<Health>();
        health.Died += OnDeath;
        home = transform.position;
        agent.stoppingDistance = attackRange * 0.8f;
    }

    private void Update()
    {
        if (!health.IsAlive || !agent.isOnNavMesh)
            return;

        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + 0.25f;
            target = FindTarget();
        }

        if (target == null)
        {
            agent.stoppingDistance = 0f;
            if (Vector3.Distance(transform.position, home) > 0.25f)
                agent.SetDestination(home);
            else if (agent.hasPath)
                agent.ResetPath();
            return;
        }

        agent.stoppingDistance = attackRange * 0.8f;
        Vector3 toward = target.transform.position - transform.position;
        toward.y = 0f;
        if (toward.sqrMagnitude > attackRange * attackRange)
        {
            agent.SetDestination(target.transform.position);
            return;
        }

        if (agent.hasPath)
            agent.ResetPath();
        if (toward.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(toward), 540f * Time.deltaTime);
        if (Time.time < nextAttackTime)
            return;
        nextAttackTime = Time.time + attackInterval;
        CombatDamage.TryApply(health, target, damage);
    }

    private Health FindTarget()
    {
        // shortcut: This open arena uses distance and NavMesh reachability; add sight checks when cover exists.
        Health nearest = null;
        float nearestDistance = detectionRange * detectionRange;
        foreach (CharacterCombat combat in FindObjectsByType<CharacterCombat>(FindObjectsSortMode.None))
        {
            Health candidate = combat.GetComponent<Health>();
            if (candidate == null || !candidate.IsAlive)
                continue;
            float distance = (combat.transform.position - transform.position).sqrMagnitude;
            if (distance >= nearestDistance ||
                !agent.CalculatePath(combat.transform.position, path) || path.status != NavMeshPathStatus.PathComplete)
                continue;
            nearest = candidate;
            nearestDistance = distance;
        }
        return nearest;
    }

    private void OnDeath(Health _)
    {
        if (agent.isOnNavMesh)
            agent.ResetPath();
        agent.enabled = false;
        enabled = false;
    }

    private void OnDestroy()
    {
        if (health != null)
            health.Died -= OnDeath;
    }
}
