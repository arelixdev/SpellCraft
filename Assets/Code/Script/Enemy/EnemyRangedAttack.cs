using UnityEngine;

public class EnemyRangedAttack : MonoBehaviour
{
    [SerializeField] private float      _damage          = 10f;
    [SerializeField] private float      _attackRange     = 10f;
    [SerializeField] private float      _attackRate      = 1f; // attacks per second
    [SerializeField] private float      _projectileSpeed = 12f;
    [SerializeField] private GameObject _projectilePrefab;

    private float         _baseDamage;
    private float         _baseAttackRange;
    private float         _baseAttackRate;
    private float         _attackCooldown;
    private Transform     _player;
    private EnemyLOD      _lod;

    private void Start()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null) return;

        _player = playerGO.transform;
        _lod    = GetComponent<EnemyLOD>();
    }

    private void Update()
    {
        if (_player == null) return;

        if (_attackCooldown > 0f)
        {
            _attackCooldown -= Time.deltaTime;
            return;
        }

        if (_lod != null && !_lod.ShouldUpdateAttack()) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= _attackRange)
            Attack();
    }

    private void Awake()
    {
        _baseDamage      = _damage;
        _baseAttackRange = _attackRange;
        _baseAttackRate  = _attackRate;
    }

    private void OnEnable()
    {
        _damage      = _baseDamage;
        _attackRange = _baseAttackRange;
        _attackRate  = _baseAttackRate;
    }

    public void ApplyMultiplier(float multiplier) => _damage = _baseDamage * multiplier;

    // Utilisé par EnemyGenericController : remplace la base (issue d'un EnemyDefinitionSO) avant tout ApplyMultiplier.
    public void SetBase(float baseDamage)
    {
        _baseDamage = baseDamage;
        _damage     = baseDamage;
    }

    // Utilisé par EnemyBehaviorRouter : remplace la base (issue d'un EnemyDefinitionSO).
    public void Configure(float attackRange, float attackRate, float projectileSpeed, GameObject projectilePrefab)
    {
        _baseAttackRange   = attackRange;
        _baseAttackRate    = attackRate;
        _attackRange       = attackRange;
        _attackRate        = attackRate;
        _projectileSpeed   = projectileSpeed;
        _projectilePrefab  = projectilePrefab;
    }

    private void Attack()
    {
        _attackCooldown = 1f / _attackRate;

        if (_projectilePrefab == null) return;

        Vector3 direction = (_player.position - transform.position).normalized;
        var go = Instantiate(_projectilePrefab, transform.position, Quaternion.LookRotation(direction));
        go.GetComponent<EnemyProjectile>()?.Initialize(direction, _projectileSpeed, _damage);

        Debug.Log($"[EnemyRangedAttack] {name} fired projectile for {_damage} dmg.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}
