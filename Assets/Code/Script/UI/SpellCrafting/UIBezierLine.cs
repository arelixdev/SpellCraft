using UnityEngine;
using UnityEngine.UI;

/// Draws a cubic bezier curve as a UI mesh.
/// The RectTransform must fill its parent (anchor 0,0→1,1, offsets 0)
/// so that vertex positions in local space match the parent's local space.
[RequireComponent(typeof(CanvasRenderer))]
public class UIBezierLine : MaskableGraphic
{
    [SerializeField] private float _lineWidth = 4f;
    [SerializeField] private int   _segments  = 24;

    private Vector2 _start;
    private Vector2 _end;

    private void Awake() => raycastTarget = false;

    public void SetPoints(Vector2 start, Vector2 end)
    {
        _start = start;
        _end   = end;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (_start == _end) return;

        var  pts  = SampleBezier(_start, _end, _segments);
        float h   = _lineWidth * 0.5f;

        for (int i = 0; i < pts.Length - 1; i++)
        {
            Vector2 a    = pts[i];
            Vector2 b    = pts[i + 1];
            Vector2 dir  = (b - a).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x) * h;

            var v = new UIVertex { color = color };

            v.position = a + perp; vh.AddVert(v);
            v.position = a - perp; vh.AddVert(v);
            v.position = b + perp; vh.AddVert(v);
            v.position = b - perp; vh.AddVert(v);

            int o = i * 4;
            vh.AddTriangle(o,     o + 1, o + 2);
            vh.AddTriangle(o + 1, o + 3, o + 2);
        }
    }

    private static Vector2[] SampleBezier(Vector2 s, Vector2 e, int n)
    {
        float   cx = Mathf.Abs(e.x - s.x) * 0.5f + 50f;
        Vector2 c1 = s + new Vector2( cx, 0f);
        Vector2 c2 = e - new Vector2( cx, 0f);

        var pts = new Vector2[n + 1];
        for (int i = 0; i <= n; i++)
        {
            float t = i / (float)n, u = 1f - t;
            pts[i] = u*u*u*s + 3f*u*u*t*c1 + 3f*u*t*t*c2 + t*t*t*e;
        }
        return pts;
    }
}
