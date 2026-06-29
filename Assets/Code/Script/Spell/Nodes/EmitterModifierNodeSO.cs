using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Spell/Nodes/EmitterModifier")]
public class EmitterModifierNodeSO : SpellNodeSO
{
    [Title("Modifier Type")]
    [EnumToggleButtons, HideLabel]
    public EmitterModifierType modifierType;

    [Title("Emitter Compatibility")]
    [InfoBox("Ce modificateur ne s'applique qu'aux types d'émetteurs cochés.")]
    [EnumToggleButtons, HideLabel]
    public EmitterTypeFlags compatibleEmitters = EmitterTypeFlags.Projectile;

    // ── Homecoming ────────────────────────────────────────────────────────────
    // Recherche l'ennemi le plus proche dans le rayon et y dirige le projectile.
    // Sans ennemi à portée, le projectile continue tout droit.

    [BoxGroup("Parameters")]
    [ShowIf("modifierType", EmitterModifierType.Homecoming)]
    [LabelWidth(160), MinValue(0.5f)]
    public float homingRadius = 8f;

    [BoxGroup("Parameters")]
    [ShowIf("modifierType", EmitterModifierType.Homecoming)]
    [LabelWidth(160), MinValue(0.1f)]
    [Tooltip("Vitesse de rotation vers la cible en radians/seconde")]
    public float homingStrength = 3f;

    // ── Bounce ────────────────────────────────────────────────────────────────
    // Rebondit sur les obstacles et optionnellement sur les ennemis.

    [BoxGroup("Parameters")]
    [ShowIf("modifierType", EmitterModifierType.Bounce)]
    [LabelWidth(160), MinValue(1)]
    public int bounceCount = 3;

    [BoxGroup("Parameters")]
    [ShowIf("modifierType", EmitterModifierType.Bounce)]
    [LabelWidth(160), ToggleLeft]
    public bool bounceOnEnemies = true;

    // ── Scale ─────────────────────────────────────────────────────────────────
    // Grossit progressivement jusqu'à maxScaleMultiplier × la taille de base.

    [BoxGroup("Parameters")]
    [ShowIf("modifierType", EmitterModifierType.Scale)]
    [LabelWidth(160), MinValue(0.01f)]
    [Tooltip("Unités de scale ajoutées par seconde")]
    public float scaleRate = 0.5f;

    [BoxGroup("Parameters")]
    [ShowIf("modifierType", EmitterModifierType.Scale)]
    [LabelWidth(160), MinValue(1.1f)]
    [Tooltip("Multiplicateur maximum par rapport à la taille initiale")]
    public float maxScaleMultiplier = 3f;

    // ── Back ──────────────────────────────────────────────────────────────────
    // Après backDelay secondes, revient vers le lanceur.

    [BoxGroup("Parameters")]
    [ShowIf("modifierType", EmitterModifierType.Back)]
    [LabelWidth(160), MinValue(0f)]
    [Tooltip("Délai en secondes avant le retour")]
    public float backDelay = 1.5f;

    [BoxGroup("Parameters")]
    [ShowIf("modifierType", EmitterModifierType.Back)]
    [LabelWidth(160), MinValue(10f), MaxValue(720f)]
    [Tooltip("Vitesse de virage en degrés/seconde")]
    public float returnSteerSpeed = 180f;

    // ── Acceleration ──────────────────────────────────────────────────────────
    // Augmente la vitesse (projectile) ou la fréquence de tick (zone) avec le temps.

    [BoxGroup("Parameters")]
    [ShowIf("modifierType", EmitterModifierType.Acceleration)]
    [LabelWidth(160), MinValue(0.1f)]
    [Tooltip("Gain de vitesse en unités/seconde² (projectile) ou diviseur d'intervalle/seconde (zone)")]
    public float accelerationRate = 3f;

    [BoxGroup("Parameters")]
    [ShowIf("modifierType", EmitterModifierType.Acceleration)]
    [LabelWidth(160), MinValue(1.1f)]
    [Tooltip("Multiplicateur maximum de la vitesse/fréquence initiale")]
    public float maxSpeedMultiplier = 3f;

    // ─────────────────────────────────────────────────────────────────────────

    private void OnEnable()   => nodeType = NodeType.EmitterModifier;
    private void Reset()      => compatibleEmitters = EmitterTypeFlags.Projectile;

    private void OnValidate()
    {
        if (compatibleEmitters == EmitterTypeFlags.None)
            Debug.LogWarning($"[EmitterModifier] '{name}' → compatibleEmitters est None, ce node ne s'appliquera jamais.", this);
    }

    public override void Execute(SpellContext ctx)
    {
        EmitterTypeFlags flag = ctx.Emitter switch
        {
            EmitterType.Projectile => EmitterTypeFlags.Projectile,
            EmitterType.Zone       => EmitterTypeFlags.Zone,
            EmitterType.Cone       => EmitterTypeFlags.Cone,
            EmitterType.Beam       => EmitterTypeFlags.Beam,
            EmitterType.Self       => EmitterTypeFlags.Self,
            EmitterType.Grenade    => EmitterTypeFlags.Grenade,
            EmitterType.Orbital    => EmitterTypeFlags.Orbital,
            _                      => EmitterTypeFlags.None,
        };

        if (flag != EmitterTypeFlags.None && (compatibleEmitters & flag) != 0)
            ctx.EmitterModifiers.Add(this);
    }
}
