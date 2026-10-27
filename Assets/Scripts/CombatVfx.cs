using UnityEngine;

public static class CombatVfx
{
    public static void Play(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return;
        GameObject effect = Object.Instantiate(prefab, position, rotation);
        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
            particles.Play(true);
        // shortcut: Effects are short lived; pool them if combat density grows.
        Object.Destroy(effect, 4f);
    }
}
