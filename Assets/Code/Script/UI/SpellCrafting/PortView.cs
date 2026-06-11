using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PortView : MonoBehaviour, IPointerClickHandler
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

    public void OnPointerClick(PointerEventData e)
    {
        if (Type == PortType.Output)
            GraphCanvasController.Instance.BeginConnection(this);
        else
            GraphCanvasController.Instance.CompleteConnection(this);
    }

    // In Screen Space Overlay, transform.position.xy == screen position in pixels
    public Vector2 ScreenPosition => transform.position;
}
