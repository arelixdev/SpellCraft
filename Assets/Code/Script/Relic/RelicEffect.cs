using System;
using UnityEngine;
using Sirenix.OdinInspector;

// Effet permanent porté par une relique, appliqué au Player entier pour la durée du run
// (contrairement à CorruptionModifier, qui ne s'applique que tant qu'un node reste câblé
// sur un slot). Remove() n'est appelé que par RelicManager.ResetForNewRun.
//
// `source` est la clé de stacking fournie par RelicManager.CollectRelic (un `object` neuf
// par EXEMPLAIRE ramassé) — surtout ne pas utiliser `this` à la place : une RelicSO est un
// asset ScriptableObject partagé, donc son RelicEffect est la MÊME instance à chaque
// ramassage. L'utiliser comme clé de dictionnaire écraserait silencieusement le bonus
// précédent au lieu de s'empiler quand on ramasse deux fois la même relique.
[Serializable, InlineProperty]
public abstract class RelicEffect
{
    public abstract void Apply(GameObject player, object source);
    public virtual void Remove(GameObject player, object source) { }
}

[Serializable]
public class MaxHealthBonusEffect : RelicEffect
{
    [LabelWidth(110), MinValue(0f)] public float flatBonus = 20f;
    public override void Apply(GameObject player, object source) => player.GetComponent<PlayerHealth>()?.AddMaxHealthBonus(source, flatBonus);
    public override void Remove(GameObject player, object source) => player.GetComponent<PlayerHealth>()?.RemoveMaxHealthBonus(source);
}

[Serializable]
public class MoveSpeedMultiplierEffect : RelicEffect
{
    [LabelWidth(110), Range(0.5f, 3f)] public float multiplier = 1.15f;
    public override void Apply(GameObject player, object source) => player.GetComponent<PlayerController>()?.SetSpeedMultiplier(source, multiplier);
    public override void Remove(GameObject player, object source) => player.GetComponent<PlayerController>()?.ClearSpeedMultiplier(source);
}

[Serializable]
public class DamageMultiplierEffect : RelicEffect
{
    [LabelWidth(110), Range(0.5f, 3f)] public float multiplier = 1.2f;
    public override void Apply(GameObject player, object source) => player.GetComponent<RelicManager>()?.SetDamageMultiplier(source, multiplier);
    public override void Remove(GameObject player, object source) => player.GetComponent<RelicManager>()?.ClearDamageMultiplier(source);
}

[Serializable]
public class CritChanceBonusEffect : RelicEffect
{
    [LabelWidth(110), Range(0f, 1f)] public float delta = 0.1f;
    public override void Apply(GameObject player, object source) => player.GetComponent<RelicManager>()?.SetCritChanceBonus(source, delta);
    public override void Remove(GameObject player, object source) => player.GetComponent<RelicManager>()?.ClearCritChanceBonus(source);
}

[Serializable]
public class CritMultiplierBonusEffect : RelicEffect
{
    [LabelWidth(110), Range(0f, 3f)] public float delta = 0.3f;
    public override void Apply(GameObject player, object source) => player.GetComponent<RelicManager>()?.SetCritMultiplierBonus(source, delta);
    public override void Remove(GameObject player, object source) => player.GetComponent<RelicManager>()?.ClearCritMultiplierBonus(source);
}

[Serializable]
public class CooldownReductionEffect : RelicEffect
{
    [LabelWidth(110), Range(0.1f, 1f), Tooltip("Multiplie le cooldown des sorts (< 1 = cast plus rapide)")]
    public float cooldownMultiplier = 0.85f;
    public override void Apply(GameObject player, object source) => player.GetComponent<RelicManager>()?.SetCooldownMultiplier(source, cooldownMultiplier);
    public override void Remove(GameObject player, object source) => player.GetComponent<RelicManager>()?.ClearCooldownMultiplier(source);
}

[Serializable]
public class AddSpellSlotEffect : RelicEffect
{
    [LabelWidth(110), Required] public SpellLauncherConfigSO LauncherConfig;
    [LabelWidth(110)] public SpellGraphSO Spell;

    // Pas de retrait : SpellCaster.ResetForNewRun réécrase _spellSlots à partir du loadout
    // du robot choisi au run suivant, ce qui efface déjà les slots ajoutés par relique.
    public override void Apply(GameObject player, object source) => player.GetComponent<SpellCaster>()?.AddSlot(LauncherConfig, Spell);
}
