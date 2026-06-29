using System.Collections;
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
        EmitterNodeSO emitterNode = null;
        TraversePreSpawn(graph, startIdx, ctx, ref emitterNode);
        Spawn(emitterNode, ctx);
    }

    // Re-traverses graph and refreshes OrbitalManager without spawning any other emitter type.
    // Safe to call on graph change — won't duplicate projectiles or trigger side-effects.
    public static void RefreshPermanentOrbitals(SpellGraphSO graph, int startNodeIndex, SpellContext ctx)
    {
        if (graph == null || graph.nodes.Count == 0) return;
        int startIdx = Mathf.Clamp(startNodeIndex, 0, graph.nodes.Count - 1);
        EmitterNodeSO emitterNode = null;
        TraversePreSpawn(graph, startIdx, ctx, ref emitterNode);

        if (ctx.Emitter != EmitterType.Orbital || !ctx.OrbitalPermanent) return;
        if (emitterNode != null) SpawnOrbital(emitterNode, ctx);
    }

    // Called by SpellProjectile when a trigger fires
    public static void ExecuteFrom(SpellGraphSO graph, List<int> startIndices, SpellContext ctx)
    {
        EmitterNodeSO emitterNode = null;
        foreach (var idx in startIndices)
            TraversePreSpawn(graph, idx, ctx, ref emitterNode);
        Spawn(emitterNode, ctx);
    }

    // Walks the graph and builds up ctx. Captures the EmitterNodeSO on the traversed path.
    // Stops at Trigger nodes (they are deferred to runtime).
    private static void TraversePreSpawn(SpellGraphSO graph, int idx, SpellContext ctx, ref EmitterNodeSO emitterNode)
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

        if (node is ElementNodeSO arcane && arcane.element == ElementType.Arcane)
            ctx.ArcaneNodeCount = graph.CountDownstreamNodes(idx);

        if (node is ConditionNodeSO condition)
            EvaluateCondition(condition, ctx);
        else
            node.Execute(ctx);

        if (node is EmitterNodeSO em)
            emitterNode = em;

        node.corruptedEffect?.ApplyTo(ctx);

        foreach (var outputIdx in graph.GetOutputIndices(idx))
            TraversePreSpawn(graph, outputIdx, ctx, ref emitterNode);
    }

    private static void RegisterTrigger(SpellGraphSO graph, int idx, TriggerNodeSO trigger, SpellContext ctx)
    {
        ctx.PendingTriggers.Add(new SpellContext.PendingTrigger
        {
            Type           = trigger.triggerType,
            Graph          = graph,
            OutputIndices  = graph.GetOutputIndices(idx),
            TickInterval   = trigger.tickInterval,
            SpawnSource    = trigger.spawnSource,
            DirectionMode  = trigger.directionMode,
            SpawnOffset    = trigger.spawnOffset,
            StatusFilter   = trigger.statusFilter,
            ComboThreshold = trigger.comboThreshold,
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

    private static void Spawn(EmitterNodeSO emitterNode, SpellContext ctx)
    {
        switch (ctx.Emitter)
        {
            case EmitterType.Projectile:
                if (emitterNode != null) SpawnProjectile(emitterNode, ctx);
                break;
            case EmitterType.Grenade:
                if (emitterNode != null) SpawnGrenade(emitterNode, ctx);
                break;
            case EmitterType.Orbital:
                if (emitterNode != null) SpawnOrbital(emitterNode, ctx);
                break;
            case EmitterType.Zone:
                if (emitterNode != null) SpawnZone(emitterNode, ctx);
                break;
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

    private static void SpawnZone(EmitterNodeSO emitter, SpellContext ctx)
    {
        if (emitter.projectilePrefab == null)
        {
            Debug.LogWarning($"[SpellExecutor] Zone '{emitter.nodeName}' has no prefab assigned.");
            return;
        }

        var zoneCtx = new SpellContext
        {
            Caster              = ctx.Caster,
            Origin              = ctx.Origin,
            Direction           = ctx.Direction,
            Damage              = ctx.Damage,
            Size                = ctx.Size,
            CritChance          = ctx.CritChance,
            CritMultiplier      = ctx.CritMultiplier,
            Element             = ctx.Element,
            Emitter             = ctx.Emitter,
            OverrideMaterial    = ctx.OverrideMaterial,
            ZoneType            = ctx.ZoneType,
            ZoneDamageMode      = ctx.ZoneDamageMode,
            ZoneRadius          = ctx.ZoneRadius,
            ZoneGrowDuration    = ctx.ZoneGrowDuration,
            ZoneDuration        = ctx.ZoneDuration,
            ZoneTickInterval    = ctx.ZoneTickInterval,
            Behaviors           = new List<BehaviorType>(ctx.Behaviors),
            PendingTriggers     = new List<SpellContext.PendingTrigger>(ctx.PendingTriggers),
            ActiveEffects       = new List<EffectNodeSO>(ctx.ActiveEffects),
            EmitterModifiers    = new List<EmitterModifierNodeSO>(ctx.EmitterModifiers),
            ConditionMultiplier = ctx.ConditionMultiplier,
            Generation          = ctx.Generation,
        };

        if (ctx.ZoneType == ZoneType.StaticOnPlayer)
        {
            var manager = ctx.Caster.GetComponent<SpellZoneManager>()
                       ?? ctx.Caster.AddComponent<SpellZoneManager>();
            manager.Register(emitter.projectilePrefab, zoneCtx);
            return;
        }

        var go = Object.Instantiate(emitter.projectilePrefab, ctx.Origin, Quaternion.identity);

        if (ctx.OverrideMaterial != null)
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                r.material = ctx.OverrideMaterial;

        go.AddComponent<ZoneEffect>().Initialize(zoneCtx);
    }

    private const float BurstDelay = 0.15f;

    private static void SpawnProjectile(EmitterNodeSO emitter, SpellContext ctx)
    {
        if (emitter.projectilePrefab == null)
        {
            Debug.LogWarning($"[SpellExecutor] Emitter '{emitter.nodeName}' has no prefab assigned.");
            return;
        }

        int burst = Mathf.Max(1, ctx.BurstCount);
        if (burst <= 1)
        {
            SpawnProjectileVolley(emitter, ctx);
            return;
        }

        var mb = ctx.Caster.GetComponent<MonoBehaviour>();
        if (mb != null)
            mb.StartCoroutine(BurstCoroutine(emitter, ctx, burst));
        else
            SpawnProjectileVolley(emitter, ctx);
    }

    private static IEnumerator BurstCoroutine(EmitterNodeSO emitter, SpellContext ctx, int burst)
    {
        for (int b = 0; b < burst; b++)
        {
            SpawnProjectileVolley(emitter, ctx);
            if (b < burst - 1)
                yield return new WaitForSeconds(BurstDelay);
        }
    }

    private static void SpawnProjectileVolley(EmitterNodeSO emitter, SpellContext ctx)
    {
        int   count  = Mathf.Max(1, emitter.projectileCount);
        float spread = emitter.spreadAngle;

        for (int i = 0; i < count; i++)
        {
            float   angle = count == 1 ? 0f : -spread / 2f + i * (spread / (count - 1));
            Vector3 dir   = Quaternion.AngleAxis(angle, Vector3.up) * ctx.Direction;

            var rotation = dir != Vector3.zero ? Quaternion.LookRotation(dir) : Quaternion.identity;
            var go       = Object.Instantiate(emitter.projectilePrefab, ctx.Origin, rotation);

            if (ctx.OverrideMaterial != null)
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                    r.material = ctx.OverrideMaterial;

            var projCtx = new SpellContext
            {
                Caster                = ctx.Caster,
                Origin                = ctx.Origin,
                Direction             = dir,
                Damage                = ctx.Damage,
                Size                  = ctx.Size,
                Speed                 = ctx.Speed,
                CritChance            = ctx.CritChance,
                CritMultiplier        = ctx.CritMultiplier,
                Element               = ctx.Element,
                Emitter               = ctx.Emitter,
                OverrideMaterial      = ctx.OverrideMaterial,
                Lifetime              = ctx.Lifetime,
                PierceCount           = ctx.PierceCount,
                ReduceDamageOnPierce  = ctx.ReduceDamageOnPierce,
                DamageReductionPerHit = ctx.DamageReductionPerHit,
                StatusChance          = ctx.StatusChance,
                StatusDuration        = ctx.StatusDuration,
                FireTickInterval      = ctx.FireTickInterval,
                FireTickDamage        = ctx.FireTickDamage,
                IceSlowPercent        = ctx.IceSlowPercent,
                LightningChainRange   = ctx.LightningChainRange,
                LightningChainDamage  = ctx.LightningChainDamage,
                LightningChainCount   = ctx.LightningChainCount,
                Behaviors             = new List<BehaviorType>(ctx.Behaviors),
                PendingTriggers       = new List<SpellContext.PendingTrigger>(ctx.PendingTriggers),
                ActiveEffects         = new List<EffectNodeSO>(ctx.ActiveEffects),
                EmitterModifiers      = new List<EmitterModifierNodeSO>(ctx.EmitterModifiers),
                ConditionMultiplier   = ctx.ConditionMultiplier,
                Generation            = ctx.Generation,
            };

            go.AddComponent<SpellProjectile>().Initialize(projCtx);
        }
    }

    private static void SpawnGrenade(EmitterNodeSO emitter, SpellContext ctx)
    {
        if (emitter.projectilePrefab == null)
        {
            Debug.LogWarning($"[SpellExecutor] Grenade '{emitter.nodeName}' has no prefab assigned.");
            return;
        }

        int   count  = Mathf.Max(1, emitter.projectileCount);
        float spread = emitter.spreadAngle;

        for (int i = 0; i < count; i++)
        {
            float   angle = count == 1 ? 0f : -spread / 2f + i * (spread / (count - 1));
            Vector3 dir   = Quaternion.AngleAxis(angle, Vector3.up) * ctx.Direction;

            var go = Object.Instantiate(emitter.projectilePrefab, ctx.Origin, Quaternion.identity);

            if (ctx.OverrideMaterial != null)
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                    r.material = ctx.OverrideMaterial;

            var projCtx = new SpellContext
            {
                Caster           = ctx.Caster,
                Origin           = ctx.Origin,
                Direction        = dir,
                Damage           = ctx.Damage,
                Size             = ctx.Size,
                Speed            = ctx.Speed,
                CritChance       = ctx.CritChance,
                CritMultiplier   = ctx.CritMultiplier,
                Element          = ctx.Element,
                Lifetime         = ctx.Lifetime,
                OverrideMaterial = ctx.OverrideMaterial,
                Generation       = ctx.Generation,
            };

            go.AddComponent<GrenadeProjectile>().Initialize(
                projCtx,
                emitter.bounceCount,
                emitter.launchAngle,
                emitter.gravityMultiplier,
                emitter.explosionRadius,
                emitter.explosionDamageMultiplier,
                emitter.explosionPrefab
            );
        }
    }

    private static void SpawnOrbital(EmitterNodeSO emitter, SpellContext ctx)
    {
        if (emitter.projectilePrefab == null)
        {
            Debug.LogWarning($"[SpellExecutor] Orbital '{emitter.nodeName}' has no prefab assigned.");
            return;
        }

        int count = Mathf.Max(1, emitter.orbitalCount);

        var projCtx = new SpellContext
        {
            Caster              = ctx.Caster,
            Origin              = ctx.Origin,
            Direction           = ctx.Direction,
            Damage              = ctx.Damage,
            Size                = ctx.Size,
            Speed               = ctx.Speed,
            CritChance          = ctx.CritChance,
            CritMultiplier      = ctx.CritMultiplier,
            Element             = ctx.Element,
            Emitter             = ctx.Emitter,
            OverrideMaterial    = ctx.OverrideMaterial,
            OrbitalRadius       = ctx.OrbitalRadius,
            OrbitalSpeed        = ctx.OrbitalSpeed,
            OrbitalPermanent    = ctx.OrbitalPermanent,
            OrbitalLifetime     = ctx.OrbitalLifetime,
            Behaviors           = new List<BehaviorType>(ctx.Behaviors),
            PendingTriggers     = new List<SpellContext.PendingTrigger>(ctx.PendingTriggers),
            ActiveEffects       = new List<EffectNodeSO>(ctx.ActiveEffects),
            EmitterModifiers    = new List<EmitterModifierNodeSO>(ctx.EmitterModifiers),
            ConditionMultiplier = ctx.ConditionMultiplier,
            Generation          = ctx.Generation,
        };

        if (ctx.OrbitalPermanent)
        {
            var manager = ctx.Caster.GetComponent<OrbitalManager>()
                       ?? ctx.Caster.AddComponent<OrbitalManager>();
            manager.Register(emitter.projectilePrefab, projCtx, count);
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                float baseAngle = 360f / count * i;
                var   go        = Object.Instantiate(emitter.projectilePrefab, ctx.Origin, Quaternion.identity);

                if (ctx.OverrideMaterial != null)
                    foreach (var r in go.GetComponentsInChildren<Renderer>())
                        r.material = ctx.OverrideMaterial;

                go.AddComponent<OrbitalProjectile>().Initialize(projCtx, baseAngle);
            }
        }
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
