using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Spell/Nodes/Element")]
public class ElementNodeSO : SpellNodeSO
{
    [Title("Element Type")]
    [EnumToggleButtons, HideLabel]
    public ElementType element;

    [BoxGroup("Stats")]
    [HorizontalGroup("Stats/Row")]
    [LabelWidth(120), Range(0.1f, 5f)] public float damageMultiplier = 1f;

    [HorizontalGroup("Stats/Row")]
    [LabelWidth(90), Range(0f, 1f)] public float statusChance = 0.3f;

    [BoxGroup("Stats")]
    [LabelWidth(120), MinValue(0)] public float statusDuration = 2f;

    public override void Execute(SpellContext ctx)
    {
        ctx.Element  = element;
        ctx.Damage  *= damageMultiplier;
    }
}
