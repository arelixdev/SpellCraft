using System.Collections.Generic;
using UnityEngine;

public class SpellZoneManager : MonoBehaviour
{
    private struct ZoneSlot
    {
        public GameObject Prefab;
        public GameObject Instance;
    }

    private readonly List<ZoneSlot> _slots = new();

    public void Register(GameObject prefab, SpellContext ctx)
    {
        int existing = FindSlot(prefab);

        if (existing >= 0)
        {
            var slot = _slots[existing];
            if (slot.Instance != null)
            {
                slot.Instance.GetComponent<ZoneEffect>()?.UpdateContext(ctx);
                return;
            }
            _slots.RemoveAt(existing);
        }

        SpawnSlot(prefab, ctx);
    }

    private void SpawnSlot(GameObject prefab, SpellContext ctx)
    {
        var go = Instantiate(prefab, transform.position, Quaternion.identity);

        if (ctx.OverrideMaterial != null)
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                r.material = ctx.OverrideMaterial;

        go.AddComponent<ZoneEffect>().Initialize(ctx);
        _slots.Add(new ZoneSlot { Prefab = prefab, Instance = go });
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
            if (slot.Instance != null) Destroy(slot.Instance);
    }
}
