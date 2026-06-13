using UnityEngine;

/// Top-level coordinator for the spell crafting UI.
/// Opens with a working copy of the graph; writes back to the real slot on Apply.
public class SpellCraftingPanel : MonoBehaviour
{
    [Header("References")]
    public GraphCanvasController  CanvasController;
    public SlotSidebarController  SlotSidebar;
    public SpellCaster            TargetCaster;

    private int          _activeSlot;
    private SpellGraphSO _workingCopy;

    public SpellGraphSO WorkingGraph => _workingCopy;

    public void OnOpen()
    {
        SlotSidebar?.Init(this);
        LoadSlot(0);
    }

    public void OnClose()
    {
        if (_workingCopy != null) Destroy(_workingCopy);
        CanvasController?.ClearGraph();
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
        if (_workingCopy != null) CanvasController?.FlushToGraph();
        LoadSlot(slotIndex);
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void LoadSlot(int index)
    {
        _activeSlot = index;
        if (_workingCopy != null) Destroy(_workingCopy);

        var slot   = TargetCaster != null ? TargetCaster.GetSlot(index) : null;
        var source = slot?.connectedSpell;

        _workingCopy = source != null
            ? ScriptableObject.Instantiate(source)
            : ScriptableObject.CreateInstance<SpellGraphSO>();

        if (slot?.launcherConfig != null && source != null)
            _workingCopy.launcherConnected = true;

        CanvasController?.LoadGraph(_workingCopy);
        SlotSidebar?.RefreshPortColor();
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
