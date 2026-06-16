using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float _detectionRange = 15f;
    [SerializeField] private float _stopDistance   = 1.5f;
    public bool CanMove = true;

    private NavMeshAgent _agent;
    private Transform    _player;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
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

        if (!CanMove)
        {
            _agent.ResetPath();
            return;
        }

        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist <= _detectionRange && dist > _stopDistance)
            _agent.SetDestination(_player.position);
        else
            _agent.ResetPath();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _stopDistance);
    }
}
