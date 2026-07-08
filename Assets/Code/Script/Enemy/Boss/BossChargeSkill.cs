using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Compétence "Charge" : télégraphe court, puis fonce en ligne droite dans la direction
/// visée au moment du cast. En Rage, laisse une traînée de feu sur tout le trajet.
/// Le déclenchement (choix aléatoire, exclusivité, délai entre skills) est géré par BossController.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class BossChargeSkill : MonoBehaviour, IBossSkill
{
    [Header("Portée de déclenchement")]
    [SerializeField] private float _minRange = 3f;
    [SerializeField] private float _maxRange = 15f;

    [Header("Charge")]
    [SerializeField] private float _telegraphDuration = 0.6f;
    [SerializeField] private float _chargeDistance    = 10f;
    [SerializeField] private float _chargeSpeed       = 18f;
    [SerializeField] private float _damage            = 20f;
    [SerializeField] private float _hitRadius         = 2f;

    [Header("Traînée de feu (Rage)")]
    [SerializeField] private float _fireTickDamage   = 4f;
    [SerializeField] private float _fireTickInterval = 0.5f;
    [SerializeField] private float _fireDuration     = 4f;
    [SerializeField] private float _fireWidth        = 3f;

    [HideInInspector] public bool LeavesFireTrail;

    private Transform    _player;
    private PlayerHealth _playerHealth;
    private NavMeshAgent _agent;
    private EnemyAI      _ai;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _ai    = GetComponent<EnemyAI>();
    }

    private void Start()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null) return;

        _player       = playerGO.transform;
        _playerHealth = playerGO.GetComponent<PlayerHealth>();
    }

    public bool IsInRange(Transform player)
    {
        float dist = Vector3.Distance(transform.position, player.position);
        return dist >= _minRange && dist <= _maxRange;
    }

    public IEnumerator Execute()
    {
        _ai.CanMove = false;
        _agent.ResetPath();

        // Direction et orientation figées dès le cast : le télégraphe montre où le boss va foncer.
        Vector3 direction = _player.position - transform.position;
        direction.y = 0f;
        direction.Normalize();
        transform.rotation    = Quaternion.LookRotation(direction);
        _agent.updateRotation = false;

        yield return new WaitForSeconds(_telegraphDuration);

        Vector3 startPos = transform.position;
        bool    hasHit   = false;
        float   traveled = 0f;

        while (traveled < _chargeDistance)
        {
            float step = _chargeSpeed * Time.deltaTime;
            _agent.Move(direction * step);
            traveled += step;

            if (!hasHit && Vector3.Distance(transform.position, _player.position) <= _hitRadius)
            {
                _playerHealth.TakeDamage(_damage);
                hasHit = true;
            }

            yield return null;
        }

        if (LeavesFireTrail)
            SpawnFireTrail(startPos, transform.position, direction);

        _agent.updateRotation = true;
        _ai.CanMove           = true;
    }

    private void SpawnFireTrail(Vector3 start, Vector3 end, Vector3 direction)
    {
        float   length = Vector3.Distance(start, end);
        Vector3 mid    = (start + end) * 0.5f;

        var zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zone.name = "BossFireTrail";
        Destroy(zone.GetComponent<Collider>());

        var col = zone.AddComponent<BoxCollider>();
        col.isTrigger = true;

        zone.GetComponent<MeshRenderer>().material.color = new Color(1f, 0.3f, 0f, 0.6f);

        zone.transform.position   = mid + Vector3.up * 0.05f;
        zone.transform.rotation  = Quaternion.LookRotation(direction);
        zone.transform.localScale = new Vector3(_fireWidth, 0.1f, length);

        zone.AddComponent<BossFireTrailZone>().Initialize(_fireTickDamage, _fireTickInterval, _fireDuration);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _minRange);
        Gizmos.DrawWireSphere(transform.position, _maxRange);
    }
}
