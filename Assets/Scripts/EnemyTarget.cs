using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyTarget : MonoBehaviour
{
    public bool IsAlive => GetComponent<Health>().IsAlive;

    private void Awake()
    {
        GetComponent<Health>().Died += OnDeath;
    }

    private void OnDeath(Health _)
    {
        // shortcut: Prototype enemies disappear immediately; add a death animation with combat visuals.
        gameObject.SetActive(false);
    }
}
