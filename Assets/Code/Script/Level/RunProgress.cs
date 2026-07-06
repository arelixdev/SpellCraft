/// <summary>
/// Survit au rechargement de scène (contrairement à RunController, qui est recréé à chaque scène) :
/// mémorise le niveau atteint pour scaler la difficulté du prochain run.
/// </summary>
public static class RunProgress
{
    public static int Level = 1;
}
