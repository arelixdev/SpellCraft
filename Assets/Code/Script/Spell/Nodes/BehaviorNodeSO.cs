using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Spell/Nodes/Behavior")]
public class BehaviorNodeSO : SpellNodeSO
{
    [Title("Behavior Type")]
    [EnumToggleButtons, HideLabel]
    public BehaviorType behaviorType;

    [BoxGroup("Parameters")]
    [ShowIf("behaviorType", BehaviorType.Bounce)]
    [LabelWidth(110), MinValue(1)] public int bounceCount = 3;

    [BoxGroup("Parameters")]
    [ShowIf("behaviorType", BehaviorType.Split)]
    [LabelWidth(110), MinValue(2)] public int splitCount = 2;

    public override void Execute(SpellContext ctx) => ctx.Behaviors.Add(behaviorType);
}
