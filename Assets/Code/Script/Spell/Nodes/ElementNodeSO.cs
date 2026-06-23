using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Spell/Nodes/Element")]
public class ElementNodeSO : SpellNodeSO
{
    [Title("Element Type")]
    [EnumToggleButtons, HideLabel]
    public ElementType element;

    // ── Stats communes ─────────────────────────────────────────────────────────

    [BoxGroup("Stats")]
    [HorizontalGroup("Stats/Row")]
    [LabelWidth(120), Range(0.1f, 5f), HideIf("enableRoll")] public float damageMultiplier = 1f;

    [HorizontalGroup("Stats/Row")]
    [LabelWidth(90), Range(0f, 1f), HideIf("enableRoll")] public float statusChance = 0.3f;

    [BoxGroup("Stats")]
    [LabelWidth(120), MinValue(0), HideIf("enableRoll")] public float statusDuration = 2f;

    // ── Stats Fire ─────────────────────────────────────────────────────────────

    [BoxGroup("Fire")]
    [ShowIf("@element == ElementType.Fire && !enableRoll")]
    [HorizontalGroup("Fire/Row")]
    [LabelWidth(120), MinValue(0.1f)] public float fireTickInterval = 0.5f;

    [HorizontalGroup("Fire/Row")]
    [ShowIf("@element == ElementType.Fire && !enableRoll")]
    [LabelWidth(110), MinValue(0f)] public float fireTickDamage = 5f;

    // ── Stats Ice ──────────────────────────────────────────────────────────────

    [BoxGroup("Ice")]
    [ShowIf("@element == ElementType.Ice && !enableRoll")]
    [LabelWidth(120), Range(0f, 1f)] public float iceSlowPercent = 0.5f;

    // ── Tirage ─────────────────────────────────────────────────────────────────

    [Title("Tirage")]
    [ToggleLeft] public bool enableRoll = false;

    [ShowIf("enableRoll"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0.1f, 5f, true), LabelWidth(120)]
    public Vector2 damageMultiplierRange = new(0.8f, 1.5f);

    [ShowIf("enableRoll"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0f, 1f, true), LabelWidth(120)]
    public Vector2 statusChanceRange = new(0.1f, 0.5f);

    [ShowIf("enableRoll"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0f, 10f, true), LabelWidth(120)]
    public Vector2 statusDurationRange = new(1f, 4f);

    [ShowIf("@enableRoll && element == ElementType.Fire"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0.1f, 3f, true), LabelWidth(120)]
    public Vector2 fireTickIntervalRange = new(0.3f, 1f);

    [ShowIf("@enableRoll && element == ElementType.Fire"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0f, 30f, true), LabelWidth(120)]
    public Vector2 fireTickDamageRange = new(3f, 10f);

    [ShowIf("@enableRoll && element == ElementType.Ice"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0f, 1f, true), LabelWidth(120)]
    public Vector2 iceSlowPercentRange = new(0.3f, 0.7f);

    [ShowIf("enableRoll"), BoxGroup("Tirage")]
    [Button("Tirer les valeurs", ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 0.4f)]
    public void RollValues()
    {
        damageMultiplier = Random.Range(damageMultiplierRange.x, damageMultiplierRange.y);
        statusChance     = Random.Range(statusChanceRange.x,     statusChanceRange.y);
        statusDuration   = Random.Range(statusDurationRange.x,   statusDurationRange.y);

        if (element == ElementType.Fire)
        {
            fireTickInterval = Random.Range(fireTickIntervalRange.x, fireTickIntervalRange.y);
            fireTickDamage   = Random.Range(fireTickDamageRange.x,   fireTickDamageRange.y);
        }

        if (element == ElementType.Ice)
            iceSlowPercent = Random.Range(iceSlowPercentRange.x, iceSlowPercentRange.y);
    }

    public override void RuntimeInit()
    {
        if (enableRoll) RollValues();
    }

    public override void Execute(SpellContext ctx)
    {
        ctx.Element  = element;
        ctx.Damage  *= damageMultiplier;
        if (overrideMaterial != null)
            ctx.OverrideMaterial = overrideMaterial;

        ctx.StatusChance   = statusChance;
        ctx.StatusDuration = statusDuration;

        if (element == ElementType.Fire)
        {
            ctx.FireTickInterval = fireTickInterval;
            ctx.FireTickDamage   = fireTickDamage;
        }

        if (element == ElementType.Ice)
            ctx.IceSlowPercent = iceSlowPercent;
    }

    [Title("Visual")]
    [LabelWidth(120)] public Material overrideMaterial;
}
