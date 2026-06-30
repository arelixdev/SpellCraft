using UnityEngine;

public class EnemyMeleeAttack : MonoBehaviour
{
    [SerializeField] private float _damage      = 10f;
    [SerializeField] private float _attackRange = 1.8f;
    [SerializeField] private float _attackRate  = 1f; // attacks per second

    private float         _attackCooldown;
    private Transform     _player;
    private PlayerHealth  _playerHealth;
    private EnemyLOD      _lod;

    private void Start()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null) return;

        _player       = playerGO.transform;
        _playerHealth = playerGO.GetComponent<PlayerHealth>();
        _lod          = GetComponent<EnemyLOD>();
    }

    private void Update()
    {
        if (_player == null || _playerHealth == null) return;

        // Le cooldown décrémente toujours (précision indépendante du LOD)
        if (_attackCooldown > 0f)
        {
            _attackCooldown -= Time.deltaTime;
            return;
        }

        // Vérification de distance/attaque uniquement quand le LOD l'autorise
        if (_lod != null && !_lod.ShouldUpdateAttack()) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= _attackRange)
            Attack();
    }

    public void ApplyMultiplier(float multiplier) => _damage *= multiplier;

    private void Attack()
    {
        _playerHealth.TakeDamage(_damage);
        _attackCooldown = 1f / _attackRate;

        Debug.Log($"[EnemyMeleeAttack] {name} hit player for {_damage} dmg.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}
