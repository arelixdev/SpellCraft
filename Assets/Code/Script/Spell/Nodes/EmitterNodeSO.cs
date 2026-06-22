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
    [LabelWidth(50), MinValue(0), HideIf("@enableRoll || emitterType == EmitterType.Orbital")] public float baseSpeed = 10f;

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

    [BoxGroup("Projectile")]
    [ShowIf("@emitterType == EmitterType.Projectile && !enableRoll")]
    [LabelWidth(140), Range(1, 10)] public int burstCount = 1;

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

    // ── Stats Zone ────────────────────────────────────────────────────────────

    [BoxGroup("Zone")]
    [ShowIf("@emitterType == EmitterType.Zone")]
    [EnumToggleButtons, LabelWidth(140)]
    public ZoneType zoneType = ZoneType.StaticOnPlayer;

    [BoxGroup("Zone")]
    [ShowIf("@emitterType == EmitterType.Zone")]
    [EnumToggleButtons, LabelWidth(140)]
    public ZoneDamageMode zoneDamageMode = ZoneDamageMode.Tick;

    [BoxGroup("Zone")]
    [ShowIf("@emitterType == EmitterType.Zone && !enableRoll")]
    [LabelWidth(140), MinValue(0.5f)] public float zoneRadius = 3f;

    [BoxGroup("Zone")]
    [ShowIf("@emitterType == EmitterType.Zone && zoneType != ZoneType.StaticOnPlayer && !enableRoll")]
    [LabelWidth(140), MinValue(0.1f)] public float zoneGrowDuration = 1f;

    [BoxGroup("Zone")]
    [ShowIf("@emitterType == EmitterType.Zone && !enableRoll")]
    [LabelWidth(140), MinValue(0f), Tooltip("Durée totale en secondes (0 = permanent)")]
    public float zoneDuration = 0f;

    [BoxGroup("Zone")]
    [ShowIf("@emitterType == EmitterType.Zone && (zoneDamageMode == ZoneDamageMode.Tick || zoneDamageMode == ZoneDamageMode.Both) && !enableRoll")]
    [LabelWidth(140), MinValue(0.1f)] public float zoneTickInterval = 1f;

    // ── Stats Orbital ─────────────────────────────────────────────────────────

    [BoxGroup("Orbital")]
    [ShowIf("@emitterType == EmitterType.Orbital && !enableRoll")]
    [LabelWidth(140), Range(1, 10)] public int orbitalCount = 3;

    [BoxGroup("Orbital")]
    [ShowIf("@emitterType == EmitterType.Orbital && !enableRoll")]
    [LabelWidth(140), MinValue(0.5f)] public float orbitalRadius = 3f;

    [BoxGroup("Orbital")]
    [ShowIf("@emitterType == EmitterType.Orbital && !enableRoll")]
    [LabelWidth(140), Range(10f, 720f)] public float orbitalSpeed = 90f;

    [BoxGroup("Orbital")]
    [ShowIf("@emitterType == EmitterType.Orbital")]
    [LabelWidth(140), ToggleLeft] public bool orbitalPermanent = true;

    [BoxGroup("Orbital")]
    [ShowIf("@emitterType == EmitterType.Orbital && !enableRoll && !orbitalPermanent")]
    [LabelWidth(140), MinValue(0.1f)] public float orbitalLifetime = 5f;

    // ── Tirage ────────────────────────────────────────────────────────────────

    [Title("Tirage")]
    [ToggleLeft] public bool enableRoll = false;

    [ShowIf("enableRoll"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0f, 200f, true), LabelWidth(80)]
    public Vector2 damageRange = new(5f, 15f);

    [ShowIf("@enableRoll && emitterType != EmitterType.Orbital"), BoxGroup("Tirage/Ranges")]
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

    [ShowIf("@enableRoll && emitterType == EmitterType.Projectile"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(1, 10, true), LabelWidth(80)]
    public Vector2Int burstCountRange = new(1, 3);

    [ShowIf("@enableRoll && emitterType == EmitterType.Grenade"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(1, 10, true), LabelWidth(80)]
    public Vector2Int bounceCountRange = new(2, 5);

    [ShowIf("@enableRoll && emitterType == EmitterType.Grenade"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(10f, 80f, true), LabelWidth(80)]
    public Vector2 launchAngleRange = new(30f, 60f);

    [ShowIf("@enableRoll && emitterType == EmitterType.Orbital"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(1, 10, true), LabelWidth(80)]
    public Vector2Int orbitalCountRange = new(2, 4);

    [ShowIf("@enableRoll && emitterType == EmitterType.Orbital"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0.5f, 15f, true), LabelWidth(80)]
    public Vector2 orbitalRadiusRange = new(2f, 5f);

    [ShowIf("@enableRoll && emitterType == EmitterType.Orbital"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(10f, 720f, true), LabelWidth(80)]
    public Vector2 orbitalSpeedRange = new(60f, 180f);

    [ShowIf("@enableRoll && emitterType == EmitterType.Orbital && !orbitalPermanent"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0.5f, 30f, true), LabelWidth(80)]
    public Vector2 orbitalLifetimeRange = new(3f, 10f);

    [ShowIf("@enableRoll && emitterType == EmitterType.Zone"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0.5f, 20f, true), LabelWidth(80)]
    public Vector2 zoneRadiusRange = new(2f, 6f);

    [ShowIf("@enableRoll && emitterType == EmitterType.Zone && zoneType != ZoneType.StaticOnPlayer"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0.1f, 5f, true), LabelWidth(80)]
    public Vector2 zoneGrowDurationRange = new(0.5f, 2f);

    [ShowIf("@enableRoll && emitterType == EmitterType.Zone"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0f, 20f, true), LabelWidth(80), Tooltip("0 = permanent")]
    public Vector2 zoneDurationRange = new(3f, 8f);

    [ShowIf("@enableRoll && emitterType == EmitterType.Zone && (zoneDamageMode == ZoneDamageMode.Tick || zoneDamageMode == ZoneDamageMode.Both)"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0.1f, 5f, true), LabelWidth(80)]
    public Vector2 zoneTickIntervalRange = new(0.5f, 2f);

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
            burstCount      = Random.Range(burstCountRange.x,      burstCountRange.y + 1);
        }

        if (emitterType == EmitterType.Grenade)
        {
            bounceCount     = Random.Range(bounceCountRange.x,  bounceCountRange.y + 1);
            launchAngle     = Random.Range(launchAngleRange.x,  launchAngleRange.y);
            projectileCount = Random.Range(projectileCountRange.x, projectileCountRange.y + 1);
        }

        if (emitterType == EmitterType.Orbital)
        {
            orbitalCount  = Random.Range(orbitalCountRange.x,  orbitalCountRange.y + 1);
            orbitalRadius = Random.Range(orbitalRadiusRange.x, orbitalRadiusRange.y);
            orbitalSpeed  = Random.Range(orbitalSpeedRange.x,  orbitalSpeedRange.y);
            if (!orbitalPermanent)
                orbitalLifetime = Random.Range(orbitalLifetimeRange.x, orbitalLifetimeRange.y);
        }

        if (emitterType == EmitterType.Zone)
        {
            zoneRadius   = Random.Range(zoneRadiusRange.x,   zoneRadiusRange.y);
            zoneDuration = Random.Range(zoneDurationRange.x, zoneDurationRange.y);
            if (zoneType != ZoneType.StaticOnPlayer)
                zoneGrowDuration = Random.Range(zoneGrowDurationRange.x, zoneGrowDurationRange.y);
            if (zoneDamageMode == ZoneDamageMode.Tick || zoneDamageMode == ZoneDamageMode.Both)
                zoneTickInterval = Random.Range(zoneTickIntervalRange.x, zoneTickIntervalRange.y);
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
            ctx.BurstCount            = burstCount;
        }

        if (emitterType == EmitterType.Grenade)
            ctx.Lifetime = baseLifetimeGrenade;

        if (emitterType == EmitterType.Orbital)
        {
            ctx.OrbitalRadius    = orbitalRadius;
            ctx.OrbitalSpeed     = orbitalSpeed;
            ctx.OrbitalPermanent = orbitalPermanent;
            ctx.OrbitalLifetime  = orbitalLifetime;
        }

        if (emitterType == EmitterType.Zone)
        {
            ctx.ZoneType         = zoneType;
            ctx.ZoneDamageMode   = zoneDamageMode;
            ctx.ZoneRadius       = zoneRadius;
            ctx.ZoneGrowDuration = zoneGrowDuration;
            ctx.ZoneDuration     = zoneDuration;
            ctx.ZoneTickInterval = zoneTickInterval;
        }
    }
}
