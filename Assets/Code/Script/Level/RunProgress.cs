using System.Collections.Generic;

/// <summary>
/// Survit au rechargement de scène (contrairement à RunController, qui est recréé à chaque scène) :
/// mémorise le niveau atteint pour scaler la difficulté du prochain run, et quels LevelDefinitionSO
/// ont déjà été tirés cette run (pour ne pas retomber dessus avant la fin du run).
/// </summary>
public static class RunProgress
{
    public static int Level = 1;
    public static readonly List<LevelDefinitionSO> UsedLevels = new();

    // À appeler au moment de démarrer un tout nouveau run (choix du robot), pour que
    // l'exclusion des niveaux déjà vus ne survive pas d'un run à l'autre dans la même session.
    public static void ResetRun()
    {
        Level = 1;
        UsedLevels.Clear();
    }
}
