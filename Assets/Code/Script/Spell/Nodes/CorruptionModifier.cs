using System;
using UnityEngine;
using Sirenix.OdinInspector;

[Serializable]
public abstract class CorruptionModifier
{
    public abstract void Apply(SpellContext ctx);
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
