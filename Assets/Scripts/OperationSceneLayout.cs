using UnityEngine;

public class OperationSceneLayout : MonoBehaviour
{
    [SerializeField] private Transform playerSpawn;

    public Transform PlayerSpawn => playerSpawn;
    public OperationSpawnPoint[] SpawnPoints => GetComponentsInChildren<OperationSpawnPoint>();

    private void OnDrawGizmos()
    {
        if (playerSpawn == null)
            return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(playerSpawn.position, 0.5f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(playerSpawn.position + Vector3.up, "Squad spawn");
#endif
    }
}
