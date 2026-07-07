using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Sirenix.OdinInspector;

[Serializable, InlineProperty]
public abstract class CorruptionModifier
{
    public abstract void Apply(SpellContext ctx);

    // Passive hooks — only overridden by modifiers that must stay active the whole
    // time the node is wired into an equipped slot, not just while a spell is cast
    // (e.g. a permanent player debuff). No-op by default.
    public virtual void ApplyPermanent(GameObject player)  { }
    public virtual void RemovePermanent(GameObject player) { }
}

// ── Stats multiplicatives ────────────────────────────────────────────────────

[Serializable]
public class DamageMultiplier : CorruptionModifier
{
    [LabelWidth(110), Range(0.1f, 5f)] public float multiplier = 1.5f;
    public override void Apply(SpellContext ctx) => ctx.Damage *= multiplier;
}

[Serializable]
public class SpeedMultiplier : CorruptionModifier
{
    [LabelWidth(110), Range(0.1f, 5f)] public float multiplier = 1.5f;
    public override void Apply(SpellContext ctx) => ctx.Speed *= multiplier;
}

[Serializable]
public class SizeMultiplier : CorruptionModifier
{
    [LabelWidth(110), Range(0.1f, 5f)] public float multiplier = 1.5f;
    public override void Apply(SpellContext ctx) => ctx.Size *= multiplier;
}

// ── Stats additives ──────────────────────────────────────────────────────────

[Serializable]
public class CritChanceModifier : CorruptionModifier
{
    [LabelWidth(110), Range(-1f, 1f)] public float delta = 0.2f;
    public override void Apply(SpellContext ctx)
        => ctx.CritChance = Mathf.Clamp01(ctx.CritChance + delta);
}

[Serializable]
public class CritMultiplierModifier : CorruptionModifier
{
    [LabelWidth(110), Range(-2f, 4f)] public float delta = 0.5f;
    public override void Apply(SpellContext ctx) => ctx.CritMultiplier += delta;
}

[Serializable]
public class PierceModifier : CorruptionModifier
{
    [LabelWidth(110), MinValue(-10)] public int delta = 1;
    public override void Apply(SpellContext ctx)
        => ctx.PierceCount = Mathf.Max(0, ctx.PierceCount + delta);
}

// ── Comportements ────────────────────────────────────────────────────────────

[Serializable]
public class AddBehaviorModifier : CorruptionModifier
{
    [LabelWidth(110)] public BehaviorType behavior = BehaviorType.Bounce;
    public override void Apply(SpellContext ctx)
    {
        if (!ctx.Behaviors.Contains(behavior))
            ctx.Behaviors.Add(behavior);
    }
}

[Serializable]
public class ForceElementModifier : CorruptionModifier
{
    [LabelWidth(110)] public ElementType element = ElementType.Arcane;
    public override void Apply(SpellContext ctx) => ctx.Element = element;
}

// ── Stat générique ───────────────────────────────────────────────────────────
// Pour toute stat de SpellContext qui n'a pas encore de modifier dédié
// (ex: IceSlowPercent, StatusChance, PoisonTickDamage...). Choisis la stat par
// son nom et applique la valeur directement dessus à la création du sort.

[Serializable]
public class StatModifier : CorruptionModifier
{
    public enum Operation { Set, Add, Multiply }

    [LabelWidth(110), ValueDropdown(nameof(GetStatNames))]
    public string statName;

    [LabelWidth(110)] public Operation operation = Operation.Add;

    [LabelWidth(110)] public float value = 1f;

    public override void Apply(SpellContext ctx)
    {
        if (string.IsNullOrEmpty(statName)) return;

        var field = typeof(SpellContext).GetField(statName, BindingFlags.Public | BindingFlags.Instance);
        if (field == null || field.FieldType != typeof(float))
        {
            Debug.LogWarning($"[StatModifier] Stat '{statName}' introuvable (ou non float) sur SpellContext.");
            return;
        }

        float current = (float)field.GetValue(ctx);
        field.SetValue(ctx, operation switch
        {
            Operation.Set      => value,
            Operation.Add      => current + value,
            Operation.Multiply => current * value,
            _                  => current,
        });
    }

    private static IEnumerable<string> GetStatNames() =>
        typeof(SpellContext)
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(float))
            .Select(f => f.Name)
            .OrderBy(n => n);
}

// ── Passif joueur ─────────────────────────────────────────────────────────────
// S'applique tant que le node est branché sur un slot équipé (pas seulement au
// cast) — ex: ralentir le joueur en malus d'un node de gel.

[Serializable]
public class PlayerSpeedModifier : CorruptionModifier
{
    [LabelWidth(110), Range(0.1f, 2f)] public float multiplier = 0.7f;

    public override void Apply(SpellContext ctx) { }

    public override void ApplyPermanent(GameObject player) =>
        player.GetComponent<PlayerController>()?.SetSpeedMultiplier(this, multiplier);

    public override void RemovePermanent(GameObject player) =>
        player.GetComponent<PlayerController>()?.ClearSpeedMultiplier(this);
}

// ── Trigger (nodes OnHit / OnKill / OnCombo / ...) ────────────────────────────
// S'applique au moment où le Trigger node est enregistré (avant qu'il ne se
// déclenche réellement en jeu) — voir SpellExecutor.RegisterTrigger.

[Serializable]
public class TriggerRepeatModifier : CorruptionModifier
{
    [LabelWidth(110), MinValue(1)]
    [Tooltip("Nombre de déclenchements SUPPLÉMENTAIRES (2 = déclenche 3x au total)")]
    public int extraFires = 2;

    public override void Apply(SpellContext ctx) => ctx.PendingTriggerExtraFires += extraFires;
}

[Serializable]
public class SelfDamagePercentOnTriggerModifier : CorruptionModifier
{
    [LabelWidth(110), Range(0f, 1f)] public float percentOfMaxHP = 0.05f;

    public override void Apply(SpellContext ctx) => ctx.PendingTriggerSelfDamagePct += percentOfMaxHP;
}

// ── Poison ────────────────────────────────────────────────────────────────────

[Serializable]
public class PoisonStackModifier : CorruptionModifier
{
    [LabelWidth(110), Range(1, 10)] public int stacksPerHit = 5;
    public override void Apply(SpellContext ctx) => ctx.PoisonStacksPerHit = stacksPerHit;
}

// ── Direction ─────────────────────────────────────────────────────────────────

[Serializable]
public class RandomizeDirectionModifier : CorruptionModifier
{
    [LabelWidth(110), Range(0f, 180f)]
    [Tooltip("180 = direction totalement aléatoire")]
    public float maxAngle = 180f;

    public override void Apply(SpellContext ctx)
    {
        float angle = UnityEngine.Random.Range(-maxAngle, maxAngle);
        ctx.Direction = Quaternion.AngleAxis(angle, Vector3.up) * ctx.Direction;
    }
}
