using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpellProjectile : MonoBehaviour
{
    private SpellContext _ctx;
    private int          _pierceHitsRemaining;
    private float        _lifetimeRemaining;
    private Collider     _spawnIgnoreCollider;
    private bool         _lifetimeExpired;

    // Emitter modifier state
    private float   _initialSpeed;
    private float   _initialSize;
    private int     _bouncesRemaining;
    private Vector3 _lastPosition;

    private readonly Collider[] _homingBuffer = new Collider[16];

    // Parallel lists to track OnTick timers without a struct-key dictionary
    private List<SpellContext.PendingTrigger> _tickTriggers = new();
    private List<float>                       _tickTimers   = new();

    private void Awake() => enabled = false;

    public void Initialize(SpellContext ctx)
    {
        _ctx                 = ctx;
        _pierceHitsRemaining = ctx.PierceCount;
        _lifetimeRemaining   = ctx.Lifetime;
        _spawnIgnoreCollider = ctx.IgnoreOnSpawn;
        _initialSpeed        = ctx.Speed;
        _initialSize         = ctx.Size;
        _lastPosition        = ctx.Origin;
        enabled              = true;
        transform.localScale = Vector3.one * ctx.Size;

        foreach (var mod in ctx.EmitterModifiers)
        {
            if (mod.modifierType == EmitterModifierType.Bounce)
                _bouncesRemaining = mod.bounceCount;
        }

        foreach (var trigger in ctx.PendingTriggers)
        {
            if (trigger.Type == TriggerType.OnTick)
            {
                _tickTriggers.Add(trigger);
                _tickTimers.Add(trigger.TickInterval);
            }
        }
    }

    private void Update()
    {
        _lastPosition = transform.position;

        float elapsed = _ctx.Lifetime - _lifetimeRemaining;
        ApplyModifiers(elapsed);

        transform.position += _ctx.Direction * _ctx.Speed * Time.deltaTime;
        TickTriggers();

        _lifetimeRemaining -= Time.deltaTime;
        if (_lifetimeRemaining <= 0f)
        {
            _lifetimeExpired = true;
            Destroy(gameObject);
        }
    }

    private void ApplyModifiers(float elapsed)
    {
        foreach (var mod in _ctx.EmitterModifiers)
        {
            switch (mod.modifierType)
            {
                case EmitterModifierType.Homecoming:   ApplyHomecoming(mod);            break;
                case EmitterModifierType.Scale:        ApplyScale(mod);                 break;
                case EmitterModifierType.Back:         ApplyBack(mod, elapsed);         break;
                case EmitterModifierType.Acceleration: ApplyAcceleration(mod, elapsed); break;
            }
        }
    }

    private void ApplyHomecoming(EmitterModifierNodeSO mod)
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, mod.homingRadius, _homingBuffer);
        Transform nearest = null;
        float nearestSqDist = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (!_homingBuffer[i].CompareTag("Enemy")) continue;
            float sqDist = (_homingBuffer[i].transform.position - transform.position).sqrMagnitude;
            if (sqDist >= nearestSqDist) continue;
            nearestSqDist = sqDist;
            nearest = _homingBuffer[i].transform;
        }
        if (nearest == null) return;
        Vector3 toTarget = (nearest.position - transform.position).normalized;
        _ctx.Direction = Vector3.RotateTowards(_ctx.Direction, toTarget, mod.homingStrength * Time.deltaTime, 0f);
    }

    private void ApplyScale(EmitterModifierNodeSO mod)
    {
        float current = transform.localScale.x;
        float target  = _initialSize * mod.maxScaleMultiplier;
        transform.localScale = Vector3.one * Mathf.MoveTowards(current, target, mod.scaleRate * Time.deltaTime);
    }

    private void ApplyBack(EmitterModifierNodeSO mod, float elapsed)
    {
        if (elapsed < mod.backDelay || _ctx.Caster == null) return;
        Vector3 toCaster = (_ctx.Caster.transform.position - transform.position).normalized;
        float maxRad = mod.returnSteerSpeed * Mathf.Deg2Rad * Time.deltaTime;
        _ctx.Direction = Vector3.RotateTowards(_ctx.Direction, toCaster, maxRad, 0f);
    }

    private void ApplyAcceleration(EmitterModifierNodeSO mod, float elapsed)
    {
        float newSpeed = _initialSpeed + mod.accelerationRate * elapsed;
        _ctx.Speed = Mathf.Min(newSpeed, _initialSpeed * mod.maxSpeedMultiplier);
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
        if (other.TryGetComponent<EnemyProjectile>(out _)) return;
        if (other.GetComponentInParent<ActivityBase>() != null) return;

        if (other.CompareTag("Enemy"))
        {
            bool  isCrit      = Random.value < _ctx.CritChance;
            float finalDamage = isCrit ? _ctx.Damage * _ctx.CritMultiplier : _ctx.Damage;
            string critLabel  = isCrit ? " [CRIT]" : "";
            Debug.Log($"[SpellProjectile] Hit '{other.name}' → {finalDamage} dmg ({_ctx.Element}){critLabel}");
            var enemyHealth = other.GetComponent<EnemyHealth>();
            enemyHealth?.TakeDamage(finalDamage, _ctx.Element, isCrit);

            FireStatusTriggers(other.gameObject);

            if (_ctx.Element == ElementType.Fire && Random.value < _ctx.StatusChance)
                BurnStatus.Apply(other.gameObject, _ctx.FireTickDamage, _ctx.FireTickInterval, _ctx.StatusDuration);

            if (_ctx.Element == ElementType.Ice && Random.value < _ctx.StatusChance)
                IceStatus.Apply(other.gameObject, _ctx.IceSlowPercent, _ctx.StatusDuration);

            if (_ctx.Element == ElementType.Poison && Random.value < _ctx.StatusChance)
                PoisonStatus.Apply(other.gameObject, _ctx.PoisonTickDamage, _ctx.PoisonTickInterval, _ctx.StatusDuration);

            if (_ctx.Element == ElementType.Lightning)
                LightningChain.Apply(other.gameObject, _ctx.LightningChainDamage, _ctx.LightningChainRange, _ctx.LightningChainCount);

            FireTriggers(TriggerType.OnHit, other.gameObject);
            if (isCrit) FireTriggers(TriggerType.OnCrit, other.gameObject);

            FireComboTriggers(other.gameObject);

            if (enemyHealth != null && enemyHealth.IsDead)
                FireTriggers(TriggerType.OnKill, other.gameObject);

            // Bounce off enemy takes priority over pierce
            if (_bouncesRemaining > 0 && HasBounceOnEnemies())
                BounceOffEnemy(other.transform);
            else if (_pierceHitsRemaining > 0)
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
            if (_bouncesRemaining > 0)
                BounceOffWall();
            else
                Destroy(gameObject);
        }
    }

    private bool HasBounceOnEnemies()
    {
        foreach (var mod in _ctx.EmitterModifiers)
            if (mod.modifierType == EmitterModifierType.Bounce && mod.bounceOnEnemies)
                return true;
        return false;
    }

    private void BounceOffEnemy(Transform enemy)
    {
        Vector3 normal = transform.position - enemy.position;
        normal.y = 0f;
        if (normal.sqrMagnitude < 0.001f) normal = -_ctx.Direction;
        else normal.Normalize();
        _ctx.Direction = Vector3.Reflect(_ctx.Direction, normal).normalized;
        _bouncesRemaining--;
    }

    private void BounceOffWall()
    {
        Vector3 moveDir  = transform.position - _lastPosition;
        float   moveDist = moveDir.magnitude;

        if (moveDist > 0.001f)
        {
            moveDir /= moveDist;
            if (Physics.Raycast(_lastPosition, moveDir, out RaycastHit hit, moveDist + 0.5f))
                _ctx.Direction = Vector3.Reflect(_ctx.Direction, hit.normal).normalized;
            else
                _ctx.Direction = new Vector3(-_ctx.Direction.x, _ctx.Direction.y, -_ctx.Direction.z).normalized;
        }

        _bouncesRemaining--;
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

    private void FireStatusTriggers(GameObject target)
    {
        foreach (var trigger in _ctx.PendingTriggers)
        {
            if (trigger.Type != TriggerType.OnStatus) continue;
            bool hasStatus = trigger.StatusFilter switch
            {
                ElementType.Fire   => target.TryGetComponent<BurnStatus>(out _),
                ElementType.Ice    => target.TryGetComponent<IceStatus>(out _),
                ElementType.Poison => target.TryGetComponent<PoisonStatus>(out _),
                _                  => false,
            };
            if (hasStatus) FireTrigger(trigger, target);
        }
    }

    private void FireComboTriggers(GameObject enemy)
    {
        if (_ctx.Caster != null)
        {
            var state = _ctx.Caster.GetComponent<CasterComboState>() ?? _ctx.Caster.AddComponent<CasterComboState>();
            if (state.LastHitEnemy != null && state.LastHitEnemy != enemy)
                state.LastHitEnemy.GetComponent<ComboHitTracker>()?.ResetAll();
            state.LastHitEnemy = enemy;
        }

        var tracker = enemy.GetComponent<ComboHitTracker>() ?? enemy.AddComponent<ComboHitTracker>();
        foreach (var trigger in _ctx.PendingTriggers)
        {
            if (trigger.Type != TriggerType.OnCombo) continue;
            if (tracker.RegisterHit(trigger.ComboThreshold))
                FireTrigger(trigger, enemy);
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
