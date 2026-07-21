using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Pool de niveaux réguliers dans lequel LevelDirector pioche sans remise (par run).
/// </summary>
[CreateAssetMenu(fileName = "NewLevelPool", menuName = "SpellCraft/Level Pool")]
public class LevelPoolSO : ScriptableObject
{
    [LabelText("Niveaux disponibles")]
    public List<LevelDefinitionSO> Levels = new();
}
