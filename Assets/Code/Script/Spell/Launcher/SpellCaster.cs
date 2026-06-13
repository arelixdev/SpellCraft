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

    // Called by NodePickup when the player walks over it
    public void CollectNode(SpellNodeSO node)
    {
        var canvas = GraphCanvasController.Instance;
        if (canvas != null && canvas.IsLoaded)
        {
            // Panel open — add through canvas so the node appears immediately and auto-applies
            canvas.AddNodeAtRandom(node);
            Debug.Log($"[SpellCaster] Collected '{node.nodeName}' — added to open graph");
            return;
        }

        // Panel closed — make sure base spell nodes are already in craftingGraph before appending
        EnsureCraftingGraphInitialized();

        // Count legacy nodes and slot rows so the loot node lands BELOW all slot rows
        int slotRowCount   = 0;
        int legacyNodeCount = 0;
        foreach (var s in _spellSlots)
        {
            if (s?.connectedSpell != null && s.connectedSpell.nodes.Count > 0)
            {
                slotRowCount++;
                legacyNodeCount += s.connectedSpell.nodes.Count;
            }
        }

        int newIdx  = craftingGraph.nodes.Count;
        int lootCol = newIdx - legacyNodeCount; // 0 for first loot node, 1 for second, etc.
        craftingGraph.nodes.Add(node);
        craftingGraph.editorLayout.Add(new SpellGraphSO.NodePlacement
        {
            nodeIndex      = newIdx,
            canvasPosition = new Vector2(-200f + lootCol * 180f, 80f - slotRowCount * 150f)
        });

        Debug.Log($"[SpellCaster] Collected '{node.nodeName}' — stored at loot row {slotRowCount}, will appear when panel opens");
    }

    // Merges legacy per-slot spells into craftingGraph (only if not already done).
    // Mirrors SpellCraftingPanel.PopulateFromSlots but runs without the panel being open.
    private void EnsureCraftingGraphInitialized()
    {
        if (craftingGraph != null && craftingGraph.nodes.Count > 0) return;

        if (craftingGraph == null)
            craftingGraph = ScriptableObject.CreateInstance<SpellGraphSO>();

        int nodeOffset = 0;
        for (int i = 0; i < _spellSlots.Length; i++)
        {
            var source = _spellSlots[i]?.connectedSpell;
            if (source == null || source.nodes.Count == 0) continue;

            craftingGraph.SetSlotEntry(i, nodeOffset);

            for (int j = 0; j < source.nodes.Count; j++)
            {
                craftingGraph.nodes.Add(source.nodes[j]);
                craftingGraph.editorLayout.Add(new SpellGraphSO.NodePlacement
                {
                    nodeIndex      = nodeOffset + j,
                    canvasPosition = new Vector2(-200f + j * 180f, 80f - i * 150f)
                });
            }

            foreach (var conn in source.connections)
                craftingGraph.connections.Add(new SpellGraphSO.Connection
                {
                    fromIndex = conn.fromIndex + nodeOffset,
                    toIndex   = conn.toIndex   + nodeOffset
                });

            nodeOffset += source.nodes.Count;
        }
    }

    public SpellSlot   GetSlot(int i)  => (i >= 0 && i < _spellSlots.Length) ? _spellSlots[i] : null;
    public SpellSlot[] GetSlots()      => _spellSlots;
    public void SetSlotGraph(int i, SpellGraphSO graph)
    {
        if (i >= 0 && i < _spellSlots.Length) _spellSlots[i].connectedSpell = graph;
    }
}
