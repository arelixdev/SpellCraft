using UnityEngine;

/// Top-level coordinator for the spell crafting UI.
/// All nodes live in ONE shared SpellGraphSO (SpellCaster.craftingGraph).
/// Slots are entry-points into that graph — any slot can connect to any node.
public class SpellCraftingPanel : MonoBehaviour
{
    [Header("References")]
    public GraphCanvasController  CanvasController;
    public SlotSidebarController  SlotSidebar;
    public SpellCaster            TargetCaster;

    private SpellGraphSO _workingGraph;

    public SpellGraphSO WorkingGraph => _workingGraph;

    public void OnOpen()
    {
        var cg = TargetCaster != null ? TargetCaster.craftingGraph : null;

        if (cg != null && cg.nodes.Count > 0)
        {
            _workingGraph = Object.Instantiate(cg);
        }
        else
        {
            _workingGraph = ScriptableObject.CreateInstance<SpellGraphSO>();
            PopulateFromSlots(_workingGraph);
        }

        if (SlotSidebar != null) SlotSidebar.Init(this);
        if (CanvasController != null)
        {
            CanvasController.LoadGraph(_workingGraph);
            CanvasController.OnGraphModified += AutoApply;
        }
    }

    public void OnClose()
    {
        if (CanvasController != null)
            CanvasController.OnGraphModified -= AutoApply;

        if (CanvasController != null) CanvasController.ClearGraph();
        if (_workingGraph != null) Object.Destroy(_workingGraph);
        _workingGraph = null;
    }

    public void AutoApply()
    {
        if (_workingGraph == null || TargetCaster == null) return;

        CanvasController.FlushToGraph(_workingGraph);

        if (TargetCaster.craftingGraph == null)
            TargetCaster.craftingGraph = ScriptableObject.CreateInstance<SpellGraphSO>();

        CopyGraph(_workingGraph, TargetCaster.craftingGraph);

        if (SlotSidebar != null) SlotSidebar.RefreshAllPortColors();
    }

    // Called by sidebar / bottom-bar to track active slot (canvas no longer uses it)
    public void OnSlotChanged(int slotIndex) { }

    // ── Private ──────────────────────────────────────────────────────────────

    // Merge per-slot connectedSpell assets into a single shared graph.
    // Used only the first time (when craftingGraph is null/empty).
    private void PopulateFromSlots(SpellGraphSO graph)
    {
        if (TargetCaster == null) return;
        var slots = TargetCaster.GetSlots();
        int nodeOffset = 0;

        for (int i = 0; i < slots.Length; i++)
        {
            var source = slots[i]?.connectedSpell;
            if (source == null || source.nodes.Count == 0) continue;

            graph.SetSlotEntry(i, nodeOffset);

            for (int j = 0; j < source.nodes.Count; j++)
            {
                graph.nodes.Add(source.nodes[j]);
                graph.editorLayout.Add(new SpellGraphSO.NodePlacement
                {
                    nodeIndex      = nodeOffset + j,
                    canvasPosition = RowPosition(i, j)
                });
            }

            foreach (var conn in source.connections)
                graph.connections.Add(new SpellGraphSO.Connection
                {
                    fromIndex = conn.fromIndex + nodeOffset,
                    toIndex   = conn.toIndex   + nodeOffset
                });

            nodeOffset += source.nodes.Count;
        }
    }

    private static Vector2 RowPosition(int slotIndex, int nodeIndex) =>
        new Vector2(-200f + nodeIndex * 180f, 80f - slotIndex * 150f);

    private static void CopyGraph(SpellGraphSO src, SpellGraphSO dst)
    {
        dst.complexityBudget = src.complexityBudget;
        dst.slotEntries      = new(src.slotEntries);
        dst.nodes            = new(src.nodes);
        dst.connections      = new(src.connections);
        dst.editorLayout     = new(src.editorLayout);
    }
}
