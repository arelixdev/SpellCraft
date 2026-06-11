using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpellProjectile : MonoBehaviour
{
    private SpellContext _ctx;

    // Parallel lists to track OnTick timers without a struct-key dictionary
    private List<SpellContext.PendingTrigger> _tickTriggers = new();
    private List<float>                       _tickTimers   = new();

    public void Initialize(SpellContext ctx)
    {
        _ctx = ctx;
        transform.localScale = Vector3.one * ctx.Size;

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
        if (!other.CompareTag("Enemy")) return;

        // TODO: apply damage via Health component when available
        // other.GetComponent<Health>()?.TakeDamage(_ctx.Damage, _ctx.Element);

        FireTriggers(TriggerType.OnHit, other.gameObject);

        if (!_ctx.Behaviors.Contains(BehaviorType.Pierce))
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
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

        var newCtx = new SpellContext
        {
            Caster     = _ctx.Caster,
            Origin     = transform.position,
            Direction  = _ctx.Direction,
            Generation = _ctx.Generation + 1,
            Damage     = _ctx.Damage,
            Size       = _ctx.Size,
            Speed      = _ctx.Speed,
            Element    = _ctx.Element,
        };

        SpellExecutor.ExecuteFrom(trigger.Graph, trigger.OutputIndices, newCtx);
    }
}
