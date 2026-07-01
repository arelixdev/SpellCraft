using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Classe de base pour toutes les activités de la zone.
/// Le joueur entre dans le trigger → appuie sur Espace → OnInteract est appelé.
/// L'ActivityManager assigne Definition après l'instantiation.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class ActivityBase : MonoBehaviour
{
    [HideInInspector]
    public ActivitySO Definition;

    [FoldoutGroup("État"), ShowInInspector, ReadOnly]
    public bool IsCompleted { get; private set; }

    public event Action OnCompleted;

    private SpellCaster _nearbyPlayer;

    protected virtual void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsCompleted || _nearbyPlayer != null) return;
        var caster = other.GetComponent<SpellCaster>()
                  ?? other.GetComponentInParent<SpellCaster>();
        if (caster == null) return;
        _nearbyPlayer = caster;
    }

    private void OnTriggerExit(Collider other)
    {
        var caster = other.GetComponent<SpellCaster>()
                  ?? other.GetComponentInParent<SpellCaster>();
        if (caster == _nearbyPlayer) _nearbyPlayer = null;
    }

    private void Update()
    {
        if (IsCompleted || _nearbyPlayer == null) return;
        if (Keyboard.current?.spaceKey.wasPressedThisFrame == true)
            OnInteract(_nearbyPlayer);
    }

    protected virtual void OnInteract(SpellCaster caster) { }

    protected void Complete()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        _nearbyPlayer = null;
        OnCompleted?.Invoke();
        Debug.Log($"[Activity] '{Definition?.DisplayName ?? name}' terminée.");
    }
}
