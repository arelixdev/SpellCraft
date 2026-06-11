using UnityEngine;
using UnityEngine.UI;

public class ConnectionView : MonoBehaviour
{
    public int FromNodeIndex { get; set; }
    public int ToNodeIndex   { get; set; }

    private UIBezierLine  _line;
    private PortView      _fromPort;
    private PortView      _toPort;
    private RectTransform _graphAreaRect;

    public void Init(PortView from, PortView to, RectTransform graphArea)
    {
        _fromPort      = from;
        _toPort        = to;
        _graphAreaRect = graphArea;
        FromNodeIndex  = from.OwnerNodeView.NodeIndex;
        ToNodeIndex    = to.OwnerNodeView.NodeIndex;

        _line = GetComponent<UIBezierLine>();
        Canvas.willRenderCanvases += UpdateLine;
    }

    private void OnDestroy() => Canvas.willRenderCanvases -= UpdateLine;

    private void UpdateLine()
    {
        if (_fromPort == null || _toPort == null || _graphAreaRect == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _graphAreaRect, _fromPort.ScreenPosition, null, out var s);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _graphAreaRect, _toPort.ScreenPosition,   null, out var e);
        _line.SetPoints(s, e);
    }

    public void UpdateIndices(int deletedIndex)
    {
        if (FromNodeIndex > deletedIndex) FromNodeIndex--;
        if (ToNodeIndex   > deletedIndex) ToNodeIndex--;
    }
}
