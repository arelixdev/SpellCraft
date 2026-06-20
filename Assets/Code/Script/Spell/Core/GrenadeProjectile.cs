using UnityEngine;

public class GrenadeProjectile : MonoBehaviour
{
    private SpellContext _ctx;
    private Rigidbody    _rb;
    private int          _bouncesRemaining;
    private float        _explosionRadius;
    private float        _explosionDamageMultiplier;
    private GameObject   _explosionPrefab;
    private float        _lifetimeRemaining;
    private float        _gravityMultiplier;
    private bool         _exploded;

    public void Initialize(SpellContext ctx, int bounceCount, float launchAngle,
                           float gravityMultiplier, float explosionRadius,
                           float explosionDamageMultiplier, GameObject explosionPrefab)
    {
        _ctx                       = ctx;
        _bouncesRemaining          = bounceCount;
        _gravityMultiplier         = gravityMultiplier;
        _explosionRadius           = explosionRadius;
        _explosionDamageMultiplier = explosionDamageMultiplier;
        _explosionPrefab           = explosionPrefab;
        _lifetimeRemaining         = ctx.Lifetime;

        // PhysicMaterial en code : bounciness élevée + friction nulle
        // bounceCombine = Maximum garantit que notre valeur gagne sur celle du sol
        var bounceMat = new PhysicsMaterial("GrenadeBounce")
        {
            bounciness       = 0.75f,
            dynamicFriction  = 0f,
            staticFriction   = 0f,
            frictionCombine  = PhysicsMaterialCombine.Minimum,
            bounceCombine    = PhysicsMaterialCombine.Maximum,
        };

        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.isTrigger      = false;
            col.sharedMaterial = bounceMat;
        }

        _rb = GetComponent<Rigidbody>();
        if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();

        _rb.useGravity             = false;
        _rb.linearDamping                   = 0f;
        _rb.angularDamping            = 0f;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        transform.localScale = Vector3.one * ctx.Size;

        Vector3 flatDir   = new Vector3(ctx.Direction.x, 0f, ctx.Direction.z).normalized;
        float   rad       = launchAngle * Mathf.Deg2Rad;
        Vector3 launchDir = flatDir * Mathf.Cos(rad) + Vector3.up * Mathf.Sin(rad);
        _rb.linearVelocity = launchDir * ctx.Speed;
    }

    private void FixedUpdate()
    {
        _rb.AddForce(Physics.gravity * _gravityMultiplier, ForceMode.Acceleration);
    }

    private void Update()
    {
        _lifetimeRemaining -= Time.deltaTime;
        if (_lifetimeRemaining <= 0f) Explode();
    }

    private void OnCollisionEnter(Collision col)
    {
        if (_exploded) return;
        if (col.gameObject.CompareTag("Pickup")) return;

        if (col.gameObject.CompareTag("Enemy"))
        {
            Explode();
            return;
        }

        // Le PhysicMaterial gère le rebond, on ne fait que compter
        _bouncesRemaining--;
        if (_bouncesRemaining <= 0) Explode();
    }

    private void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        if (_explosionPrefab != null)
            Instantiate(_explosionPrefab, transform.position, Quaternion.identity);

        bool  isCrit      = Random.value < _ctx.CritChance;
        float baseDmg     = _ctx.Damage * _explosionDamageMultiplier;
        float finalDamage = isCrit ? baseDmg * _ctx.CritMultiplier : baseDmg;

        foreach (var col in Physics.OverlapSphere(transform.position, _explosionRadius))
        {
            if (!col.CompareTag("Enemy")) continue;
            col.GetComponent<EnemyHealth>()?.TakeDamage(finalDamage, _ctx.Element, isCrit);
        }

        Destroy(gameObject);
    }
}
