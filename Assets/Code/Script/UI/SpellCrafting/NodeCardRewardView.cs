using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Carte de choix affichée par NodeRewardPanel. Version enrichie de NodeCardView
/// (rareté, description, synergies, corruption) avec des références sérialisées
/// explicites plutôt qu'une indexation positionnelle, pour rester facilement personnalisable.
public class NodeCardRewardView : MonoBehaviour
{
    [SerializeField] private Image    _background;
    [SerializeField] private Image    _icon;
    [SerializeField] private TMP_Text _nameLabel;
    [SerializeField] private Image    _rarityBadge;
    [SerializeField] private TMP_Text _rarityLabel;
    [SerializeField] private TMP_Text _costLabel;
    [SerializeField] private TMP_Text _descriptionLabel;
    [SerializeField] private TMP_Text _synergiesLabel;
    [SerializeField] private GameObject _corruptedBadgeRoot;
    [SerializeField] private TMP_Text _corruptedNameLabel;
    [SerializeField] private TMP_Text _corruptedDescriptionLabel;
    [SerializeField] private Button   _button;

    private SpellNodeSO _data;

    public void Init(SpellNodeSO data, Action<SpellNodeSO> onChosen)
    {
        _data = data;

        if (_background != null) _background.color = NodeView.ColorForType(data.nodeType) * 0.7f;

        if (_icon != null)
        {
            _icon.enabled = data.icon != null;
            _icon.sprite  = data.icon;
        }

        if (_nameLabel != null) _nameLabel.text = data.nodeName;

        if (_rarityBadge != null) _rarityBadge.color = NodeView.ColorForRarity(data.rarity);
        if (_rarityLabel != null) _rarityLabel.text = data.rarity.ToString();

        if (_costLabel != null) _costLabel.text = $"Coût : {data.complexityCost}";

        if (_descriptionLabel != null) _descriptionLabel.text = data.description;

        if (_synergiesLabel != null)
        {
            bool hasSynergies = data.synergies != null && data.synergies.Count > 0;
            _synergiesLabel.gameObject.SetActive(hasSynergies);
            if (hasSynergies)
                _synergiesLabel.text = "Synergie : " + string.Join(", ", data.synergies.Where(s => s != null).Select(s => s.nodeName));
        }

        if (_corruptedBadgeRoot != null)
        {
            bool corrupted = data.corruptedEffect != null;
            _corruptedBadgeRoot.SetActive(corrupted);
            if (corrupted)
            {
                if (_corruptedNameLabel != null) _corruptedNameLabel.text = data.corruptedEffect.effectName;
                if (_corruptedDescriptionLabel != null) _corruptedDescriptionLabel.text = data.corruptedEffect.description;
            }
        }

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onChosen(data));
        }
    }
}
