using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;
    [SerializeField] private float _moveSpeed    = 2f;
    [SerializeField] private float _duration     = 1f;
    [SerializeField] private float _fadeInDuration = 0.15f;

    [Header("Effects")]
    public bool EnableFadeIn  = false;
    public bool EnableFadeOut = true;

    private float _elapsed;
    private Color _baseColor;
    private float _startFontSize;

    public void Setup(float damage, bool isCrit, ElementType element)
    {
        _label.text     = isCrit ? $"<b>{Mathf.RoundToInt(damage)}</b>" : Mathf.RoundToInt(damage).ToString();
        _label.color    = GetElementColor(element);
        _label.fontSize = isCrit ? _label.fontSize * 1.4f : _label.fontSize;
        _baseColor      = _label.color;
        _startFontSize  = _label.fontSize;

        if (EnableFadeIn)
            _label.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0f);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        transform.position += Vector3.up * _moveSpeed * Time.deltaTime;

        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;

        float alpha = _baseColor.a;

        if (EnableFadeIn && _elapsed < _fadeInDuration)
        {
            alpha = _elapsed / _fadeInDuration;
        }
        else if (EnableFadeOut)
        {
            float fadeStart = _duration * 0.5f;
            if (_elapsed > fadeStart)
                alpha = Mathf.Lerp(1f, 0f, (_elapsed - fadeStart) / (_duration - fadeStart));
        }

        _label.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);

        if (_elapsed >= _duration)
            Destroy(gameObject);
    }

    private static Color GetElementColor(ElementType element) => element switch
    {
        ElementType.Fire      => new Color(1f,   0.4f, 0.1f),
        ElementType.Ice       => new Color(0.4f, 0.8f, 1f),
        ElementType.Lightning => new Color(1f,   0.9f, 0.2f),
        ElementType.Arcane    => new Color(0.8f, 0.3f, 1f),
        ElementType.Poison    => new Color(0.3f, 0.9f, 0.2f),
        _                     => Color.white,
    };
}
