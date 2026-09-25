using System;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{   
    
    
    [Header("Navmesh")]
    private NavMeshAgent agent;
    private NavMeshPath agentpath;
    
    [Header("Particle System")]
    [SerializeField] ParticleSystem arrowparticle;
    
    private Vector3 targetposition;
        private bool isMove = false;
        private Ray ray;
    [Header("Animation")] 
   private Animator _playerAnim;

    private void Awake()
    {
        _playerAnim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        agentpath = new NavMeshPath();
    }

    private void Update()
    {
        
    SetInput();
        if (isMove && !agent.pathPending && 
            agent.remainingDistance <= agent.stoppingDistance)
        {
            isMove = false;
            _playerAnim.SetBool("isRunning",isMove);
        }
    }

    private void SetInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (CanReachTarget())
            {
                agent.SetPath(agentpath);
                isMove = true;
                _playerAnim.SetBool("isRunning",isMove);
            }
        }
    }
    
    private Vector3 GetMouseWorldPosition()
    {
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray,out RaycastHit hit))
        {
            arrowparticle.transform.position = hit.point;
            arrowparticle.Play();
            return hit.point;
        }

        return Vector3.zero;
    }

    private bool CanReachTarget()
    {
        if (agent.CalculatePath(GetMouseWorldPosition(),agentpath) && 
            agentpath.status == NavMeshPathStatus.PathComplete)
        {
            return true;
        }

        return false;
    }
}

