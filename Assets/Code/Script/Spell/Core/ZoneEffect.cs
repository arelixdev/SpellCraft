using System.Collections.Generic;
using UnityEngine;

public class ZoneEffect : MonoBehaviour
{
    private SpellContext _ctx;
    private float        _elapsed;
    private float        _tickTimer;
    private float        _currentTickInterval;
    private float        _currentRadius;
    private float        _meshExtent = 0.5f;

    private const float GroundOffset  = -1f;
    private const float ZoneHeight    =  3f;
    private const int   OverlapBuffer = 32;

    private readonly Collider[]         _overlapResults = new Collider[OverlapBuffer];
    private readonly HashSet<EnemyHealth> _enteredEnemies = new();

    private void Awake() => enabled = false;

    public void Initialize(SpellContext ctx)
    {
        _ctx = ctx;

        var mf = GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            var ext = mf.sharedMesh.bounds.extents;
            float detected = Mathf.Max(ext.x, ext.z);
            if (detected > 0f) _meshExtent = detected;
        }

        var pos = transform.position;
        pos.y              = pos.y + GroundOffset;
        transform.position = pos;

        float startRadius = ctx.ZoneType == ZoneType.StaticOnPlayer ? ctx.ZoneRadius : 0f;
        SetRadius(startRadius);

        _currentTickInterval = ctx.ZoneTickInterval;
        _tickTimer           = ctx.ZoneTickInterval;
        enabled              = true;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        UpdateAcceleration();

        if (_ctx.ZoneType != ZoneType.GrowingOnGround && _ctx.Caster != null)
        {
            var pos = _ctx.Caster.transform.position;
            pos.y              = pos.y + GroundOffset;
            transform.position = pos;
        }

        if (_ctx.ZoneType != ZoneType.StaticOnPlayer)
        {
            float t = _ctx.ZoneGrowDuration > 0f
                ? Mathf.Clamp01(_elapsed / _ctx.ZoneGrowDuration)
                : 1f;
            SetRadius(Mathf.Lerp(0f, _ctx.ZoneRadius, t));
        }

        if (_ctx.ZoneDamageMode == ZoneDamageMode.OnEnter || _ctx.ZoneDamageMode == ZoneDamageMode.Both)
            CheckOnEnter();

        if (_ctx.ZoneDamageMode == ZoneDamageMode.Tick || _ctx.ZoneDamageMode == ZoneDamageMode.Both)
        {
            _tickTimer -= Time.deltaTime;
            if (_tickTimer <= 0f)
            {
                _tickTimer = _currentTickInterval;
                ApplyTickDamage();
            }
        }

        if (_ctx.ZoneDuration > 0f && _elapsed >= _ctx.ZoneDuration)
            Destroy(gameObject);
    }

    public void UpdateContext(SpellContext ctx)
    {
        _ctx = ctx;
        SetRadius(ctx.ZoneRadius);
        if (ctx.OverrideMaterial != null)
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.material = ctx.OverrideMaterial;
    }

    private void UpdateAcceleration()
    {
        foreach (var mod in _ctx.EmitterModifiers)
        {
            if (mod.modifierType != EmitterModifierType.Acceleration) continue;
            float minInterval = _ctx.ZoneTickInterval / mod.maxSpeedMultiplier;
            _currentTickInterval = Mathf.Max(
                _ctx.ZoneTickInterval / (1f + mod.accelerationRate * _elapsed),
                minInterval);
            return;
        }
    }

    private void CheckOnEnter()
    {
        Vector3 bottom = transform.position;
        Vector3 top    = bottom + Vector3.up * ZoneHeight;
        int count = Physics.OverlapCapsuleNonAlloc(bottom, top, _currentRadius, _overlapResults);
        for (int i = 0; i < count; i++)
        {
            var health = _overlapResults[i].GetComponentInParent<EnemyHealth>();
            if (health == null || !_enteredEnemies.Add(health)) continue;
            DealDamage(health);
        }
    }

    private void ApplyTickDamage()
    {
        Vector3 bottom = transform.position;
        Vector3 top    = bottom + Vector3.up * ZoneHeight;
        int count    = Physics.OverlapCapsuleNonAlloc(bottom, top, _currentRadius, _overlapResults);
        var hitCache = new HashSet<EnemyHealth>();
        for (int i = 0; i < count; i++)
        {
            var health = _overlapResults[i].GetComponentInParent<EnemyHealth>();
            if (health == null || !hitCache.Add(health)) continue;
            DealDamage(health);
        }
    }

    private void DealDamage(EnemyHealth health)
    {
        bool  isCrit      = Random.value < _ctx.CritChance;
        float finalDamage = isCrit ? _ctx.Damage * _ctx.CritMultiplier : _ctx.Damage;
        Debug.Log($"[ZoneEffect] Hit '{health.name}' → {finalDamage} dmg ({_ctx.Element}){(isCrit ? " [CRIT]" : "")}");
        health.TakeDamage(finalDamage, _ctx.Element, isCrit);
    }

    private void SetRadius(float radius)
    {
        _currentRadius       = radius;
        float scale          = radius / _meshExtent;
        transform.localScale = new Vector3(scale, 1f, scale);
    }
}
