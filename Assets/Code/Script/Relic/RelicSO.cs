using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Objet permanent ramassable en run : booste une ou plusieurs stats du joueur, ou lui
/// ajoute un nouveau sort de slotbar. Créer via : clic droit → SpellCraft/Relic
/// </summary>
[CreateAssetMenu(fileName = "NewRelic", menuName = "SpellCraft/Relic")]
public class RelicSO : ScriptableObject
{
    [LabelText("Nom affiché")]
    public string DisplayName = "Relique";

    [LabelText("Description"), TextArea]
    public string Description;

    [LabelText("Icône"), PreviewField(64)]
    public Sprite Icon;

    [LabelText("Rareté")]
    public NodeRarity Rarity = NodeRarity.Commun;

    [LabelText("Effets"), ListDrawerSettings(ShowIndexLabels = true)]
    [SerializeReference] public List<RelicEffect> Effects = new();
}
