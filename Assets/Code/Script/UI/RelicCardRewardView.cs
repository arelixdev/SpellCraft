using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Carte de choix affichée par RelicRewardPanel. Version relique de NodeCardRewardView,
/// sans les champs spécifiques aux nodes (coût, synergies, corruption).
public class RelicCardRewardView : MonoBehaviour
{
    [SerializeField] private Image    _icon;
    [SerializeField] private TMP_Text _nameLabel;
    [SerializeField] private Image    _rarityBadge;
    [SerializeField] private TMP_Text _rarityLabel;
    [SerializeField] private TMP_Text _descriptionLabel;
    [SerializeField] private Button   _button;

    private RelicSO _data;

    public void Init(RelicSO data, Action<RelicSO> onChosen)
    {
        _data = data;

        if (_icon != null)
        {
            _icon.enabled = data.Icon != null;
            _icon.sprite  = data.Icon;
        }

        if (_nameLabel != null) _nameLabel.text = data.DisplayName;

        if (_rarityBadge != null) _rarityBadge.color = NodeView.ColorForRarity(data.Rarity);
        if (_rarityLabel != null) _rarityLabel.text = data.Rarity.ToString();

        if (_descriptionLabel != null) _descriptionLabel.text = data.Description;

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onChosen(data));
        }
    }
}
