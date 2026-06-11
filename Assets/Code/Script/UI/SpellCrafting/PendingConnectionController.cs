using UnityEngine;
using UnityEngine.InputSystem;

/// Draws the "in-progress" bezier from an output port to the mouse cursor.
/// Registered to Canvas.willRenderCanvases so it renders while paused.
public class PendingConnectionController : MonoBehaviour
{
    public static PendingConnectionController Instance { get; private set; }

    public PortView SourcePort { get; private set; }
    public bool     IsActive   => SourcePort != null;

    private UIBezierLine  _line;
    private RectTransform _graphAreaRect;

    private void Awake()
    {
        Instance       = this;
        _line          = GetComponent<UIBezierLine>();
        _graphAreaRect = transform.parent.GetComponent<RectTransform>();
        gameObject.SetActive(false);
    }

    public void StartFrom(PortView port)
    {
        SourcePort = port;
        gameObject.SetActive(true);
        Canvas.willRenderCanvases += UpdateLine;
    }

    public void Cancel()
    {
        if (!IsActive) return;
        Canvas.willRenderCanvases -= UpdateLine;
        SourcePort = null;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsActive) return;
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            Cancel();
    }

    private void UpdateLine()
    {
        if (!IsActive || _graphAreaRect == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _graphAreaRect, SourcePort.ScreenPosition, null, out var s);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _graphAreaRect, Mouse.current.position.ReadValue(), null, out var e);
        _line.SetPoints(s, e);
    }
}
