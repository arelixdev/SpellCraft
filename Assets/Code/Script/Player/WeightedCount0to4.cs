using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Pondération réutilisable pour tirer un nombre entre 0 et 4 (nombre de slots, nombre de
/// slots remplis d'un sort, etc.). Weight0 par défaut à 0 : utilisée telle quelle pour un
/// nombre de slots (toujours ≥ 1), ou avec Weight0 > 0 pour autoriser "aucun" (ex : slots
/// remplis d'un sort de départ).
/// </summary>
[Serializable]
public class WeightedCount0to4
{
    [HorizontalGroup("Weights"), LabelText("0"), MinValue(0f)] public float Weight0 = 0f;
    [HorizontalGroup("Weights"), LabelText("1"), MinValue(0f)] public float Weight1 = 40f;
    [HorizontalGroup("Weights"), LabelText("2"), MinValue(0f)] public float Weight2 = 30f;
    [HorizontalGroup("Weights"), LabelText("3"), MinValue(0f)] public float Weight3 = 20f;
    [HorizontalGroup("Weights"), LabelText("4"), MinValue(0f)] public float Weight4 = 10f;

    public int Roll()
    {
        float total = Weight0 + Weight1 + Weight2 + Weight3 + Weight4;
        if (total <= 0f) return 1;

        float roll = UnityEngine.Random.value * total;
        float cumulative = Weight0;
        if (roll < cumulative) return 0;
        cumulative += Weight1;
        if (roll < cumulative) return 1;
        cumulative += Weight2;
        if (roll < cumulative) return 2;
        cumulative += Weight3;
        if (roll < cumulative) return 3;
        return 4;
    }
}
