using System;
using UnityEngine;
using Sirenix.OdinInspector;

// Effet permanent porté par une relique, appliqué au Player entier pour la durée du run
// (contrairement à CorruptionModifier, qui ne s'applique que tant qu'un node reste câblé
// sur un slot). Remove() n'est appelé que par RelicManager.ResetForNewRun.
[Serializable, InlineProperty]
public abstract class RelicEffect
{
    public abstract void Apply(GameObject player);
    public virtual void Remove(GameObject player) { }
}

[Serializable]
public class MaxHealthBonusEffect : RelicEffect
{
    [LabelWidth(110), MinValue(0f)] public float flatBonus = 20f;
    public override void Apply(GameObject player) => player.GetComponent<PlayerHealth>()?.AddMaxHealthBonus(this, flatBonus);
    public override void Remove(GameObject player) => player.GetComponent<PlayerHealth>()?.RemoveMaxHealthBonus(this);
}

[Serializable]
public class MoveSpeedMultiplierEffect : RelicEffect
{
    [LabelWidth(110), Range(0.5f, 3f)] public float multiplier = 1.15f;
    public override void Apply(GameObject player) => player.GetComponent<PlayerController>()?.SetSpeedMultiplier(this, multiplier);
    public override void Remove(GameObject player) => player.GetComponent<PlayerController>()?.ClearSpeedMultiplier(this);
}

[Serializable]
public class DamageMultiplierEffect : RelicEffect
{
    [LabelWidth(110), Range(0.5f, 3f)] public float multiplier = 1.2f;
    public override void Apply(GameObject player) => player.GetComponent<RelicManager>()?.SetDamageMultiplier(this, multiplier);
    public override void Remove(GameObject player) => player.GetComponent<RelicManager>()?.ClearDamageMultiplier(this);
}

[Serializable]
public class CritChanceBonusEffect : RelicEffect
{
    [LabelWidth(110), Range(0f, 1f)] public float delta = 0.1f;
    public override void Apply(GameObject player) => player.GetComponent<RelicManager>()?.SetCritChanceBonus(this, delta);
    public override void Remove(GameObject player) => player.GetComponent<RelicManager>()?.ClearCritChanceBonus(this);
}

[Serializable]
public class CritMultiplierBonusEffect : RelicEffect
{
    [LabelWidth(110), Range(0f, 3f)] public float delta = 0.3f;
    public override void Apply(GameObject player) => player.GetComponent<RelicManager>()?.SetCritMultiplierBonus(this, delta);
    public override void Remove(GameObject player) => player.GetComponent<RelicManager>()?.ClearCritMultiplierBonus(this);
}

[Serializable]
public class CooldownReductionEffect : RelicEffect
{
    [LabelWidth(110), Range(0.1f, 1f), Tooltip("Multiplie le cooldown des sorts (< 1 = cast plus rapide)")]
    public float cooldownMultiplier = 0.85f;
    public override void Apply(GameObject player) => player.GetComponent<RelicManager>()?.SetCooldownMultiplier(this, cooldownMultiplier);
    public override void Remove(GameObject player) => player.GetComponent<RelicManager>()?.ClearCooldownMultiplier(this);
}

[Serializable]
public class AddSpellSlotEffect : RelicEffect
{
    [LabelWidth(110), Required] public SpellLauncherConfigSO LauncherConfig;
    [LabelWidth(110)] public SpellGraphSO Spell;

    // Pas de retrait : SpellCaster.ResetForNewRun réécrase _spellSlots à partir du loadout
    // du robot choisi au run suivant, ce qui efface déjà les slots ajoutés par relique.
    public override void Apply(GameObject player) => player.GetComponent<SpellCaster>()?.AddSlot(LauncherConfig, Spell);
}
