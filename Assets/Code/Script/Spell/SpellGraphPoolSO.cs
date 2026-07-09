using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Réserve de SpellGraphSO répartis en 4 paliers pondérés (même principe que LootPoolSO,
/// mais pour des sorts entiers plutôt que des nodes). Utilisée pour tirer le sort de départ
/// (SpellSlot.connectedSpell) de chaque slot d'un robot généré (RobotArchetypeSO.Roll()).
/// </summary>
[CreateAssetMenu(menuName = "Spell/Spell Graph Pool")]
public class SpellGraphPoolSO : ScriptableObject
{
    [Serializable]
    public class Tier
    {
        [LabelText("Poids (%)"), MinValue(0f)]
        public float Weight;

        [LabelText("Spellgraphs")]
        public List<SpellGraphSO> Graphs = new();
    }

    [ListDrawerSettings(ShowIndexLabels = true), LabelText("Paliers")]
    public List<Tier> Tiers = new()
    {
        new Tier { Weight = 40f },
        new Tier { Weight = 30f },
        new Tier { Weight = 20f },
        new Tier { Weight = 10f },
    };

    public SpellGraphSO DrawOne()
    {
        var remaining = new List<Tier>(Tiers.Where(t => t.Weight > 0f));

        while (remaining.Count > 0)
        {
            float total = remaining.Sum(t => t.Weight);
            if (total <= 0f) return null;

            float roll = UnityEngine.Random.value * total;
            float cumulative = 0f;
            int chosenIndex = remaining.Count - 1;
            for (int i = 0; i < remaining.Count; i++)
            {
                cumulative += remaining[i].Weight;
                if (roll < cumulative) { chosenIndex = i; break; }
            }

            var bucket = remaining[chosenIndex].Graphs.Where(g => g != null).ToList();
            if (bucket.Count > 0)
                return bucket[UnityEngine.Random.Range(0, bucket.Count)];

            // Palier vide : on le retire et on retente sur les paliers restants.
            remaining.RemoveAt(chosenIndex);
        }

        Debug.LogWarning($"[SpellGraphPoolSO] '{name}' : aucun spellgraph disponible dans les paliers pondérés.");
        return null;
    }
}
