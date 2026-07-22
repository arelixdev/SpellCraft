using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Panneau Options réutilisable (prefab instancié dans MainMenu et Gameplay, chacun avec son
// propre bouton déclencheur) : ouverture/fermeture (scale) + blocage des clics sous le panneau
// pendant qu'il est ouvert, même schéma que HistoryPanelController côté CharacterSelect.
// Content reste vide pour l'instant — prêt à accueillir de vrais réglages plus tard.
public class OptionsPanelController : MonoBehaviour
{
    [SerializeField] private RectTransform _panel;
    [SerializeField] private GameObject    _blocker;
    [SerializeField] private Button        _closeButton;
    [SerializeField] private float         _animDuration = 0.25f;

    private Tween _tween;

    private void Awake()
    {
        _panel.localScale = Vector3.zero;
        if (_blocker != null) _blocker.SetActive(false);
        if (_closeButton != null) _closeButton.onClick.AddListener(Close);
    }

    public void Open()
    {
        if (_blocker != null) _blocker.SetActive(true);

        _tween?.Kill();
        _tween = _panel.DOScale(1f, _animDuration).SetEase(Ease.OutBack);
    }

    public void Close()
    {
        _tween?.Kill();
        _tween = _panel.DOScale(0f, _animDuration).SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                if (_blocker != null) _blocker.SetActive(false);
            });
    }
}
