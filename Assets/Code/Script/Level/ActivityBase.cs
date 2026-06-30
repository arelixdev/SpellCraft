using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Classe de base pour toutes les activités de la zone.
/// Hériter et surcharger pour créer : vague d'ennemis, shop, mystère, etc.
/// L'ActivityManager assigne Definition après l'instantiation.
/// </summary>
public abstract class ActivityBase : MonoBehaviour
{
    [HideInInspector]
    public ActivitySO Definition;

    [FoldoutGroup("État"), ShowInInspector, ReadOnly]
    public bool IsCompleted { get; private set; }

    public event Action OnCompleted;

    protected void Complete()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        OnCompleted?.Invoke();
        Debug.Log($"[Activity] '{Definition?.DisplayName ?? name}' terminée.");
    }
}
