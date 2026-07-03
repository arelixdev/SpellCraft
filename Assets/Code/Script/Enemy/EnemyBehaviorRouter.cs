using UnityEngine;

/// <summary>
/// Bascule entre les paires de composants Chaser (EnemyAI + EnemyMeleeAttack) et
/// RangedCaster (EnemyRangedAI + EnemyRangedAttack) selon l'EnemyDefinitionSO courant.
/// Par défaut (avant tout Configure), retombe toujours sur Chaser — nécessaire pour les
/// spawns hors EnemySpawner (ex: TombstoneActivity) qui n'appellent jamais Initialize.
/// </summary>
public class EnemyBehaviorRouter : MonoBehaviour
{
    private EnemyAI            _ai;
    private EnemyMeleeAttack   _meleeAttack;
    private EnemyRangedAI      _rangedAI;
    private EnemyRangedAttack  _rangedAttack;

    private void Awake()
    {
        _ai           = GetComponent<EnemyAI>();
        _meleeAttack  = GetComponent<EnemyMeleeAttack>();
        _rangedAI     = GetComponent<EnemyRangedAI>();
        _rangedAttack = GetComponent<EnemyRangedAttack>();
    }

    private void OnEnable()
    {
        // Réinitialise l'ennemi quand le pool le réactive : Chaser par défaut.
        if (_ai           != null) _ai.enabled           = true;
        if (_meleeAttack  != null) _meleeAttack.enabled   = true;
        if (_rangedAI     != null) _rangedAI.enabled      = false;
        if (_rangedAttack != null) _rangedAttack.enabled  = false;
    }

    public void Configure(EnemyDefinitionSO def, float damageMult)
    {
        bool isChaser = def.Behavior == EnemyDefinitionSO.BehaviorType.Chaser;

        if (_ai           != null) _ai.enabled           = isChaser;
        if (_meleeAttack  != null) _meleeAttack.enabled   = isChaser;
        if (_rangedAI     != null) _rangedAI.enabled      = !isChaser;
        if (_rangedAttack != null) _rangedAttack.enabled  = !isChaser;

        if (isChaser)
        {
            _ai?.Configure(def.DetectionRange, def.StopDistance);
            if (_meleeAttack != null)
            {
                _meleeAttack.SetBase(def.BaseDamage * damageMult);
                _meleeAttack.Configure(def.AttackRange, def.AttackRate);
            }
        }
        else
        {
            _rangedAI?.Configure(def.DetectionRange, def.PreferredRange);
            if (_rangedAttack != null)
            {
                _rangedAttack.SetBase(def.BaseDamage * damageMult);
                _rangedAttack.Configure(def.AttackRange, def.AttackRate, def.ProjectileSpeed, def.ProjectilePrefab);
            }
        }
    }
}
