using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class SpellCraftingToggle : MonoBehaviour
{
    public static SpellCraftingToggle Instance { get; private set; }

    [SerializeField] private RectTransform      _panelRT;
    [SerializeField] private SpellCraftingPanel _panel;
    [SerializeField] private Camera             _gameCamera;
    [SerializeField] private float              _slideDuration = 0.2f;

    [Tooltip("Root RectTransform for HUD elements (health, energy, gold, Game Over) that must stay centered within the visible game viewport rather than the full screen. Its anchorMax.x is kept in sync with the game camera's viewport width.")]
    [SerializeField] private RectTransform      _hudRoot;

    public bool IsOpen { get; private set; }

    private float     _panelWidth;
    private Coroutine _slideCoroutine;

    private void Awake()
    {
        Instance    = this;
        _panelWidth = _panelRT ? _panelRT.rect.width : 860f;
        if (_panelRT != null)
            _panelRT.anchoredPosition = new Vector2(_panelWidth, 0f);
    }

    private void Update()
    {
        if (Keyboard.current?.tabKey.wasPressedThisFrame == true)
            Toggle();
    }

    public void Toggle() => SetOpen(!IsOpen);

    public void SetOpen(bool open)
    {
        IsOpen = open;
        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
        _slideCoroutine = StartCoroutine(SlidePanel(open ? 0f : _panelWidth, open ? 0.6f : 1f));
    }

    private IEnumerator SlidePanel(float targetX, float targetViewportW)
    {
        if (IsOpen)  _panel?.OnOpen();

        float startX  = _panelRT.anchoredPosition.x;
        float elapsed = 0f;

        Rect startViewport = _gameCamera ? _gameCamera.rect : new Rect(0, 0, 1, 1);

        while (elapsed < _slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _slideDuration));
            _panelRT.anchoredPosition = new Vector2(Mathf.Lerp(startX, targetX, t), 0f);

            float viewportW = Mathf.Lerp(startViewport.width, targetViewportW, t);
            if (_gameCamera != null)
                _gameCamera.rect = new Rect(startViewport.x, startViewport.y, viewportW, startViewport.height);
            if (_hudRoot != null)
                _hudRoot.anchorMax = new Vector2(viewportW, 1f);

            yield return null;
        }

        _panelRT.anchoredPosition = new Vector2(targetX, 0f);
        if (_gameCamera != null)
            _gameCamera.rect = new Rect(startViewport.x, startViewport.y, targetViewportW, startViewport.height);
        if (_hudRoot != null)
            _hudRoot.anchorMax = new Vector2(targetViewportW, 1f);

        if (!IsOpen) _panel?.OnClose();
    }
}
