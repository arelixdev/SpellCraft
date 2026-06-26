using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpellProjectile : MonoBehaviour
{
    private SpellContext _ctx;
    private int          _pierceHitsRemaining;
    private float        _lifetimeRemaining;
    private Collider     _spawnIgnoreCollider;

    private bool _lifetimeExpired;

    // Parallel lists to track OnTick timers without a struct-key dictionary
    private List<SpellContext.PendingTrigger> _tickTriggers = new();
    private List<float>                       _tickTimers   = new();

    private void Awake() => enabled = false;

    public void Initialize(SpellContext ctx)
    {
        _ctx                  = ctx;
        _pierceHitsRemaining  = ctx.PierceCount;
        _lifetimeRemaining    = ctx.Lifetime;
        _spawnIgnoreCollider  = ctx.IgnoreOnSpawn;
        enabled               = true;
        transform.localScale  = Vector3.one * ctx.Size;

        foreach (var trigger in ctx.PendingTriggers)
        {
            if (trigger.Type != TriggerType.OnTick) continue;
            _tickTriggers.Add(trigger);
            _tickTimers.Add(trigger.TickInterval);
        }
    }

    private void Update()
    {
        transform.position += _ctx.Direction * _ctx.Speed * Time.deltaTime;
        TickTriggers();

        _lifetimeRemaining -= Time.deltaTime;
        if (_lifetimeRemaining <= 0f)
        {
            _lifetimeExpired = true;
            Destroy(gameObject);
        }
    }

    private void TickTriggers()
    {
        for (int i = 0; i < _tickTriggers.Count; i++)
        {
            _tickTimers[i] -= Time.deltaTime;
            if (_tickTimers[i] > 0) continue;
            _tickTimers[i] = _tickTriggers[i].TickInterval;
            FireTrigger(_tickTriggers[i], null);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pickup")) return;
        if (other == _spawnIgnoreCollider) return;
        if (other.TryGetComponent<SpellProjectile>(out _)) return;

        if (other.CompareTag("Enemy"))
        {
            bool  isCrit      = Random.value < _ctx.CritChance;
            float finalDamage = isCrit ? _ctx.Damage * _ctx.CritMultiplier : _ctx.Damage;
            string critLabel  = isCrit ? " [CRIT]" : "";
            Debug.Log($"[SpellProjectile] Hit '{other.name}' → {finalDamage} dmg ({_ctx.Element}){critLabel}");
            var enemyHealth = other.GetComponent<EnemyHealth>();
            enemyHealth?.TakeDamage(finalDamage, _ctx.Element, isCrit);

            if (_ctx.Element == ElementType.Fire && Random.value < _ctx.StatusChance)
                BurnStatus.Apply(other.gameObject, _ctx.FireTickDamage, _ctx.FireTickInterval, _ctx.StatusDuration);

            if (_ctx.Element == ElementType.Ice && Random.value < _ctx.StatusChance)
                SlowStatus.Apply(other.gameObject, _ctx.IceSlowPercent, _ctx.StatusDuration);

            if (_ctx.Element == ElementType.Poison && Random.value < _ctx.StatusChance)
                PoisonStatus.Apply(other.gameObject, _ctx.PoisonTickDamage, _ctx.PoisonTickInterval, _ctx.StatusDuration);

            if (_ctx.Element == ElementType.Lightning)
                LightningChain.Apply(other.gameObject, _ctx.LightningChainDamage, _ctx.LightningChainRange, _ctx.LightningChainCount);

            FireTriggers(TriggerType.OnHit, other.gameObject);

            if (enemyHealth != null && enemyHealth.IsDead)
                FireTriggers(TriggerType.OnKill, other.gameObject);

            if (_pierceHitsRemaining > 0)
            {
                _pierceHitsRemaining--;
                if (_ctx.ReduceDamageOnPierce)
                    _ctx.Damage *= 1f - _ctx.DamageReductionPerHit / 100f;
            }
            else
                Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_ctx == null || !_lifetimeExpired) return;
        FireTriggers(TriggerType.OnExpire, null);
    }

    private void FireTriggers(TriggerType type, GameObject target)
    {
        foreach (var trigger in _ctx.PendingTriggers)
        {
            if (trigger.Type == type)
                FireTrigger(trigger, target);
        }
    }

    private void FireTrigger(SpellContext.PendingTrigger trigger, GameObject target)
    {
        if (_ctx.Generation >= SpellContext.MaxGeneration) return;

        Vector3 origin = trigger.SpawnSource switch
        {
            TriggerSpawnSource.Target => target != null ? target.transform.position : transform.position,
            TriggerSpawnSource.Caster => _ctx.Caster != null ? _ctx.Caster.transform.position : transform.position,
            _                         => transform.position,
        };

        Vector3 toTarget = origin - (_ctx.Caster != null ? _ctx.Caster.transform.position : origin);
        Vector3 direction = trigger.DirectionMode switch
        {
            TriggerDirectionMode.AwayFromCaster => toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : _ctx.Direction,
            TriggerDirectionMode.TowardCaster   => toTarget.sqrMagnitude > 0.001f ? -toTarget.normalized : _ctx.Direction,
            TriggerDirectionMode.Random         => new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f)).normalized,
            _                                   => _ctx.Direction,
        };

        if (trigger.SpawnSource == TriggerSpawnSource.Target && trigger.SpawnOffset > 0f)
            origin += direction * trigger.SpawnOffset;

        var newCtx = new SpellContext
        {
            Caster           = _ctx.Caster,
            Origin           = origin,
            Direction        = direction,
            Generation       = _ctx.Generation + 1,
            Damage           = _ctx.Damage,
            Size             = _ctx.Size,
            Speed            = _ctx.Speed,
            Element          = _ctx.Element,
            OverrideMaterial = _ctx.OverrideMaterial,
            IgnoreOnSpawn    = target?.GetComponent<Collider>(),
        };

        SpellExecutor.ExecuteFrom(trigger.Graph, trigger.OutputIndices, newCtx);
    }
}
