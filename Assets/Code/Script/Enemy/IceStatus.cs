using UnityEngine;
using UnityEngine.AI;

public class IceStatus : MonoBehaviour
{
    private NavMeshAgent _agent;
    private float        _baseSpeed;
    private float        _slowPercent;
    private float        _duration;

    public static void Apply(GameObject enemy, float slowPercent, float duration)
    {
        var ice = enemy.GetComponent<IceStatus>() ?? enemy.AddComponent<IceStatus>();
        ice.Refresh(slowPercent, duration);
    }

    private void Refresh(float slowPercent, float duration)
    {
        bool alreadySlowed = _duration > 0f;

        _agent       = GetComponent<NavMeshAgent>();
        _slowPercent = slowPercent;
        _duration    = duration;

        if (!alreadySlowed && _agent != null)
        {
            _baseSpeed   = _agent.speed;
            _agent.speed = _baseSpeed * (1f - slowPercent);
            Debug.Log($"[IceStatus] {name} ralenti de {slowPercent * 100f:F0}% pendant {duration:F1}s");
        }
    }

    private void Update()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0f)
        {
            if (_agent != null)
                _agent.speed = _baseSpeed;
            Debug.Log($"[IceStatus] {name} n'est plus ralenti.");
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        if (_agent != null && _duration > 0f)
            _agent.speed = _baseSpeed;
    }
}
