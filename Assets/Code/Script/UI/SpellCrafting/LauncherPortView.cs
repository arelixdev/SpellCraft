using UnityEngine;
using UnityEngine.EventSystems;

/// Output port on a slot icon in the sidebar.
/// Clicking disconnects the current launcher link and starts a draggable cable.
public class LauncherPortView : MonoBehaviour, IPointerDownHandler
{
    public int                   SlotIndex { get; set; }
    public SlotSidebarController Sidebar   { get; set; }

    private SpellCraftingPanel Panel => Sidebar != null ? Sidebar.Panel : null;

    public Vector2 ScreenPosition => transform.position;

    public void OnPointerDown(PointerEventData e)
    {
        var pending = PendingConnectionController.Instance;

        if (pending != null && pending.IsActive)
        {
            pending.Cancel();
            return;
        }

        var panel = Panel;
        var graph = panel?.WorkingGraph;
        if (graph != null)
        {
            graph.RemoveSlotEntry(SlotIndex);
            Sidebar?.RefreshAllPortColors();
            panel?.AutoApply();
        }

        pending?.StartFromLauncher(this);
    }
}
