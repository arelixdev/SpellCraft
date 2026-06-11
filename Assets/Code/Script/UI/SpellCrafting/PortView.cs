using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PortView : MonoBehaviour,
    IPointerDownHandler,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler
{
    public enum PortType { Input, Output }

    public NodeView OwnerNodeView { get; private set; }
    public PortType Type          { get; private set; }

    public void Init(NodeView owner, PortType type, Color color)
    {
        OwnerNodeView = owner;
        Type          = type;
        var img = GetComponent<Image>();
        if (img) img.color = color;
    }

    // OnPointerDown (pas OnPointerClick) : fire dès le press, pas besoin
    // que la souris soit immobile entre press et release.
    public void OnPointerDown(PointerEventData e)
    {
        var gca     = GraphCanvasController.Instance;
        var pending = PendingConnectionController.Instance;

        if (pending != null && pending.IsActive && Type == PortType.Input)
        {
            gca.CompleteConnection(this);
            return;
        }

        gca.GrabConnection(this);
    }

    // No-op : absorbe l'event pour qu'il ne remonte pas
    public void OnPointerClick(PointerEventData e) { }
    public void OnBeginDrag(PointerEventData e)    { }
    public void OnDrag(PointerEventData e)          { }

    public Vector2 ScreenPosition => transform.position;
}
