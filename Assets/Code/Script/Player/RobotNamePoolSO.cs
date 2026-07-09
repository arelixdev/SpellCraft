using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Réserve de noms tirés au sort pour les robots générés par RobotArchetypeSO.Roll().
/// </summary>
[CreateAssetMenu(fileName = "RobotNamePool", menuName = "SpellCraft/Robot Name Pool")]
public class RobotNamePoolSO : ScriptableObject
{
    [LabelText("Noms possibles")]
    public List<string> Names = new();

    public string GetRandom()
    {
        if (Names == null || Names.Count == 0) return "Robot";
        return Names[Random.Range(0, Names.Count)];
    }
}
