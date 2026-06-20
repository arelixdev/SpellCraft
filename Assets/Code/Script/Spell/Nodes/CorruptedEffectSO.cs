using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Spell/CorruptedEffect")]
public class CorruptedEffectSO : ScriptableObject
{
    [LabelWidth(110)]
    public string effectName = "Effet corrompu";

    [TextArea(2, 4), HideLabel]
    [Title("Description")]
    public string description;

    [Title("Bonus")]
    [SerializeReference]
    [ListDrawerSettings(ShowFoldout = false, DraggableItems = true)]
    public List<CorruptionModifier> bonuses = new();

    [Title("Malus")]
    [SerializeReference]
    [ListDrawerSettings(ShowFoldout = false, DraggableItems = true)]
    public List<CorruptionModifier> maluses = new();

    public void ApplyTo(SpellContext ctx)
    {
        foreach (var bonus in bonuses) bonus?.Apply(ctx);
        foreach (var malus in maluses) malus?.Apply(ctx);
    }
}
