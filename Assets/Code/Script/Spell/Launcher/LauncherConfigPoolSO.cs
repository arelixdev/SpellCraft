using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Réserve de SpellLauncherConfigSO utilisée pour composer le loadout de slots d'un
/// robot généré (voir RobotArchetypeSO.Roll()). Les configs clavier sont piochées comme
/// un préfixe ordonné (1, puis 1+2, puis 1+2+3...) : jamais 3/4 sans 1/2, pour que les
/// touches utilisées correspondent toujours à l'ordre affiché dans la barre de slots.
/// </summary>
[CreateAssetMenu(fileName = "LauncherConfigPool", menuName = "SpellCraft/Launcher Config Pool")]
public class LauncherConfigPoolSO : ScriptableObject
{
    [LabelText("Configs sans touche (Autocast, etc.)")]
    public List<SpellLauncherConfigSO> NonSequentialConfigs = new();

    [LabelText("Configs clavier (dans l'ordre 1, 2, 3, 4)")]
    public List<SpellLauncherConfigSO> OrderedKeybindConfigs = new();

    /// <summary>
    /// Tire 'count' configs pour 'count' slots. Le nombre de slots clavier est tiré au
    /// hasard (0 à min(count, touches disponibles)), mais ce sont toujours les touches
    /// les plus basses (1, 2, 3...) qui sont utilisées — jamais un trou dans la séquence.
    /// Le reste des slots reçoit une config sans touche (piochée avec remise).
    /// </summary>
    public List<SpellLauncherConfigSO> DrawForSlotCount(int count)
    {
        var result = new List<SpellLauncherConfigSO>();

        int maxKeybind = Mathf.Min(count, OrderedKeybindConfigs.Count);
        int keybindCount = NonSequentialConfigs.Count > 0 ? Random.Range(0, maxKeybind + 1) : maxKeybind;

        for (int i = 0; i < keybindCount; i++)
            result.Add(OrderedKeybindConfigs[i]);

        int nonKeybindCount = count - keybindCount;
        for (int i = 0; i < nonKeybindCount && NonSequentialConfigs.Count > 0; i++)
            result.Add(NonSequentialConfigs[Random.Range(0, NonSequentialConfigs.Count)]);

        return result;
    }
}
