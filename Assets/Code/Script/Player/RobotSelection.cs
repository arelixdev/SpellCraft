/// <summary>
/// Survit au chargement de scène (contrairement aux objets de la scène Gameplay) :
/// mémorise le robot choisi sur l'écran de sélection pour que RobotLoader puisse
/// l'appliquer au spawn dans Gameplay. Même idiome que RunProgress.Level.
/// </summary>
public static class RobotSelection
{
    public static RobotDefinitionSO Chosen;
}
