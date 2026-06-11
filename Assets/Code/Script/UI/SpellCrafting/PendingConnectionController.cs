using UnityEngine;
using UnityEngine.InputSystem;

public class PendingConnectionController : MonoBehaviour
{
    public static PendingConnectionController Instance { get; private set; }

    public PortView SourcePort { get; private set; }
    public bool     IsActive   => SourcePort != null;

    private UIBezierLine  _line;
    private RectTransform _graphAreaRect;
    private bool          _skipNextRelease; // ignore le release qui suit le press de démarrage

    private void Awake()
    {
        Instance       = this;
        _line          = GetComponent<UIBezierLine>();
        _graphAreaRect = transform.parent.GetComponent<RectTransform>();
        gameObject.SetActive(false);
    }

    public void StartFrom(PortView port)
    {
        SourcePort       = port;
        _skipNextRelease = true;
        gameObject.SetActive(true);          // Awake s'exécute ici si c'est la première activation
        _line.SetPoints(Vector2.zero, Vector2.zero); // _line est initialisé après SetActive
        Canvas.willRenderCanvases += UpdateLine;
    }

    public void Cancel()
    {
        if (!IsActive) return;
        Canvas.willRenderCanvases -= UpdateLine;
        _line.SetPoints(Vector2.zero, Vector2.zero);
        SourcePort = null;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsActive) return;

        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
        { Cancel(); return; }

        if (Mouse.current?.leftButton.wasReleasedThisFrame == true)
        {
            if (_skipNextRelease) { _skipNextRelease = false; return; }
            Cancel();
        }
        else
        {
            // Reset le flag dès que le bouton n'est plus pressé sur cette frame
            // (cas où press + release arrivent sur deux frames distinctes)
            if (!Mouse.current.leftButton.isPressed)
                _skipNextRelease = false;
        }
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
