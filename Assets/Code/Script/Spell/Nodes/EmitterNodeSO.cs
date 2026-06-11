using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Spell/Nodes/Emitter")]
public class EmitterNodeSO : SpellNodeSO
{
    [Title("Emitter Type")]
    [EnumToggleButtons, HideLabel]
    public EmitterType emitterType;

    [BoxGroup("Base Stats")]
    [Required, LabelWidth(140)]
    public GameObject projectilePrefab;

    [BoxGroup("Base Stats")]
    [HorizontalGroup("Base Stats/Values")]
    [LabelWidth(60), MinValue(0)] public float baseDamage = 10f;

    [HorizontalGroup("Base Stats/Values")]
    [LabelWidth(50), MinValue(0)] public float baseSpeed = 10f;

    [HorizontalGroup("Base Stats/Values")]
    [LabelWidth(45), MinValue(0)] public float baseSize = 1f;

    public override void Execute(SpellContext ctx)
    {
        ctx.Emitter = emitterType;
        ctx.Damage  = baseDamage;
        ctx.Speed   = baseSpeed;
        ctx.Size    = baseSize;
    }
}
