using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Réserve de SpellLauncherConfigSO tirés au sort pour composer le loadout de slots
/// d'un robot généré (voir RobotArchetypeSO.Roll()).
/// </summary>
[CreateAssetMenu(fileName = "LauncherConfigPool", menuName = "SpellCraft/Launcher Config Pool")]
public class LauncherConfigPoolSO : ScriptableObject
{
    [LabelText("Configs possibles")]
    public List<SpellLauncherConfigSO> Configs = new();

    /// <summary>
    /// Tire 'count' configs distinctes (sans doublon, donc jamais deux slots sur la
    /// même touche). Si 'count' dépasse le nombre de configs disponibles, il est réduit.
    /// </summary>
    public List<SpellLauncherConfigSO> DrawUnique(int count)
    {
        var pool = new List<SpellLauncherConfigSO>(Configs);
        var result = new List<SpellLauncherConfigSO>();

        count = Mathf.Min(count, pool.Count);
        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }
}
