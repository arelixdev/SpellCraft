using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "SpellCraft/Relic Catalog")]
public class RelicCatalogSO : ScriptableObject
{
    [ListDrawerSettings(ShowIndexLabels = true)]
    public List<RelicSO> AllRelics = new();
}
