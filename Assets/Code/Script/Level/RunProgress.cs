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

    // Coffres ouverts depuis le début du run (tous niveaux confondus) : sert à faire monter
    // le prix des coffres (ChestActivity.CurrentPrice). Doit survivre au rechargement de
    // scène entre deux niveaux, contrairement à RunController (recréé à chaque scène) — d'où
    // sa place ici plutôt que sur RunController.
    public static int ChestsOpenedThisRun = 0;

    // À appeler au moment de démarrer un tout nouveau run (choix du robot), pour que
    // l'exclusion des niveaux déjà vus ni le prix des coffres ne survivent d'un run à
    // l'autre dans la même session.
    public static void ResetRun()
    {
        Level = 1;
        UsedLevels.Clear();
        ChestsOpenedThisRun = 0;
    }
}
