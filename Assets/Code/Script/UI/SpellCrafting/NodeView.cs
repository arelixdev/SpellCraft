using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NodeView : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IPointerClickHandler
{
    public SpellNodeSO Data       { get; private set; }
    public int         NodeIndex  { get; set; }
    public PortView    InputPort  { get; private set; }
    public PortView    OutputPort { get; private set; }

    private RectTransform         _rt;
    private GraphCanvasController _canvas;
    private Vector2               _dragOffset;

    public static Color ColorForType(NodeType t) => t switch
    {
        NodeType.Emitter   => new Color(0.30f, 0.55f, 0.95f),
        NodeType.Element   => new Color(0.95f, 0.55f, 0.20f),
        NodeType.Behavior  => new Color(0.65f, 0.30f, 0.95f),
        NodeType.Condition => new Color(0.95f, 0.85f, 0.20f),
        NodeType.Trigger   => new Color(0.95f, 0.30f, 0.30f),
        NodeType.Effect    => new Color(0.30f, 0.90f, 0.55f),
        _                  => Color.gray,
    };

    public void Init(SpellNodeSO data, GraphCanvasController canvas)
    {
        Data    = data;
        _canvas = canvas;
        _rt     = GetComponent<RectTransform>();

        var bg = GetComponent<Image>();
        if (bg) bg.color = ColorForType(data.nodeType);

        var labels = GetComponentsInChildren<Text>();
        if (labels.Length > 0) labels[0].text = data.nodeName;
        if (labels.Length > 1) labels[1].text = $"[{data.complexityCost}]";

        InputPort  = transform.Find("InputPort") ?.GetComponent<PortView>();
        OutputPort = transform.Find("OutputPort")?.GetComponent<PortView>();
        InputPort? .Init(this, PortView.PortType.Input,  new Color(0.3f, 0.75f, 1.0f));
        OutputPort?.Init(this, PortView.PortType.Output, new Color(1.0f, 0.60f, 0.2f));
    }

    // Absorbe OnPointerDown sur le corps du nœud pour empêcher le fond
    // de GraphArea de recevoir l'event (évite une annulation involontaire).
    public void OnPointerDown(PointerEventData e) { }

    public void OnBeginDrag(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_rt.parent, e.position, e.pressEventCamera, out var local);
        _dragOffset = (Vector2)_rt.localPosition - local;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_rt.parent, e.position, e.pressEventCamera, out var local);
        _rt.localPosition = local + _dragOffset;
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Right)
            _canvas.DeleteNode(this);
    }

    public Vector2 GetLocalPosition()      => _rt.localPosition;
    public void    SetLocalPosition(Vector2 p) => _rt.localPosition = p;

    public void SetAlpha(float a)
    {
        var cg = GetComponent<CanvasGroup>();
        if (cg) cg.alpha = a;
    }
}
