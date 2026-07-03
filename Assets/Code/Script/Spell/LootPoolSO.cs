using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Table de loot centralisée : tire une SpellNodeSO du catalogue en pondérant par rareté.
/// Une pool différente par contexte (zone normale, zone corrompue, coffre épique, boss).
/// </summary>
[CreateAssetMenu(menuName = "Spell/Loot Pool")]
public class LootPoolSO : ScriptableObject
{
    [Serializable]
    public struct RarityWeight
    {
        public NodeRarity Rarity;
        [MinValue(0f)] public float Weight;
    }

    [Required, LabelText("Catalogue")]
    public SpellNodeCatalogSO Catalog;

    [ListDrawerSettings, LabelText("Pondération par rareté")]
    public List<RarityWeight> Weights = new()
    {
        new RarityWeight { Rarity = NodeRarity.Commun, Weight = 60f },
        new RarityWeight { Rarity = NodeRarity.Rare,    Weight = 30f },
        new RarityWeight { Rarity = NodeRarity.Epique,  Weight = 10f },
    };

    [Title("Biais de synergie")]
    [Range(0f, 1f), LabelText("Chance de proposer une synergie")]
    public float SynergyChance = 0.6f;

    [MinValue(2), LabelText("Seuil de domination (nb nodes)")]
    public int SynergyDominanceThreshold = 2;

    public SpellNodeSO DrawOne() => DrawOneWithBias(null);

    public SpellNodeSO DrawOneWithBias(SpellGraphSO graph)
    {
        var synergyNode = TryDrawSynergy(graph);
        return synergyNode != null ? synergyNode : DrawByRarity();
    }

    public List<SpellNodeSO> DrawThree()
    {
        var result = new List<SpellNodeSO>();
        int attempts = 0;
        while (result.Count < 3 && attempts++ < 50)
        {
            var node = DrawByRarity();
            if (node != null && !result.Contains(node))
                result.Add(node);
        }
        return result;
    }

    private SpellNodeSO DrawByRarity()
    {
        if (Catalog == null || Catalog.allNodes.Count == 0) return null;

        var remaining = new List<RarityWeight>(Weights.Where(w => w.Weight > 0f));

        while (remaining.Count > 0)
        {
            float total = remaining.Sum(w => w.Weight);
            if (total <= 0f) return null;

            float roll = UnityEngine.Random.value * total;
            float cumulative = 0f;
            int chosenIndex = remaining.Count - 1;
            for (int i = 0; i < remaining.Count; i++)
            {
                cumulative += remaining[i].Weight;
                if (roll < cumulative) { chosenIndex = i; break; }
            }

            var rarity = remaining[chosenIndex].Rarity;
            var bucket = Catalog.allNodes.Where(n => n != null && n.rarity == rarity).ToList();
            if (bucket.Count > 0)
                return bucket[UnityEngine.Random.Range(0, bucket.Count)];

            // Bucket vide : retire cette rareté et retente sur les restantes.
            remaining.RemoveAt(chosenIndex);
        }

        Debug.LogWarning($"[LootPoolSO] '{name}' : aucune node disponible pour les raretés pondérées.");
        return null;
    }

    private SpellNodeSO TryDrawSynergy(SpellGraphSO graph)
    {
        if (graph == null || Catalog == null) return null;

        var counts = new Dictionary<ElementType, int>();
        foreach (var n in graph.nodes)
            if (n is ElementNodeSO en)
                counts[en.element] = counts.GetValueOrDefault(en.element) + 1;

        if (counts.Count == 0) return null;

        var dominant = counts.OrderByDescending(kv => kv.Value).First();
        if (dominant.Value < SynergyDominanceThreshold) return null;
        if (UnityEngine.Random.value >= SynergyChance) return null;

        var candidates = graph.nodes
            .OfType<ElementNodeSO>()
            .Where(en => en.element == dominant.Key)
            .SelectMany(en => en.synergies)
            .Where(s => s != null && Catalog.allNodes.Contains(s))
            .Distinct()
            .ToList();

        return candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : null;
    }
}
