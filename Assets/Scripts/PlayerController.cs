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

        CurrentEnemyTarget = null;
        TargetChanged?.Invoke(null);
        agent.SetPath(agentpath);
        isMove = true;
        playerAnim.SetBool("isRunning", true);
        arrowparticle.transform.position = hit.point;
        arrowparticle.Play();
    }
}

