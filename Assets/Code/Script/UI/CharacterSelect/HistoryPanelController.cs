using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// Panneau latéral (slide droite→gauche depuis le bouton "Historique") qui accueillera plus
// tard la liste des robots choisis en début de run — pour l'instant seuls l'ouverture/
// fermeture et le blocage des clics sous le panneau sont branchés, Content reste vide.
public class HistoryPanelController : MonoBehaviour
{
    [SerializeField] private RectTransform _panel;
    [SerializeField] private GameObject    _blocker;
    [SerializeField] private Button        _openButton;
    [SerializeField] private Button        _closeButton;
    [SerializeField] private TMP_Text      _titleLabel;
    [SerializeField] private float         _animDuration = 0.3f;

    private const string TitleTableName = "Menu Labels";
    private const string TitleKey       = "characterSelect.historic";

    private float _shownX;
    private float _hiddenX;
    private Tween _tween;

    private void Awake()
    {
        _shownX  = _panel.anchoredPosition.x;
        _hiddenX = _shownX + _panel.rect.width;
        _panel.anchoredPosition = new Vector2(_hiddenX, _panel.anchoredPosition.y);

        if (_blocker != null) _blocker.SetActive(false);
        if (_openButton != null) _openButton.onClick.AddListener(Open);
        if (_closeButton != null) _closeButton.onClick.AddListener(Close);

        RefreshTitle();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale _) => RefreshTitle();

    private void RefreshTitle()
    {
        if (_titleLabel != null)
            _titleLabel.text = LocalizationSettings.StringDatabase.GetLocalizedString(TitleTableName, TitleKey);
    }

    public void Open()
    {
        if (_blocker != null) _blocker.SetActive(true);

        _tween?.Kill();
        _tween = _panel.DOAnchorPosX(_shownX, _animDuration).SetEase(Ease.OutCubic);
    }

    public void Close()
    {
        _tween?.Kill();
        _tween = _panel.DOAnchorPosX(_hiddenX, _animDuration).SetEase(Ease.InCubic)
            .OnComplete(() =>
            {
                if (_blocker != null) _blocker.SetActive(false);
            });
    }
}
