using UnityEngine;

public class PoisonStatus : MonoBehaviour
{
    private const int MaxStacks = 10;

    private EnemyHealth _health;
    private float       _tickDamage;
    private float       _tickInterval;
    private float       _stackDuration;
    private int         _stacks;
    private float       _tickTimer;
    private float       _durationTimer;

    public static void Apply(GameObject enemy, float tickDamage, float tickInterval, float stackDuration, int stackCount = 1)
    {
        var poison = enemy.GetComponent<PoisonStatus>() ?? enemy.AddComponent<PoisonStatus>();
        poison.AddStack(tickDamage, tickInterval, stackDuration, stackCount);
    }

    private void AddStack(float tickDamage, float tickInterval, float stackDuration, int stackCount)
    {
        _health        = GetComponent<EnemyHealth>();
        _tickDamage    = tickDamage;
        _tickInterval  = tickInterval;
        _stackDuration = stackDuration;

        bool isFirst = _stacks == 0;
        _stacks        = Mathf.Min(_stacks + Mathf.Max(1, stackCount), MaxStacks);
        _durationTimer = stackDuration;

        if (isFirst)
            _tickTimer = tickInterval;

        Debug.Log($"[PoisonStatus] {name} empoisonné — stack {_stacks}/{MaxStacks}");
    }

    // Le pool désactive le GameObject dès la mort (avant que Update() ait pu se
    // nettoyer via IsDead) : sans ça, les stacks survivraient jusqu'au prochain respawn.
    private void OnDisable() => Destroy(this);

    private void Update()
    {
        if (_health == null || _health.IsDead)
        {
            Destroy(this);
            return;
        }

        _durationTimer -= Time.deltaTime;
        _tickTimer     -= Time.deltaTime;

        if (_tickTimer <= 0f)
        {
            _tickTimer += _tickInterval;
            float dmg = _tickDamage * _stacks;
            Debug.Log($"[PoisonStatus] {name} → -{dmg:F1} dmg ({_stacks} stacks)");
            _health.TakeDamage(dmg, ElementType.Poison, false);
        }

        if (_durationTimer <= 0f)
        {
            _stacks--;
            Debug.Log($"[PoisonStatus] {name} perd une stack → {_stacks}/{MaxStacks}");
            if (_stacks <= 0)
            {
                Destroy(this);
                return;
            }
            _durationTimer = _stackDuration;
        }
    }
}
