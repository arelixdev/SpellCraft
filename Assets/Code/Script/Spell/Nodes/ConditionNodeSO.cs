using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Spell/Nodes/Condition")]
public class ConditionNodeSO : SpellNodeSO
{
    [Title("Condition Type")]
    [EnumToggleButtons, HideLabel]
    public ConditionType conditionType;

    [BoxGroup("Parameters")]
    [ShowIf("NeedsThreshold")]
    [LabelWidth(130), MinValue(0)] public float threshold;

    [BoxGroup("Parameters")]
    [LabelWidth(130), Range(1f, 5f)] public float bonusMultiplier = 2f;

    private bool NeedsThreshold =>
        conditionType == ConditionType.ComboCount ||
        conditionType == ConditionType.EnemiesNearby;

    // Condition evaluation happens at runtime in SpellExecutor against actual game state.
    // Execute only marks the context so downstream nodes know a condition gate is present.
    public override void Execute(SpellContext ctx) { }
}
