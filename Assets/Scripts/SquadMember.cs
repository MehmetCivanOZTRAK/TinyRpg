using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SquadMember : MonoBehaviour
{
    private NavMeshAgent agent;
    private NavMeshPath path;
    private Animator animator;
    private Health health;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        path = new NavMeshPath();
        health = GetComponent<Health>();
        if (health != null)
            health.Died += OnDeath;
    }

    public bool MoveTo(Vector3 destination)
    {
        if ((health != null && !health.IsAlive) || !agent.isOnNavMesh ||
            !NavMesh.SamplePosition(destination, out NavMeshHit hit, 1.25f, agent.areaMask) ||
            !agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
            return false;

        return agent.SetPath(path);
    }

    private void Update()
    {
        if (animator != null)
            animator.SetBool("isRunning", agent.isOnNavMesh && (agent.pathPending || agent.velocity.sqrMagnitude > 0.01f));
    }

    private void OnDeath(Health _)
    {
        if (agent.isOnNavMesh)
            agent.ResetPath();
        if (animator != null)
            animator.SetBool("isRunning", false);
        enabled = false;
    }
}
