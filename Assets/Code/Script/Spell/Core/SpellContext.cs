using System.Collections.Generic;
using UnityEngine;

public class SpellContext
{
    public struct PendingTrigger
    {
        public TriggerType  Type;
        public SpellGraphSO Graph;
        public List<int>    OutputIndices;
        public float        TickInterval;
    }

    public GameObject Caster;
    public Vector3    Origin;
    public Vector3    Direction;

    public float       Damage          = 10f;
    public float       Size            =  1f;
    public float       Speed           = 10f;
    public float       CritChance      =  0f;   // 0–1, boostable via nodes
    public float       CritMultiplier  =  1.5f; // +50% dmg on crit, boostable via nodes
    public ElementType Element = ElementType.None;
    public EmitterType Emitter;
    public Material    OverrideMaterial;

    public float OrbitalRadius    = 3f;
    public float OrbitalSpeed     = 90f;
    public bool  OrbitalPermanent = true;
    public float OrbitalLifetime  = 5f;

    public float                Lifetime               = 3f;
    public int                  PierceCount            = 0;
    public bool                 ReduceDamageOnPierce   = false;
    public float                DamageReductionPerHit  = 0f;
    public List<BehaviorType>   Behaviors       = new();
    public List<PendingTrigger> PendingTriggers = new();
    public List<EffectNodeSO>   ActiveEffects   = new();

    // Written by Condition nodes, read by downstream nodes to amplify
    public float ConditionMultiplier = 1f;

    // Prevents infinite trigger chains: triggers stop firing past MaxGeneration
    public int       Generation    = 0;
    public const int MaxGeneration = 2;
}
