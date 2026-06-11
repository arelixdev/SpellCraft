using UnityEngine;

/// Top-level coordinator for the spell crafting UI.
/// Opens with a working copy of the graph; writes back to the real slot on Apply.
public class SpellCraftingPanel : MonoBehaviour
{
    [Header("References")]
    public GraphCanvasController CanvasController;
    public NodeInventoryPanel    InventoryPanel;
    public BottomBarController   BottomBar;
    public SpellCaster           TargetCaster;

    private int          _activeSlot;
    private SpellGraphSO _workingCopy;

    public void OnOpen()
    {
        InventoryPanel?.Populate();
        BottomBar?.Init(this, TargetCaster ? TargetCaster.GetSlots().Length : 0);
        LoadSlot(0);
    }

    public void OnClose()
    {
        // Discard unsaved changes
        if (_workingCopy != null) Destroy(_workingCopy);
        CanvasController?.ClearGraph();
    }

    public void OnApply()
    {
        if (_workingCopy == null || TargetCaster == null) return;
        CanvasController.FlushToGraph();

        // Write the working copy data into a fresh persisted asset (or reuse slot's graph)
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
        // Flush current canvas back to the working copy before switching
        if (_workingCopy != null) CanvasController?.FlushToGraph();
        LoadSlot(slotIndex);
    }

    public void RefreshBudget()
    {
        if (_workingCopy == null || BottomBar == null)
        {
            Debug.LogWarning($"[SpellCrafting] RefreshBudget skip — workingCopy={_workingCopy != null}, BottomBar={BottomBar != null}");
            return;
        }
        BottomBar.RefreshBudget(_workingCopy.TotalCost, _workingCopy.complexityBudget);
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private void LoadSlot(int index)
    {
        _activeSlot = index;
        if (_workingCopy != null) Destroy(_workingCopy);

        var source = TargetCaster?.GetSlot(index)?.connectedSpell;
        _workingCopy = source != null
            ? ScriptableObject.Instantiate(source)
            : ScriptableObject.CreateInstance<SpellGraphSO>();

        CanvasController?.LoadGraph(_workingCopy);
        RefreshBudget();
    }

    private static void CopyGraph(SpellGraphSO src, SpellGraphSO dst)
    {
        dst.complexityBudget = src.complexityBudget;
        dst.nodes            = new(src.nodes);
        dst.connections      = new(src.connections);
        dst.editorLayout     = new(src.editorLayout);
    }
}
