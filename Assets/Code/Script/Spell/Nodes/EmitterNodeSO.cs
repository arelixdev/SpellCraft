using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Spell/Nodes/Emitter")]
public class EmitterNodeSO : SpellNodeSO
{
    [Title("Emitter Type")]
    [EnumToggleButtons, HideLabel]
    public EmitterType emitterType;

    // ── Stats communes ────────────────────────────────────────────────────────

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
    [ShowIf("@(emitterType == EmitterType.Projectile || emitterType == EmitterType.Grenade) && !enableRoll")]
    [LabelWidth(140), Range(1, 10)] public int projectileCount = 1;

    [BoxGroup("Base Stats")]
    [ShowIf("@(emitterType == EmitterType.Projectile || emitterType == EmitterType.Grenade) && !enableRoll && projectileCount > 1")]
    [LabelWidth(140), Range(0f, 180f)] public float spreadAngle = 30f;

    // ── Stats Projectile ──────────────────────────────────────────────────────

    [BoxGroup("Projectile")]
    [ShowIf("@emitterType == EmitterType.Projectile && !enableRoll")]
    [LabelWidth(140), MinValue(0.1f)] public float baseLifetime = 3f;

    [BoxGroup("Projectile")]
    [ShowIf("@emitterType == EmitterType.Projectile && !enableRoll")]
    [LabelWidth(140), MinValue(0)] public int pierceCount = 0;

    [BoxGroup("Projectile")]
    [ShowIf("@emitterType == EmitterType.Projectile && pierceCount > 0")]
    [LabelWidth(140)] public bool reduceDamageOnPierce = false;

    [BoxGroup("Projectile")]
    [ShowIf("@emitterType == EmitterType.Projectile && pierceCount > 0 && reduceDamageOnPierce")]
    [LabelWidth(140), Range(0f, 100f)] public float damageReductionPerHit = 25f;

    // ── Stats Grenade ─────────────────────────────────────────────────────────

    [BoxGroup("Grenade")]
    [ShowIf("@emitterType == EmitterType.Grenade && !enableRoll")]
    [LabelWidth(140), Range(1, 10)] public int bounceCount = 3;

    [BoxGroup("Grenade")]
    [ShowIf("@emitterType == EmitterType.Grenade && !enableRoll")]
    [LabelWidth(140), Range(10f, 80f)] public float launchAngle = 45f;

    [BoxGroup("Grenade")]
    [ShowIf("@emitterType == EmitterType.Grenade && !enableRoll")]
    [LabelWidth(140), MinValue(0.1f)] public float baseLifetimeGrenade = 10f;

    [BoxGroup("Grenade")]
    [ShowIf("@emitterType == EmitterType.Grenade")]
    [LabelWidth(140), Range(1f, 10f)] public float gravityMultiplier = 3f;

    [BoxGroup("Grenade")]
    [ShowIf("@emitterType == EmitterType.Grenade")]
    [LabelWidth(140), MinValue(0.1f)] public float explosionRadius = 3f;

    [BoxGroup("Grenade")]
    [ShowIf("@emitterType == EmitterType.Grenade")]
    [LabelWidth(140), Range(0.5f, 5f)] public float explosionDamageMultiplier = 1.5f;

    [BoxGroup("Grenade")]
    [ShowIf("@emitterType == EmitterType.Grenade")]
    [LabelWidth(140)] public GameObject explosionPrefab;

    // ── Tirage ────────────────────────────────────────────────────────────────

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

    [ShowIf("@enableRoll && (emitterType == EmitterType.Projectile || emitterType == EmitterType.Grenade)"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(1, 10, true), LabelWidth(80)]
    public Vector2Int projectileCountRange = new(1, 3);

    [ShowIf("@enableRoll && emitterType == EmitterType.Projectile"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0.1f, 20f, true), LabelWidth(80)]
    public Vector2 lifetimeRange = new(1f, 5f);

    [ShowIf("@enableRoll && emitterType == EmitterType.Grenade"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(1, 10, true), LabelWidth(80)]
    public Vector2Int bounceCountRange = new(2, 5);

    [ShowIf("@enableRoll && emitterType == EmitterType.Grenade"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(10f, 80f, true), LabelWidth(80)]
    public Vector2 launchAngleRange = new(30f, 60f);

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

        if (emitterType == EmitterType.Grenade)
        {
            bounceCount     = Random.Range(bounceCountRange.x,  bounceCountRange.y + 1);
            launchAngle     = Random.Range(launchAngleRange.x,  launchAngleRange.y);
            projectileCount = Random.Range(projectileCountRange.x, projectileCountRange.y + 1);
        }
    }

    public override void RuntimeInit()
    {
        if (enableRoll) RollValues();
    }

    public override void Execute(SpellContext ctx)
    {
        ctx.Emitter    = emitterType;
        ctx.Damage     = baseDamage;
        ctx.Speed      = baseSpeed;
        ctx.Size       = baseSize;

        if (emitterType == EmitterType.Projectile)
        {
            ctx.Lifetime              = baseLifetime;
            ctx.PierceCount           = pierceCount;
            ctx.ReduceDamageOnPierce  = reduceDamageOnPierce;
            ctx.DamageReductionPerHit = damageReductionPerHit;
        }

        if (emitterType == EmitterType.Grenade)
            ctx.Lifetime = baseLifetimeGrenade;
    }
}
