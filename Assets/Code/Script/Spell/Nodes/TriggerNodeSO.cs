using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Spell/Nodes/Trigger")]
public class TriggerNodeSO : SpellNodeSO
{
    [Title("Trigger Type")]
    [EnumToggleButtons, HideLabel]
    public TriggerType triggerType;

    [BoxGroup("Parameters")]
    [ShowIf("triggerType", TriggerType.OnTick)]
    [LabelWidth(110), MinValue(0.1f)] public float tickInterval = 0.5f;

    // Registers a trigger callback on the projectile at runtime in SpellExecutor.
    public override void Execute(SpellContext ctx) { }
}
