using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Spell/Nodes/Trigger")]
public class TriggerNodeSO : SpellNodeSO
{
    [Title("Trigger Type")]
    [EnumToggleButtons, HideLabel]
    public TriggerType triggerType;

    [BoxGroup("Parameters")]
    [ShowIf("@triggerType == TriggerType.OnTick && !enableRoll")]
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

    // ── Tirage ────────────────────────────────────────────────────────────────

    [Title("Tirage")]
    [ShowIf("triggerType", TriggerType.OnTick)]
    [ToggleLeft] public bool enableRoll = false;

    [ShowIf("@enableRoll && triggerType == TriggerType.OnTick"), BoxGroup("Tirage/Ranges")]
    [MinMaxSlider(0.1f, 10f, true), LabelWidth(110)]
    public Vector2 tickIntervalRange = new(0.3f, 1.5f);

    [ShowIf("@enableRoll && triggerType == TriggerType.OnTick"), BoxGroup("Tirage")]
    [Button("Tirer les valeurs", ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 0.4f)]
    public void RollValues()
    {
        tickInterval = Random.Range(tickIntervalRange.x, tickIntervalRange.y);
    }

    public override void RuntimeInit()
    {
        if (enableRoll) RollValues();
    }

    public override void Execute(SpellContext ctx) { }
}
