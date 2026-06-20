using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OrbitalProjectile : MonoBehaviour
{
    private SpellContext _ctx;
    private float        _currentAngle;
    private float        _lifetimeRemaining;

    private void Awake() => enabled = false;

    public void Initialize(SpellContext ctx, float baseAngle)
    {
        _ctx               = ctx;
        _currentAngle      = baseAngle;
        _lifetimeRemaining = ctx.OrbitalLifetime;
        enabled            = true;
        transform.localScale = Vector3.one * ctx.Size;
    }

    // Called by OrbitalManager when a permanent orbital is re-cast with same count.
    public void UpdateContext(SpellContext ctx)
    {
        _ctx = ctx;
        transform.localScale = Vector3.one * ctx.Size;
        if (ctx.OverrideMaterial != null)
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.material = ctx.OverrideMaterial;
    }

    private void Update()
    {
        if (_ctx.Caster == null)
        {
            Destroy(gameObject);
            return;
        }

        _currentAngle += _ctx.OrbitalSpeed * Time.deltaTime;

        Vector3 offset = Quaternion.AngleAxis(_currentAngle, Vector3.up) * Vector3.forward * _ctx.OrbitalRadius;
        transform.position = _ctx.Caster.transform.position + offset;

        if (!_ctx.OrbitalPermanent)
        {
            _lifetimeRemaining -= Time.deltaTime;
            if (_lifetimeRemaining <= 0f)
                Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;

        bool   isCrit      = Random.value < _ctx.CritChance;
        float  finalDamage = isCrit ? _ctx.Damage * _ctx.CritMultiplier : _ctx.Damage;
        string critLabel   = isCrit ? " [CRIT]" : "";
        Debug.Log($"[OrbitalProjectile] Hit '{other.name}' → {finalDamage} dmg ({_ctx.Element}){critLabel}");
        other.GetComponent<EnemyHealth>()?.TakeDamage(finalDamage, _ctx.Element, isCrit);
    }
}
