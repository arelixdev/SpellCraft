using UnityEngine;

/// Draws one bezier line per slot from its sidebar port to the slot's entry node,
/// only when that slot has a SlotEntry in the shared crafting graph.
public class LauncherConnectionLine : MonoBehaviour
{
    public SlotSidebarController  Sidebar;
    public GraphCanvasController  Canvas;
    public SpellCraftingPanel     Panel;

    private UIBezierLine[] _lines;
    private RectTransform  _rt;

    private void Awake() => _rt = GetComponent<RectTransform>();

    private void Update()
    {
        EnsureLines();
        if (_lines == null) return;

        var graph = Panel != null ? Panel.WorkingGraph : null;

        for (int i = 0; i < _lines.Length; i++)
        {
            if (_lines[i] == null) continue;

            if (graph == null || !graph.TryGetSlotEntry(i, out int targetNode))
            {
                _lines[i].SetPoints(Vector2.zero, Vector2.zero);
                continue;
            }

            var fromRT = Sidebar != null ? Sidebar.GetPortRT(i) : null;
            var toRT   = Canvas  != null ? Canvas.GetNodeInputPortRT(targetNode) : null;

            if (fromRT == null || toRT == null)
            {
                _lines[i].SetPoints(Vector2.zero, Vector2.zero);
                continue;
            }

            _lines[i].SetPoints(RightCenter(fromRT), LeftCenter(toRT));
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void EnsureLines()
    {
        int needed = Sidebar != null ? Sidebar.SlotCount : 0;
        if (_lines != null && _lines.Length == needed) return;

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        _lines = new UIBezierLine[needed];
        for (int i = 0; i < needed; i++)
        {
            var go = new GameObject($"SlotLine_{i}", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _lines[i] = go.AddComponent<UIBezierLine>();
        }
    }

    private Vector2 RightCenter(RectTransform rt)
    {
        var c = Corners(rt);
        return ToLocal((c[2] + c[3]) * 0.5f);
    }

    private Vector2 LeftCenter(RectTransform rt)
    {
        var c = Corners(rt);
        return ToLocal((c[0] + c[1]) * 0.5f);
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
