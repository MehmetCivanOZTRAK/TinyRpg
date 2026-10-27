using System;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private ParticleSystem arrowparticle;

    private NavMeshAgent agent;
    private NavMeshPath agentpath;
    private Animator playerAnim;
    private bool isMove;

    public EnemyTarget CurrentEnemyTarget { get; private set; }
    public event Action<EnemyTarget> TargetChanged;

    private void Awake()
    {
        playerAnim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        agentpath = new NavMeshPath();
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
            body.isKinematic = true;
    }

    private void Update()
    {
        SetInput();
        if (isMove && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            isMove = false;
            playerAnim.SetBool("isRunning", false);
        }
    }

    private void SetInput()
    {
        if (!Input.GetMouseButtonDown(0) || Camera.main == null ||
            !Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
            return;

        EnemyTarget enemy = hit.collider.GetComponentInParent<EnemyTarget>();
        if (enemy != null)
        {
            agent.ResetPath();
            isMove = false;
            playerAnim.SetBool("isRunning", false);
            CurrentEnemyTarget = enemy;
            TargetChanged?.Invoke(enemy);
            return;
        }

        if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Ground") ||
            !agent.CalculatePath(hit.point, agentpath) ||
            agentpath.status != NavMeshPathStatus.PathComplete)
            return;

        if (!agent.SetPath(agentpath))
            return;

        CurrentEnemyTarget = null;
        TargetChanged?.Invoke(null);
        isMove = true;
        playerAnim.SetBool("isRunning", true);
        arrowparticle.transform.position = hit.point;
        arrowparticle.Play();

        Vector3 forward = hit.point - transform.position;
        forward.y = 0;
        forward = forward.sqrMagnitude > 0.01f ? forward.normalized : transform.forward;
        Vector3 right = new Vector3(forward.z, 0, -forward.x);
        int index = 0;
        foreach (SquadMember member in FindObjectsByType<SquadMember>(FindObjectsSortMode.InstanceID))
        {
            if (!member.isActiveAndEnabled)
                continue;

            int row = index / 2 + 1;
            float side = index % 2 == 0 ? -1f : 1f;
            member.MoveTo(hit.point - forward * row * 1.4f + right * side * 1.4f);
            index++;
        }
    }
}

