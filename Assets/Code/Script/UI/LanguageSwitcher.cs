using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// Sélecteur de langue jetable pour tester rapidement les traductions en jeu : construit sa
// propre rangée de boutons au runtime (un par locale disponible), pas de prefab à maintenir.
// À remplacer par un vrai écran d'options si la fonctionnalité doit devenir définitive.
public class LanguageSwitcher : MonoBehaviour
{
    [SerializeField] private Vector2 _anchoredPosition = new(-16f, -16f);
    [SerializeField] private Vector2 _buttonSize        = new(44f, 32f);

    private void Start()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var root = new GameObject("LanguageSwitcher_Runtime", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        root.transform.SetParent(canvas.transform, false);

        var localeCount = LocalizationSettings.AvailableLocales.Locales.Count;

        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot     = new Vector2(1f, 1f);
        rootRect.anchoredPosition = _anchoredPosition;
        rootRect.sizeDelta = new Vector2(localeCount * (_buttonSize.x + 6f), _buttonSize.y);

        var layout = root.GetComponent<HorizontalLayoutGroup>();
        layout.spacing            = 6f;
        layout.childControlWidth  = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth  = false;
        layout.childForceExpandHeight = false;

        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
            CreateButton(root.transform, locale);
    }

    private void CreateButton(Transform parent, Locale locale)
    {
        var go = new GameObject(locale.Identifier.Code, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = _buttonSize;

        var image = go.GetComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.24f, 0.9f);

        var button = go.GetComponent<Button>();
        button.onClick.AddListener(() => LocalizationSettings.SelectedLocale = locale);

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);

        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        var text = textGo.AddComponent<TextMeshProUGUI>();
        text.text      = locale.Identifier.Code.ToUpperInvariant();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize  = 16f;
        text.color     = Color.white;
    }
}
