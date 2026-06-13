using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public class SpellCaster : MonoBehaviour
{
    [Title("Crafting Graph")]
    [Tooltip("Shared spell graph built in the crafting panel. Populated automatically on first open.")]
    public SpellGraphSO craftingGraph;

    [Title("Spell Loadout (initial defaults)")]
    [ListDrawerSettings(ShowIndexLabels = true, NumberOfItemsPerPage = 6)]
    [SerializeField] private SpellSlot[] _spellSlots;

    private float[] _cooldownTimers;

    private void Awake()
    {
        _spellSlots     ??= System.Array.Empty<SpellSlot>();
        _cooldownTimers   = new float[_spellSlots.Length];
    }

    private void Update()
    {
        for (int i = 0; i < _spellSlots.Length; i++)
        {
            if (!IsSlotReady(i)) continue;
            _cooldownTimers[i] -= Time.deltaTime;

            if (_spellSlots[i].launcherConfig.launcherType == LauncherType.AutoCast
                && _cooldownTimers[i] <= 0f)
                CastSlot(i);
        }
    }

    // Called by input events wired in the Inspector (e.g. via PlayerInput component)
    public void TryCastKeybind(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        for (int i = 0; i < _spellSlots.Length; i++)
        {
            var config = _spellSlots[i].launcherConfig;
            if (config?.launcherType != LauncherType.KeyBind) continue;
            if (config.inputAction?.action != context.action) continue;
            if (_cooldownTimers[i] > 0f) continue;
            CastSlot(i);
        }
    }

    // Called by other systems (kill feed, damage events, etc.)
    public void NotifyEvent(GameEventType eventType)
    {
        for (int i = 0; i < _spellSlots.Length; i++)
        {
            var config = _spellSlots[i].launcherConfig;
            if (config?.launcherType != LauncherType.OnEvent) continue;
            if (config.eventType != eventType) continue;
            if (_cooldownTimers[i] > 0f) continue;
            CastSlot(i);
        }
    }

    private void CastSlot(int i)
    {
        var slot = _spellSlots[i];
        _cooldownTimers[i] = slot.launcherConfig.cooldown;

        var ctx = new SpellContext
        {
            Caster    = gameObject,
            Origin    = transform.position,
            Direction = transform.forward,
        };
        ctx.Damage *= slot.launcherConfig.bonusMultiplier;

        if (craftingGraph != null && craftingGraph.TryGetSlotEntry(i, out int startNode))
        {
            if (slot.launcherConfig.launcherType == LauncherType.AutoCast)
                Debug.Log($"[SpellCaster] AutoCast slot {i} → {BuildChainString(craftingGraph, startNode)}");
            SpellExecutor.Execute(craftingGraph, startNode, ctx);
        }
        else if (slot.connectedSpell != null)
        {
            if (slot.launcherConfig.launcherType == LauncherType.AutoCast)
                Debug.Log($"[SpellCaster] AutoCast slot {i} (legacy) → {BuildChainString(slot.connectedSpell, 0)}");
            SpellExecutor.Execute(slot.connectedSpell, 0, ctx);
        }
    }

    private bool IsSlotReady(int i)
    {
        var slot = _spellSlots[i];
        if (slot?.launcherConfig == null) return false;
        if (craftingGraph != null && craftingGraph.nodes.Count > 0)
            return craftingGraph.HasSlotEntry(i);
        return slot.connectedSpell != null;
    }

    private static string BuildChainString(SpellGraphSO graph, int startIndex, int depth = 0)
    {
        if (graph == null || startIndex < 0 || startIndex >= graph.nodes.Count || depth > 10) return "…";
        var node = graph.nodes[startIndex];
        string name = node != null ? node.nodeName : "?";
        var outputs = graph.GetOutputIndices(startIndex);
        if (outputs.Count == 0) return name;
        return name + " → " + BuildChainString(graph, outputs[0], depth + 1);
    }

    public SpellSlot   GetSlot(int i)  => (i >= 0 && i < _spellSlots.Length) ? _spellSlots[i] : null;
    public SpellSlot[] GetSlots()      => _spellSlots;
    public void SetSlotGraph(int i, SpellGraphSO graph)
    {
        if (i >= 0 && i < _spellSlots.Length) _spellSlots[i].connectedSpell = graph;
    }
}
