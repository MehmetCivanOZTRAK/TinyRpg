using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health), typeof(NavMeshAgent))]
public class CharacterCombat : MonoBehaviour
{
    public enum Class { Warrior, Archer, Mage }

    [SerializeField] private Class characterClass;
    [SerializeField] private GameObject castEffect;
    [SerializeField] private GameObject hitEffect;

    private Health health;
    private NavMeshAgent agent;
    private Animator animator;
    private Transform attackOrigin;
    private EnemyTarget target;
    private float normalStoppingDistance;
    private float nextAttackTime;
    private float nextRepathTime;
    private bool chasing;
    private bool attacking;

    public EnemyTarget Target => target;
    public Class CharacterClass => characterClass;
    private float Range => characterClass == Class.Warrior ? 2f : characterClass == Class.Archer ? 8f : 7f;
    private float Interval => characterClass == Class.Archer ? 0.8f : characterClass == Class.Warrior ? 1.1f : 1.3f;
    private int Damage => characterClass == Class.Warrior ? 20 : characterClass == Class.Archer ? 12 : 15;

    private void Awake()
    {
        health = GetComponent<Health>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        normalStoppingDistance = agent.stoppingDistance;

        string originName = characterClass == Class.Warrior ? "SwordHitOrigin" :
            characterClass == Class.Archer ? "ArrowSpawnOrigin" : "StaffCastOrigin";
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child.name == originName)
            {
                attackOrigin = child;
                break;
            }
    }

    public void SetTarget(EnemyTarget enemy)
    {
        if (target == enemy)
            return;

        StopAllCoroutines();
        attacking = false;
        target = enemy != null && enemy.IsAlive && health.IsAlive ? enemy : null;
        nextAttackTime = Time.time;
        nextRepathTime = 0f;
        if (chasing && agent.isOnNavMesh)
            agent.ResetPath();
        chasing = false;
        agent.stoppingDistance = normalStoppingDistance;
        if (target == null && animator != null)
            animator.SetBool("isRunning", false);
    }

    private void Update()
    {
        if (target == null)
            return;
        if (!health.IsAlive || !target.IsAlive)
        {
            SetTarget(null);
            return;
        }

        Vector3 toward = target.transform.position - transform.position;
        toward.y = 0f;
        if (toward.sqrMagnitude > Range * Range)
        {
            if (agent.isOnNavMesh && Time.time >= nextRepathTime)
            {
                agent.stoppingDistance = Range * 0.8f;
                chasing = agent.SetDestination(target.transform.position);
                nextRepathTime = Time.time + 0.2f;
            }
            if (animator != null)
                animator.SetBool("isRunning", chasing);
            return;
        }

        if (chasing && agent.isOnNavMesh)
            agent.ResetPath();
        chasing = false;
        if (animator != null)
            animator.SetBool("isRunning", false);
        if (toward.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(toward), 720f * Time.deltaTime);
        if (!attacking && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + Interval;
            StartCoroutine(Strike(target));
        }
    }

    private IEnumerator Strike(EnemyTarget aimedTarget)
    {
        attacking = true;
        if (animator != null)
            animator.SetTrigger("Attack");
        yield return new WaitForSeconds(0.25f);
        if (aimedTarget == target && health.IsAlive && aimedTarget != null && aimedTarget.IsAlive &&
            Vector3.Distance(transform.position, aimedTarget.transform.position) <= Range + 0.25f)
        {
            Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position + Vector3.up;
            CombatVfx.Play(castEffect, origin, transform.rotation);
            if (characterClass == Class.Warrior)
            {
                if (CombatDamage.TryApply(health, aimedTarget.GetComponent<Health>(), Damage))
                    CombatVfx.Play(hitEffect, aimedTarget.transform.position + Vector3.up, Quaternion.identity);
            }
            else
                CombatProjectile.Spawn(origin, health, aimedTarget, Damage,
                    characterClass == Class.Mage, hitEffect);
        }
        attacking = false;
    }
}
