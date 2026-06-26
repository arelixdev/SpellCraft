using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Spell/Nodes/Trigger")]
public class TriggerNodeSO : SpellNodeSO
{
    [Title("Trigger Type")]
    [EnumToggleButtons, HideLabel]
    public TriggerType triggerType;

    // ── OnTick ────────────────────────────────────────────────────────────────

    [BoxGroup("Parameters")]
    [ShowIf("@triggerType == TriggerType.OnTick && !enableRoll")]
    [LabelWidth(110), MinValue(0.1f)] public float tickInterval = 0.5f;

    // ── OnStatus ──────────────────────────────────────────────────────────────

    [BoxGroup("Parameters")]
    [ShowIf("triggerType", TriggerType.OnStatus)]
    [InfoBox("Se déclenche quand ce statut est appliqué à l'ennemi.")]
    [EnumToggleButtons, LabelWidth(110)]
    public ElementType statusFilter = ElementType.Fire;

    // ── OnCombo ───────────────────────────────────────────────────────────────

    [BoxGroup("Parameters")]
    [ShowIf("@triggerType == TriggerType.OnCombo && !enableRoll")]
    [LabelWidth(110), MinValue(2)]
    public int comboThreshold = 3;

    // ── Cast Context ──────────────────────────────────────────────────────────

    [Title("Cast Context")]
    [InfoBox("Projectile = depuis le sort actif · Target = depuis l'objet touché · Caster = depuis le lanceur")]
    [EnumToggleButtons, LabelWidth(110)]
    public TriggerSpawnSource spawnSource = TriggerSpawnSource.Projectile;

    [EnumToggleButtons, LabelWidth(110)]
    public TriggerDirectionMode directionMode = TriggerDirectionMode.Inherit;

    [ShowIf("spawnSource", TriggerSpawnSource.Target)]
    [LabelWidth(110), MinValue(0f)]
    public float spawnOffset = 1f;

    // ── Tirage ────────────────────────────────────────────────────────────────

    [Title("Tirage")]
    [ShowIf("@triggerType == TriggerType.OnTick || triggerType == TriggerType.OnCombo")]
    [ToggleLeft] public bool enableRoll = false;

    [ShowIf("@enableRoll && triggerType == TriggerType.OnTick"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0.1f, 10f, true), LabelWidth(120)]
    public Vector2 tickIntervalRange = new(0.3f, 1.5f);

    [ShowIf("@enableRoll && triggerType == TriggerType.OnCombo"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(2, 10, true), LabelWidth(120)]
    public Vector2Int comboThresholdRange = new(2, 5);

    [ShowIf("enableRoll"), BoxGroup("Tirage")]
    [Button("Tirer les valeurs", ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 0.4f)]
    public void RollValues()
    {
        if (triggerType == TriggerType.OnTick)
            tickInterval = Random.Range(tickIntervalRange.x, tickIntervalRange.y);
        if (triggerType == TriggerType.OnCombo)
            comboThreshold = Random.Range(comboThresholdRange.x, comboThresholdRange.y + 1);
    }

    public override void RuntimeInit()
    {
        if (enableRoll) RollValues();
    }

    public override void Execute(SpellContext ctx) { }
}
