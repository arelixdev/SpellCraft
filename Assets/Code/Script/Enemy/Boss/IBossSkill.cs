using System.Collections;
using UnityEngine;

/// <summary>
/// Contrat d'une compétence de boss, orchestrée par BossController.
/// Le composant ne gère ni son propre cooldown ni l'exclusivité : BossController
/// choisit un skill parmi ceux "à portée" et attend la fin de son Execute() avant d'en relancer un autre.
/// </summary>
public interface IBossSkill
{
    bool IsInRange(Transform player);
    IEnumerator Execute();
}
