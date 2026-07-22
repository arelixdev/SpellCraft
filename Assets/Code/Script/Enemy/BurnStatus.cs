using UnityEngine;

public class BurnStatus : MonoBehaviour
{
    private EnemyHealth _health;
    private float       _tickInterval;
    private float       _tickDamage;
    private float       _duration;
    private float       _tickTimer;

    public static void Apply(GameObject enemy, float tickDamage, float tickInterval, float duration)
    {
        var burn = enemy.GetComponent<BurnStatus>() ?? enemy.AddComponent<BurnStatus>();
        burn.Refresh(tickDamage, tickInterval, duration);
    }

    private void Refresh(float tickDamage, float tickInterval, float duration)
    {
        bool alreadyBurning = _duration > 0f;

        _health       = GetComponent<EnemyHealth>();
        _tickDamage   = tickDamage;
        _tickInterval = tickInterval;
        _duration     = duration;

        if (!alreadyBurning)
        {
            _tickTimer = tickInterval;
            Debug.Log($"[BurnStatus] {name} prend feu ! ({tickDamage} dmg / {tickInterval}s pendant {duration}s)");
        }
    }

    // Le pool désactive le GameObject dès la mort (avant que Update() ait pu se
    // nettoyer via IsDead) : sans ça, la brûlure survivrait jusqu'au prochain respawn.
    private void OnDisable() => Destroy(this);

    private void Update()
    {
        if (_health == null || _health.IsDead)
        {
            Destroy(this);
            return;
        }

        _duration  -= Time.deltaTime;
        _tickTimer -= Time.deltaTime;

        if (_tickTimer <= 0f)
        {
            _tickTimer += _tickInterval;
            Debug.Log($"[BurnStatus] {name} brûle → -{_tickDamage} dmg (reste {_duration:F1}s)");
            _health.TakeDamage(_tickDamage, ElementType.Fire, false);
        }

        if (_duration <= 0f)
            Destroy(this);
    }
}
