using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Spell/Nodes/Effect")]
public class EffectNodeSO : SpellNodeSO
{
    [Title("Effect Type")]
    [EnumToggleButtons, HideLabel]
    public EffectType effectType;

    [BoxGroup("Parameters")]
    [HorizontalGroup("Parameters/Row")]
    [LabelWidth(60), MinValue(0)] public float radius = 3f;

    [HorizontalGroup("Parameters/Row")]
    [LabelWidth(65), MinValue(0)] public float duration = 1f;

    [BoxGroup("Parameters")]
    [LabelWidth(130), MinValue(0)] public float effectStrength = 10f;

    [BoxGroup("Parameters")]
    [Required, LabelWidth(130)]
    public GameObject effectPrefab;

    public override void Execute(SpellContext ctx)
    {
        ctx.Damage *= ctx.ConditionMultiplier;
        ctx.ActiveEffects.Add(this);
    }
}
