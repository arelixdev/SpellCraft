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
    [LabelWidth(60), MinValue(0), HideIf("enableRoll")] public float baseDamage = 10f;

    [HorizontalGroup("Base Stats/Values")]
    [LabelWidth(50), MinValue(0), HideIf("enableRoll")] public float baseSpeed = 10f;

    [HorizontalGroup("Base Stats/Values")]
    [LabelWidth(45), MinValue(0), HideIf("enableRoll")] public float baseSize = 1f;

    [BoxGroup("Base Stats")]
    [ShowIf("@emitterType == EmitterType.Projectile && !enableRoll")]
    [LabelWidth(140), Range(1, 10)] public int projectileCount = 1;

    [BoxGroup("Base Stats")]
    [ShowIf("@emitterType == EmitterType.Projectile && !enableRoll && projectileCount > 1")]
    [LabelWidth(140), Range(0f, 180f)] public float spreadAngle = 30f;

    [BoxGroup("Base Stats")]
    [ShowIf("@emitterType == EmitterType.Projectile && !enableRoll")]
    [LabelWidth(140), MinValue(0.1f)] public float baseLifetime = 3f;

    [BoxGroup("Base Stats")]
    [ShowIf("@emitterType == EmitterType.Projectile && !enableRoll")]
    [LabelWidth(140), MinValue(0)] public int pierceCount = 0;

    [BoxGroup("Base Stats")]
    [ShowIf("@emitterType == EmitterType.Projectile && pierceCount > 0")]
    [LabelWidth(140)] public bool reduceDamageOnPierce = false;

    [BoxGroup("Base Stats")]
    [ShowIf("@emitterType == EmitterType.Projectile && pierceCount > 0 && reduceDamageOnPierce")]
    [LabelWidth(140), Range(0f, 100f)] public float damageReductionPerHit = 25f;

    // --- Tirage ---
    [Title("Tirage")]
    [ToggleLeft] public bool enableRoll = false;

    [ShowIf("enableRoll"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0f, 200f, true), LabelWidth(80)]
    public Vector2 damageRange = new(5f, 15f);

    [ShowIf("enableRoll"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0f, 50f, true), LabelWidth(80)]
    public Vector2 speedRange = new(8f, 12f);

    [ShowIf("enableRoll"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0f, 10f, true), LabelWidth(80)]
    public Vector2 sizeRange = new(0.5f, 2f);

    [ShowIf("@enableRoll && emitterType == EmitterType.Projectile"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0, 10, true), LabelWidth(80)]
    public Vector2Int pierceRange = new(0, 3);

    [ShowIf("@enableRoll && emitterType == EmitterType.Projectile"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(1, 10, true), LabelWidth(80)]
    public Vector2Int projectileCountRange = new(1, 3);

    [ShowIf("@enableRoll && emitterType == EmitterType.Projectile"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0.1f, 20f, true), LabelWidth(80)]
    public Vector2 lifetimeRange = new(1f, 5f);

    [ShowIf("enableRoll"), BoxGroup("Tirage")]
    [Button("Tirer les valeurs", ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 0.4f)]
    public void RollValues()
    {
        baseDamage = Random.Range(damageRange.x, damageRange.y);
        baseSpeed  = Random.Range(speedRange.x,  speedRange.y);
        baseSize   = Random.Range(sizeRange.x,   sizeRange.y);

        if (emitterType == EmitterType.Projectile)
        {
            pierceCount     = Random.Range(pierceRange.x,          pierceRange.y + 1);
            projectileCount = Random.Range(projectileCountRange.x, projectileCountRange.y + 1);
            baseLifetime    = Random.Range(lifetimeRange.x,        lifetimeRange.y);
        }
    }

    public override void RuntimeInit()
    {
        if (enableRoll) RollValues();
    }

    public override void Execute(SpellContext ctx)
    {
        ctx.Emitter     = emitterType;
        ctx.Damage      = baseDamage;
        ctx.Speed       = baseSpeed;
        ctx.Size        = baseSize;
        ctx.Lifetime               = baseLifetime;
        ctx.PierceCount            = pierceCount;
        ctx.ReduceDamageOnPierce   = reduceDamageOnPierce;
        ctx.DamageReductionPerHit  = damageReductionPerHit;
    }
}
