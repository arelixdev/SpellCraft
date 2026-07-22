using UnityEngine;
using UnityEngine.AI;

public class SlowStatus : MonoBehaviour
{
    private NavMeshAgent _agent;
    private float        _baseSpeed;
    private float        _duration;

    public static void Apply(GameObject enemy, float slowPercent, float duration)
    {
        var slow = enemy.GetComponent<SlowStatus>() ?? enemy.AddComponent<SlowStatus>();
        slow.Refresh(slowPercent, duration);
    }

    private void Refresh(float slowPercent, float duration)
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null) return;

        bool alreadySlowed = _duration > 0f;
        if (!alreadySlowed)
        {
            _baseSpeed = _agent.speed;
            _agent.speed = _baseSpeed * (1f - slowPercent);
            Debug.Log($"[SlowStatus] {name} ralenti de {slowPercent * 100f:F0}% ({_baseSpeed:F1} → {_agent.speed:F1}) pendant {duration:F1}s");
        }

        _duration = duration;
    }

    // Le pool désactive le GameObject dès la mort (avant que Update() ait pu se
    // nettoyer via la durée) : sans ça, le ralentissement (et la vitesse réduite de
    // l'agent) survivrait jusqu'au prochain respawn.
    private void OnDisable() => Destroy(this);

    private void Update()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0f)
        {
            if (_agent != null)
                _agent.speed = _baseSpeed;
            Debug.Log($"[SlowStatus] {name} n'est plus ralenti.");
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        if (_agent != null && _duration > 0f)
            _agent.speed = _baseSpeed;
    }
}
