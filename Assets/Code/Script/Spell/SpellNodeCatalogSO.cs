using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Spell/NodeCatalog")]
public class SpellNodeCatalogSO : ScriptableObject
{
    [ListDrawerSettings(ShowIndexLabels = true)]
    public List<SpellNodeSO> allNodes = new();
}
