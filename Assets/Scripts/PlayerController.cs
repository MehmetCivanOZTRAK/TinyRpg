using System;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private ParticleSystem arrowparticle;

    private NavMeshAgent agent;
    private NavMeshPath agentpath;
    private Animator playerAnim;
    private Health health;
    private bool isMove;

    public EnemyTarget CurrentEnemyTarget { get; private set; }
    public event Action<EnemyTarget> TargetChanged;

    private void Awake()
    {
        playerAnim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        agentpath = new NavMeshPath();
        health = GetComponent<Health>();
        if (health != null)
            health.Died += OnDeath;
        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
            body.isKinematic = true;
    }

    private void Update()
    {
        SetInput();
        if (Input.GetKeyDown(KeyCode.Q))
            DispatchAbility(CharacterAbilityController.Slot.Q);
        if (Input.GetKeyDown(KeyCode.E))
            DispatchAbility(CharacterAbilityController.Slot.E);
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
            if (!enemy.IsAlive)
                return;

            if (agent.isOnNavMesh)
                agent.ResetPath();
            isMove = false;
            playerAnim.SetBool("isRunning", false);
            SetTarget(enemy);
            return;
        }

        if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Ground"))
            return;

        bool leaderAlive = health == null || health.IsAlive;
        if (leaderAlive && (!agent.CalculatePath(hit.point, agentpath) ||
            agentpath.status != NavMeshPathStatus.PathComplete))
            return;

        SetTarget(null);
        if (leaderAlive)
        {
            if (!agent.SetPath(agentpath))
                return;
            isMove = true;
            playerAnim.SetBool("isRunning", true);
        }
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

    private void OnDeath(Health _)
    {
        if (agent.isOnNavMesh)
            agent.ResetPath();
        isMove = false;
        playerAnim.SetBool("isRunning", false);
    }

    private void SetTarget(EnemyTarget target)
    {
        if (CurrentEnemyTarget != null)
            CurrentEnemyTarget.GetComponent<Health>().Died -= OnTargetDied;
        CurrentEnemyTarget = target;
        if (target != null)
            target.GetComponent<Health>().Died += OnTargetDied;
        foreach (CharacterCombat combat in FindObjectsByType<CharacterCombat>(FindObjectsSortMode.None))
            combat.SetTarget(target);
        TargetChanged?.Invoke(target);
    }

    private void OnTargetDied(Health _)
    {
        SetTarget(null);
    }

    private void DispatchAbility(CharacterAbilityController.Slot slot)
    {
        foreach (CharacterAbilityController member in FindObjectsByType<CharacterAbilityController>(FindObjectsSortMode.None))
            member.TryUse(slot, CurrentEnemyTarget);
    }

    private void OnDestroy()
    {
        if (CurrentEnemyTarget != null)
            CurrentEnemyTarget.GetComponent<Health>().Died -= OnTargetDied;
    }
}

