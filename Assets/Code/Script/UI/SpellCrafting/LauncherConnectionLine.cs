using UnityEngine;

/// Draws a live bezier from the active launcher slot's output port to node 0's input port,
/// but ONLY when WorkingGraph.launcherConnected is true.
/// Must be on a RectTransform that stretches over the full CraftingPanel.
[RequireComponent(typeof(UIBezierLine))]
public class LauncherConnectionLine : MonoBehaviour
{
    public SlotSidebarController  Sidebar;
    public GraphCanvasController  Canvas;
    public SpellCraftingPanel     Panel;

    private UIBezierLine  _line;
    private RectTransform _rt;

    private void Awake()
    {
        _line = GetComponent<UIBezierLine>();
        _rt   = GetComponent<RectTransform>();
    }

    private void Update()
    {
        bool connected = Panel != null
                      && Panel.WorkingGraph != null
                      && Panel.WorkingGraph.launcherConnected;

        if (!connected)
        {
            _line.SetPoints(Vector2.zero, Vector2.zero);
            return;
        }

        var fromRT = Sidebar != null ? Sidebar.GetActivePortRT() : null;
        var toRT   = Canvas  != null ? Canvas.GetNode0InputPortRT() : null;

        if (fromRT == null || toRT == null)
        {
            _line.SetPoints(Vector2.zero, Vector2.zero);
            return;
        }

        _line.SetPoints(RightCenter(fromRT), LeftCenter(toRT));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Vector2 RightCenter(RectTransform rt)
    {
        var c = Corners(rt);
        return ToLocal((c[2] + c[3]) * 0.5f); // top-right + bottom-right
    }

    private Vector2 LeftCenter(RectTransform rt)
    {
        var c = Corners(rt);
        return ToLocal((c[0] + c[1]) * 0.5f); // bottom-left + top-left
    }

    private static Vector3[] Corners(RectTransform rt)
    {
        var c = new Vector3[4];
        rt.GetWorldCorners(c);
        return c;
    }

    private Vector2 ToLocal(Vector3 worldPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rt, (Vector2)worldPos, null, out var local);
        return local;
    }
}
