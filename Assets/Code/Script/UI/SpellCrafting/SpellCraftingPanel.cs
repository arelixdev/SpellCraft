using UnityEngine;

/// Top-level coordinator for the spell crafting UI.
/// Opens with a working copy of the graph; writes back to the real slot on Apply.
public class SpellCraftingPanel : MonoBehaviour
{
    [Header("References")]
    public GraphCanvasController  CanvasController;
    public NodeInventoryPanel     InventoryPanel;
    public SlotSidebarController  SlotSidebar;
    public SpellCaster            TargetCaster;

    private int          _activeSlot;
    private SpellGraphSO _workingCopy;

    public SpellGraphSO WorkingGraph => _workingCopy;

    public void OnOpen()
    {
        InventoryPanel.Populate();
        SlotSidebar.Init(this, TargetCaster);
        LoadSlot(0);
    }

    public void OnClose()
    {
        if (_workingCopy != null) Destroy(_workingCopy);
        CanvasController.ClearGraph();
    }

    public void OnApply()
    {
        if (_workingCopy == null || TargetCaster == null) return;
        CanvasController.FlushToGraph();

        var target = TargetCaster.GetSlot(_activeSlot)?.connectedSpell;
        if (target == null)
        {
            target = ScriptableObject.CreateInstance<SpellGraphSO>();
            TargetCaster.SetSlotGraph(_activeSlot, target);
        }

        CopyGraph(_workingCopy, target);
        Debug.Log($"[SpellCrafting] Applied graph to slot {_activeSlot}");
    }

    public void OnSlotChanged(int slotIndex)
    {
        if (_workingCopy != null) CanvasController.FlushToGraph();
        LoadSlot(slotIndex);
    }

    public void RefreshBudget()
    {
        if (_workingCopy == null || SlotSidebar == null) return;
        SlotSidebar.RefreshBudget(_workingCopy.TotalCost, _workingCopy.complexityBudget);
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void LoadSlot(int index)
    {
        _activeSlot = index;
        if (_workingCopy != null) Destroy(_workingCopy);

        var source = TargetCaster != null ? TargetCaster.GetSlot(index)?.connectedSpell : null;
        _workingCopy = source != null
            ? ScriptableObject.Instantiate(source)
            : ScriptableObject.CreateInstance<SpellGraphSO>();

        CanvasController.LoadGraph(_workingCopy);
        RefreshBudget();
        SlotSidebar.RefreshPortColor();
    }

    private static void CopyGraph(SpellGraphSO src, SpellGraphSO dst)
    {
        dst.complexityBudget  = src.complexityBudget;
        dst.launcherConnected = src.launcherConnected;
        dst.nodes             = new(src.nodes);
        dst.connections       = new(src.connections);
        dst.editorLayout      = new(src.editorLayout);
    }
}
