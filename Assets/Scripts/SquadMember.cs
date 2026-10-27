using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SquadMember : MonoBehaviour
{
    private NavMeshAgent agent;
    private NavMeshPath path;
    private Animator animator;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        path = new NavMeshPath();
    }

    public bool MoveTo(Vector3 destination)
    {
        if (!agent.isOnNavMesh || !NavMesh.SamplePosition(destination, out NavMeshHit hit, 1.25f, agent.areaMask) ||
            !agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
            return false;

        return agent.SetPath(path);
    }

    private void Update()
    {
        if (animator != null)
            animator.SetBool("isRunning", agent.isOnNavMesh && (agent.pathPending || agent.velocity.sqrMagnitude > 0.01f));
    }
}
