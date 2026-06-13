using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class SpellExecutor
{
    // Entry point called by SpellCaster
    public static void Execute(SpellGraphSO graph, int startNodeIndex, SpellContext ctx)
    {
        if (graph == null || graph.nodes.Count == 0) return;
        int startIdx = Mathf.Clamp(startNodeIndex, 0, graph.nodes.Count - 1);
        TraversePreSpawn(graph, startIdx, ctx);
        Spawn(graph, ctx);
    }

    // Called by SpellProjectile when a trigger fires
    public static void ExecuteFrom(SpellGraphSO graph, List<int> startIndices, SpellContext ctx)
    {
        foreach (var idx in startIndices)
            TraversePreSpawn(graph, idx, ctx);
        Spawn(graph, ctx);
    }

    // Walks the graph and builds up ctx. Stops at Trigger nodes (they are deferred to runtime).
    private static void TraversePreSpawn(SpellGraphSO graph, int idx, SpellContext ctx)
    {
        if (idx < 0 || idx >= graph.nodes.Count) return;
        var node = graph.nodes[idx];
        if (node == null) return;

        if (node is TriggerNodeSO trigger)
        {
            if (ctx.Generation < SpellContext.MaxGeneration)
                RegisterTrigger(graph, idx, trigger, ctx);
            return; // Stop traversal here — downstream fires at runtime
        }

        if (node is ConditionNodeSO condition)
            EvaluateCondition(condition, ctx);
        else
            node.Execute(ctx);

        foreach (var outputIdx in graph.GetOutputIndices(idx))
            TraversePreSpawn(graph, outputIdx, ctx);
    }

    private static void RegisterTrigger(SpellGraphSO graph, int idx, TriggerNodeSO trigger, SpellContext ctx)
    {
        ctx.PendingTriggers.Add(new SpellContext.PendingTrigger
        {
            Type          = trigger.triggerType,
            Graph         = graph,
            OutputIndices = graph.GetOutputIndices(idx),
            TickInterval  = trigger.tickInterval,
        });
    }

    private static void EvaluateCondition(ConditionNodeSO condition, SpellContext ctx)
    {
        bool met = condition.conditionType switch
        {
            ConditionType.SelfAtFullHP    => IsCasterAtFullHP(ctx.Caster),
            ConditionType.TargetHasStatus => false,            // evaluated per-hit in SpellProjectile
            ConditionType.ComboCount      => GetComboCount(ctx.Caster) >= condition.threshold,
            ConditionType.EnemiesNearby   => CountEnemiesNearby(ctx.Caster, 5f) >= condition.threshold,
            _                             => false,
        };

        if (met) ctx.ConditionMultiplier *= condition.bonusMultiplier;
    }

    private static void Spawn(SpellGraphSO graph, SpellContext ctx)
    {
        switch (ctx.Emitter)
        {
            case EmitterType.Projectile:
                var emitter = graph.nodes.OfType<EmitterNodeSO>().FirstOrDefault();
                if (emitter != null) SpawnProjectile(emitter, ctx);
                break;
            case EmitterType.Zone:
            case EmitterType.Cone:
            case EmitterType.Beam:
            case EmitterType.Self:
                // TODO: implement non-projectile emitter types
                break;
            case EmitterType.None:
                SpawnEffects(ctx);
                break;
        }
    }

    private static void SpawnProjectile(EmitterNodeSO emitter, SpellContext ctx)
    {
        if (emitter.projectilePrefab == null)
        {
            Debug.LogWarning($"[SpellExecutor] Emitter '{emitter.nodeName}' has no prefab assigned.");
            return;
        }

        var rotation = ctx.Direction != Vector3.zero
            ? Quaternion.LookRotation(ctx.Direction)
            : Quaternion.identity;

        var go = Object.Instantiate(emitter.projectilePrefab, ctx.Origin, rotation);
        go.AddComponent<SpellProjectile>().Initialize(ctx);
    }

    // Used when a trigger fires effects directly (no emitter in the sub-path).
    // Uses ctx.ActiveEffects so only effects in the current traversal path are spawned.
    private static void SpawnEffects(SpellContext ctx)
    {
        foreach (var effect in ctx.ActiveEffects)
        {
            if (effect.effectPrefab == null) continue;
            Object.Instantiate(effect.effectPrefab, ctx.Origin, Quaternion.identity);
        }
    }

    // --- Condition helpers (stub until systems exist) ---

    private static bool IsCasterAtFullHP(GameObject caster)
    {
        // TODO: plug into Health component
        return true;
    }

    private static int GetComboCount(GameObject caster)
    {
        // TODO: plug into ComboTracker component
        return 0;
    }

    private static int CountEnemiesNearby(GameObject caster, float radius)
    {
        return Physics.OverlapSphere(caster.transform.position, radius)
            .Count(c => c.CompareTag("Enemy"));
    }
}
