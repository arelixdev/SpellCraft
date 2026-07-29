using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralise les reliques collectées pendant le run et les stats qui n'ont pas déjà de
/// composant propriétaire (dégâts, crit, cooldown des sorts). Vie et vitesse de déplacement
/// restent gérées par PlayerHealth/PlayerController (qui ont déjà leur propre stockage de
/// stat), ce composant se contente d'y écrire via les mêmes méthodes par-source qu'eux.
/// Placé sur le Player, qui survit au rechargement de scène (PlayerPersistence).
/// </summary>
public class RelicManager : MonoBehaviour
{
    private readonly List<RelicSO> _collectedRelics = new();

    // Clé de stacking utilisée par chaque RelicEffect.Apply/Remove de ce ramassage — un
    // `object` neuf par exemplaire, PAS le RelicEffect lui-même : une RelicSO est un asset
    // partagé, donc son RelicEffect reste la même instance à chaque ramassage. L'utiliser
    // comme clé de dictionnaire écraserait le bonus précédent au lieu de s'additionner
    // quand on ramasse deux fois la même relique (cf. Coeur Renforcé x2 ne boostant le
    // max HP qu'une fois). Même index que _collectedRelics.
    private readonly List<object> _collectedSources = new();

    // Stacké multiplicativement/additivement par source, même pattern que
    // PlayerController._speedMultipliers : plusieurs reliques (ou exemplaires) coexistent
    // sans s'écraser tant que chacune a sa propre clé (voir _collectedSources ci-dessus).
    private readonly Dictionary<object, float> _damageMultipliers    = new();
    private readonly Dictionary<object, float> _critChanceBonuses    = new();
    private readonly Dictionary<object, float> _critMultiplierBonuses = new();
    private readonly Dictionary<object, float> _cooldownMultipliers  = new();

    public IReadOnlyList<RelicSO> CollectedRelics => _collectedRelics;

    public event Action<RelicSO> OnRelicCollected;

    public float DamageMultiplier
    {
        get
        {
            float result = 1f;
            foreach (var m in _damageMultipliers.Values) result *= m;
            return result;
        }
    }

    public float CritChanceBonus
    {
        get
        {
            float result = 0f;
            foreach (var v in _critChanceBonuses.Values) result += v;
            return result;
        }
    }

    public float CritMultiplierBonus
    {
        get
        {
            float result = 0f;
            foreach (var v in _critMultiplierBonuses.Values) result += v;
            return result;
        }
    }

    public float CooldownMultiplier
    {
        get
        {
            float result = 1f;
            foreach (var m in _cooldownMultipliers.Values) result *= m;
            return result;
        }
    }

    public void SetDamageMultiplier(object source, float multiplier) => _damageMultipliers[source] = multiplier;
    public void ClearDamageMultiplier(object source) => _damageMultipliers.Remove(source);

    public void SetCritChanceBonus(object source, float delta) => _critChanceBonuses[source] = delta;
    public void ClearCritChanceBonus(object source) => _critChanceBonuses.Remove(source);

    public void SetCritMultiplierBonus(object source, float delta) => _critMultiplierBonuses[source] = delta;
    public void ClearCritMultiplierBonus(object source) => _critMultiplierBonuses.Remove(source);

    public void SetCooldownMultiplier(object source, float multiplier) => _cooldownMultipliers[source] = multiplier;
    public void ClearCooldownMultiplier(object source) => _cooldownMultipliers.Remove(source);

    public void CollectRelic(RelicSO relic)
    {
        if (relic == null) return;

        object source = new object();
        _collectedRelics.Add(relic);
        _collectedSources.Add(source);

        foreach (var effect in relic.Effects)
            effect?.Apply(gameObject, source);

        Debug.Log($"[RelicManager] Relique collectée : {relic.DisplayName}");
        OnRelicCollected?.Invoke(relic);
    }

    // Le Player survit au rechargement de scène (PlayerPersistence) : à appeler au choix
    // d'un nouveau robot (CharacterSelectController.ChooseRobot) pour qu'un nouveau run ne
    // garde pas les reliques du précédent.
    public void ResetForNewRun()
    {
        for (int i = 0; i < _collectedRelics.Count; i++)
            foreach (var effect in _collectedRelics[i].Effects)
                effect?.Remove(gameObject, _collectedSources[i]);

        _collectedRelics.Clear();
        _collectedSources.Clear();
        _damageMultipliers.Clear();
        _critChanceBonuses.Clear();
        _critMultiplierBonuses.Clear();
        _cooldownMultipliers.Clear();
    }
}
