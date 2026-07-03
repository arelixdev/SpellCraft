using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Configure un ennemi générique à partir d'un EnemyDefinitionSO au moment du spawn :
/// PV/dégâts scalés par niveau, vitesse, et comportement (via EnemyBehaviorRouter).
/// </summary>
[RequireComponent(typeof(EnemyBehaviorRouter))]
public class EnemyGenericController : MonoBehaviour
{
    private EnemyHealth         _health;
    private NavMeshAgent        _agent;
    private EnemyBehaviorRouter _router;

    private void Awake()
    {
        _health = GetComponent<EnemyHealth>();
        _agent  = GetComponent<NavMeshAgent>();
        _router = GetComponent<EnemyBehaviorRouter>();
    }

    public void Initialize(EnemyDefinitionSO def, int level)
    {
        int   lvl        = Mathf.Max(1, level);
        float healthMult = Mathf.Pow(def.HealthScalePerLevel, lvl - 1);
        float damageMult = Mathf.Pow(def.DamageScalePerLevel, lvl - 1);

        _health?.SetBase(def.BaseHealth * healthMult);
        if (_agent != null) _agent.speed = def.BaseMoveSpeed;
        _router?.Configure(def, damageMult);
    }
}
