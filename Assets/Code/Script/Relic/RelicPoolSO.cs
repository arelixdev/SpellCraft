using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Table de loot pour les reliques : tire une RelicSO du catalogue en pondérant par rareté.
/// Même algorithme que LootPoolSO, sans le biais de synergie (n'a pas de sens hors sorts).
/// </summary>
[CreateAssetMenu(menuName = "SpellCraft/Relic Pool")]
public class RelicPoolSO : ScriptableObject
{
    [Serializable]
    public struct RarityWeight
    {
        public NodeRarity Rarity;
        [MinValue(0f)] public float Weight;
    }

    [Required, LabelText("Catalogue")]
    public RelicCatalogSO Catalog;

    [ListDrawerSettings, LabelText("Pondération par rareté")]
    public List<RarityWeight> Weights = new()
    {
        new RarityWeight { Rarity = NodeRarity.Commun, Weight = 60f },
        new RarityWeight { Rarity = NodeRarity.Rare,    Weight = 30f },
        new RarityWeight { Rarity = NodeRarity.Epique,  Weight = 10f },
    };

    // Tirage sans remise pour un choix multiple (autel à choix) — même approche que
    // LootPoolSO.DrawThree : retire les doublons après coup plutôt que d'exclure la
    // rareté déjà tirée, pour ne pas fausser la pondération entre les 3 propositions.
    public List<RelicSO> DrawThree()
    {
        var result = new List<RelicSO>();
        int attempts = 0;
        while (result.Count < 3 && attempts++ < 50)
        {
            var relic = DrawOne();
            if (relic != null && !result.Contains(relic))
                result.Add(relic);
        }
        return result;
    }

    public RelicSO DrawOne()
    {
        if (Catalog == null || Catalog.AllRelics.Count == 0) return null;

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
            var bucket = Catalog.AllRelics.Where(r => r != null && r.Rarity == rarity).ToList();
            if (bucket.Count > 0)
                return bucket[UnityEngine.Random.Range(0, bucket.Count)];

            // Bucket vide : retire cette rareté et retente sur les restantes.
            remaining.RemoveAt(chosenIndex);
        }

        Debug.LogWarning($"[RelicPoolSO] '{name}' : aucune relique disponible pour les raretés pondérées.");
        return null;
    }
}
