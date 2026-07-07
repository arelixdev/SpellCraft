using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyRangedAI : MonoBehaviour
{
    [SerializeField] private float _detectionRange = 15f;
    [SerializeField] private float _preferredRange  = 8f;
    public bool CanMove = true;

    private float _baseDetectionRange;
    private float _basePreferredRange;

    private NavMeshAgent _agent;
    private Transform    _player;
    private EnemyLOD     _lod;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _lod   = GetComponent<EnemyLOD>();

        _baseDetectionRange = _detectionRange;
        _basePreferredRange = _preferredRange;
    }

    private void OnEnable()
    {
        // Réinitialise l'ennemi quand le pool le réactive
        _detectionRange = _baseDetectionRange;
        _preferredRange = _basePreferredRange;
    }

    private void Start()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
            _player = playerGO.transform;
    }

    private void Update()
    {
        if (_player == null) return;

        if (_lod != null && !_lod.ShouldUpdatePath()) return;

        if (!CanMove || (_lod != null && !_agent.enabled))
        {
            if (_agent.enabled) _agent.ResetPath();
            return;
        }

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > _detectionRange)
        {
            _agent.ResetPath();
            return;
        }

        if (dist > _preferredRange)
        {
            // Trop loin : se rapproche
            _agent.SetDestination(_player.position);
        }
        else
        {
            // A portée (même si le joueur est très proche) : tient position, laisse EnemyRangedAttack tirer
            _agent.ResetPath();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _preferredRange);
    }

    // Utilisé par EnemyBehaviorRouter : remplace la base (issue d'un EnemyDefinitionSO).
    public void Configure(float detectionRange, float preferredRange)
    {
        _baseDetectionRange = detectionRange;
        _basePreferredRange = preferredRange;
        _detectionRange     = detectionRange;
        _preferredRange     = preferredRange;
    }
}
