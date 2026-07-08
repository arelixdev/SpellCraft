using UnityEngine;

/// <summary>
/// Zone de dégâts persistante (traînée de feu laissée par BossChargeSkill en Rage).
/// Inflige des dégâts au joueur tant qu'il reste dans le trigger, jusqu'à expiration.
/// </summary>
public class BossFireTrailZone : MonoBehaviour
{
    private float        _tickDamage;
    private float        _tickInterval;
    private float        _duration;
    private float        _tickTimer;
    private bool         _playerInside;
    private PlayerHealth _playerHealth;

    public void Initialize(float tickDamage, float tickInterval, float duration)
    {
        _tickDamage   = tickDamage;
        _tickInterval = tickInterval;
        _duration     = duration;
        _tickTimer    = tickInterval;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInside = true;
        _playerHealth = other.GetComponent<PlayerHealth>();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) _playerInside = false;
    }

    private void Update()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (!_playerInside || _playerHealth == null) return;

        _tickTimer -= Time.deltaTime;
        if (_tickTimer <= 0f)
        {
            _tickTimer += _tickInterval;
            _playerHealth.TakeDamage(_tickDamage, ElementType.Fire, false);
        }
    }
}
