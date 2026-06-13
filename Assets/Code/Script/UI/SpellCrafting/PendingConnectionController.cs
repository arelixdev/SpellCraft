using UnityEngine;
using UnityEngine.InputSystem;

public class PendingConnectionController : MonoBehaviour
{
    public static PendingConnectionController Instance { get; private set; }

    public PortView         SourcePort     { get; private set; }
    public LauncherPortView LauncherSource { get; private set; }

    public bool IsActive         => SourcePort != null || LauncherSource != null;
    public bool IsLauncherActive => LauncherSource != null;

    private UIBezierLine  _line;
    private RectTransform _graphAreaRect;
    private bool          _skipNextRelease;

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
        LauncherSource   = null;
        _skipNextRelease = true;
        gameObject.SetActive(true);
        _line.SetPoints(Vector2.zero, Vector2.zero);
        Canvas.willRenderCanvases += UpdateLine;
    }

    public void StartFromLauncher(LauncherPortView port)
    {
        LauncherSource   = port;
        SourcePort       = null;
        _skipNextRelease = true;
        gameObject.SetActive(true);
        _line.SetPoints(Vector2.zero, Vector2.zero);
        Canvas.willRenderCanvases += UpdateLine;
    }

    public void Cancel()
    {
        if (!IsActive) return;
        Canvas.willRenderCanvases -= UpdateLine;
        _line.SetPoints(Vector2.zero, Vector2.zero);
        SourcePort     = null;
        LauncherSource = null;
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
            if (!Mouse.current.leftButton.isPressed)
                _skipNextRelease = false;
        }
    }

    private void UpdateLine()
    {
        if (!IsActive || _graphAreaRect == null) return;

        Vector2 screenPos = SourcePort != null
            ? SourcePort.ScreenPosition
            : LauncherSource.ScreenPosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _graphAreaRect, screenPos, null, out var s);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _graphAreaRect, Mouse.current.position.ReadValue(), null, out var e);
        _line.SetPoints(s, e);
    }
}
