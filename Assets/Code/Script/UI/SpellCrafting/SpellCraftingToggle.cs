using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class SpellCraftingToggle : MonoBehaviour
{
    public static SpellCraftingToggle Instance { get; private set; }

    [SerializeField] private RectTransform _panelRT;
    [SerializeField] private Camera        _gameCamera;
    [SerializeField] private float         _slideDuration = 0.2f;

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
        _slideCoroutine = StartCoroutine(SlidePanel(open ? 0f : _panelWidth, open ? 0.5f : 1f));
    }

    private IEnumerator SlidePanel(float targetX, float targetViewportW)
    {
        float startX  = _panelRT.anchoredPosition.x;
        float elapsed = 0f;

        Rect startViewport = _gameCamera ? _gameCamera.rect : new Rect(0, 0, 1, 1);

        while (elapsed < _slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / _slideDuration));
            _panelRT.anchoredPosition = new Vector2(Mathf.Lerp(startX, targetX, t), 0f);
            if (_gameCamera != null)
                _gameCamera.rect = new Rect(startViewport.x, startViewport.y,
                                            Mathf.Lerp(startViewport.width, targetViewportW, t),
                                            startViewport.height);
            yield return null;
        }

        _panelRT.anchoredPosition = new Vector2(targetX, 0f);
        if (_gameCamera != null)
            _gameCamera.rect = new Rect(startViewport.x, startViewport.y, targetViewportW, startViewport.height);
    }
}
