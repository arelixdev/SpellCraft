using System.Collections.Generic;
using UnityEngine;

public class OrbitalManager : MonoBehaviour
{
    private struct OrbitalSlot
    {
        public GameObject Prefab;
        public SpellContext Ctx;
        public List<GameObject> Instances;
    }

    private readonly List<OrbitalSlot> _slots = new();

    // Called each time a permanent orbital spell fires.
    // If a slot with this prefab already exists and the count matches, only updates ctx params.
    // If count changed, destroys and recreates.
    public void Register(GameObject prefab, SpellContext ctx, int count)
    {
        int existing = FindSlot(prefab);

        if (existing >= 0)
        {
            var slot = _slots[existing];

            if (slot.Instances.Count == count)
            {
                // Update params on existing orbitals without recreating
                slot.Ctx = ctx;
                _slots[existing] = slot;
                foreach (var go in slot.Instances)
                    go.GetComponent<OrbitalProjectile>()?.UpdateContext(ctx);
            }
            else
            {
                // Count changed — rebuild
                DestroySlot(existing);
                _slots.RemoveAt(existing);
                SpawnSlot(prefab, ctx, count);
            }
        }
        else
        {
            SpawnSlot(prefab, ctx, count);
        }
    }

    public void RemoveSlot(GameObject prefab)
    {
        int idx = FindSlot(prefab);
        if (idx < 0) return;
        DestroySlot(idx);
        _slots.RemoveAt(idx);
    }

    // Destroys every registered orbital whose prefab isn't in keepPrefabs — called after
    // a full graph re-traversal so orbitals dropped from a slot (node disconnected/removed)
    // actually disappear instead of orbiting forever with no live SpellCaster slot behind them.
    public void PruneExcept(HashSet<GameObject> keepPrefabs)
    {
        for (int i = _slots.Count - 1; i >= 0; i--)
        {
            if (keepPrefabs.Contains(_slots[i].Prefab)) continue;
            DestroySlot(i);
            _slots.RemoveAt(i);
        }
    }

    private void SpawnSlot(GameObject prefab, SpellContext ctx, int count)
    {
        var instances = new List<GameObject>(count);
        for (int i = 0; i < count; i++)
        {
            float baseAngle = 360f / count * i;
            var go = Instantiate(prefab, transform.position, Quaternion.identity);

            if (ctx.OverrideMaterial != null)
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                    r.material = ctx.OverrideMaterial;

            go.AddComponent<OrbitalProjectile>().Initialize(ctx, baseAngle);
            instances.Add(go);
        }

        _slots.Add(new OrbitalSlot { Prefab = prefab, Ctx = ctx, Instances = instances });
    }

    private void DestroySlot(int idx)
    {
        foreach (var go in _slots[idx].Instances)
            if (go != null) Destroy(go);
    }

    private int FindSlot(GameObject prefab)
    {
        for (int i = 0; i < _slots.Count; i++)
            if (_slots[i].Prefab == prefab) return i;
        return -1;
    }

    private void OnDestroy()
    {
        foreach (var slot in _slots)
            DestroySlot(_slots.IndexOf(slot));
    }
}
