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

    [Title("Cast Context")]
    [InfoBox("Projectile = depuis le sort actif · Target = depuis l'objet touché · Caster = depuis le lanceur")]
    [EnumToggleButtons, LabelWidth(110)]
    public TriggerSpawnSource spawnSource = TriggerSpawnSource.Projectile;

    [EnumToggleButtons, LabelWidth(110)]
    public TriggerDirectionMode directionMode = TriggerDirectionMode.Inherit;

    [ShowIf("spawnSource", TriggerSpawnSource.Target)]
    [LabelWidth(110), MinValue(0f)]
    public float spawnOffset = 1f;

    public override void Execute(SpellContext ctx) { }
}
