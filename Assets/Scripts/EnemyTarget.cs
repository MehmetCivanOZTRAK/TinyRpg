using System.Collections;
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
        Collider hitbox = GetComponent<Collider>();
        if (hitbox != null)
            hitbox.enabled = false;
        StartCoroutine(Disappear());
    }

    private IEnumerator Disappear()
    {
        Vector3 startingScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(startingScale, Vector3.zero, elapsed / 0.35f);
            yield return null;
        }
        // shortcut: Capsule shrink is the death visual until a rigged enemy clip is added.
        gameObject.SetActive(false);
    }
}
