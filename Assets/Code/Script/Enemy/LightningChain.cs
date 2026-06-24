using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LightningChain
{
    public static void Apply(GameObject firstHit, float damage, float range, int bounces)
    {
        var runner = new GameObject("LightningChainRunner").AddComponent<Runner>();
        runner.StartCoroutine(Chain(firstHit, damage, range, bounces, runner.gameObject));
    }

    private static IEnumerator Chain(GameObject firstHit, float damage, float range, int bounces, GameObject self)
    {
        var hit     = new HashSet<GameObject> { firstHit };
        var current = firstHit;

        for (int i = 0; i < bounces; i++)
        {
            yield return new WaitForSeconds(0.3f); // délai entre rebonds — à ajuster pour le VFX

            var next = FindNearest(current.transform.position, range, hit);
            if (next == null)
            {
                Debug.Log($"[Lightning] Rebond {i + 1} : aucune cible dans la range ({range}m), chaîne arrêtée.");
                break;
            }

            hit.Add(next);
            next.GetComponent<EnemyHealth>()?.TakeDamage(damage, ElementType.Lightning, false);
            Debug.Log($"[Lightning] Rebond {i + 1} : {current.name} → {next.name} ({damage} dmg)");
            current = next;
        }

        Object.Destroy(self);
    }

    private static GameObject FindNearest(Vector3 origin, float range, HashSet<GameObject> exclude)
    {
        var        colliders = Physics.OverlapSphere(origin, range);
        GameObject nearest   = null;
        float      minDist   = float.MaxValue;

        foreach (var col in colliders)
        {
            if (!col.CompareTag("Enemy"))         continue;
            if (exclude.Contains(col.gameObject)) continue;

            float dist = Vector3.Distance(origin, col.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = col.gameObject;
            }
        }

        return nearest;
    }

    private class Runner : MonoBehaviour { }
}
