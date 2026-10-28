using UnityEngine;

public class OperationSpawnPoint : MonoBehaviour
{
    [SerializeField] private bool requiredTarget = true;
    [SerializeField] private EnemyTarget enemyPrefab;

    public bool RequiredTarget => requiredTarget;
    public EnemyTarget EnemyPrefab => enemyPrefab;

    private void OnDrawGizmos()
    {
        Gizmos.color = requiredTarget ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.35f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up, name);
#endif
    }
}
