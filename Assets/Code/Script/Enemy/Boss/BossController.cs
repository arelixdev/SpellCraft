using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Pilote les phases du boss en fonction de son % de PV, et choisit aléatoirement
/// UNE compétence à la fois parmi celles à portée (jamais deux en même temps,
/// jamais enchaînées sans respiration : voir _minDelayBetweenSkills/_maxDelayBetweenSkills).
/// Phase1 (100-60%) / Phase2 (60-30%, délai plus court) / Rage (30-0%, Charge laisse une traînée de feu).
/// </summary>
public class BossController : MonoBehaviour
{
    public enum Phase { Phase1, Phase2, Rage }

    [System.Serializable]
    public class PhaseEvent : UnityEvent<Phase> { }

    [Header("Seuils de phase (% PV)")]
    [SerializeField] private float _phase2Threshold = 0.6f;
    [SerializeField] private float _rageThreshold    = 0.3f;

    [Header("Délai entre deux compétences (s)")]
    [Tooltip("Après l'exécution d'un skill, le boss attend un temps aléatoire dans cette plage avant d'en retenter un (mêlée de base toujours disponible entre-temps)")]
    [SerializeField] private float _minDelayBetweenSkills = 1.5f;
    [SerializeField] private float _maxDelayBetweenSkills = 3f;

    [Header("Cadence")]
    [Tooltip("Divise le délai entre compétences en Phase2 et Rage (skills plus fréquents)")]
    [SerializeField] private float _fastCadenceMultiplier = 1.5f;

    public PhaseEvent OnPhaseChanged;

    // Verrou partagé : un seul skill (ou aucun) s'exécute à la fois.
    [HideInInspector] public bool IsBusy;

    private EnemyHealth      _health;
    private BossChargeSkill  _chargeSkill;
    private Transform        _player;
    private List<IBossSkill> _skills;
    private Phase            _currentPhase      = Phase.Phase1;
    private float            _cadenceMultiplier = 1f;
    private float            _nextSkillTime;

    private void Awake()
    {
        _health      = GetComponent<EnemyHealth>();
        _chargeSkill = GetComponent<BossChargeSkill>();
        _skills      = new List<IBossSkill>(GetComponents<IBossSkill>());
    }

    private void Start()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null) _player = playerGO.transform;
    }

    private void Update()
    {
        if (_health == null || _health.IsDead || _player == null) return;

        UpdatePhase();

        if (IsBusy || Time.time < _nextSkillTime) return;

        TryCastRandomSkill();
    }

    private void UpdatePhase()
    {
        float ratio = _health.CurrentHealth / _health.MaxHealth;
        Phase target = ratio <= _rageThreshold  ? Phase.Rage
                     : ratio <= _phase2Threshold ? Phase.Phase2
                     : Phase.Phase1;

        if (target != _currentPhase)
            SetPhase(target);
    }

    private void SetPhase(Phase phase)
    {
        _currentPhase      = phase;
        _cadenceMultiplier = phase == Phase.Phase1 ? 1f : _fastCadenceMultiplier;

        if (_chargeSkill != null)
            _chargeSkill.LeavesFireTrail = phase == Phase.Rage;

        Debug.Log($"[BossController] {name} → {phase}");
        OnPhaseChanged?.Invoke(phase);
    }

    private void TryCastRandomSkill()
    {
        var candidates = new List<IBossSkill>();
        foreach (var skill in _skills)
            if (skill.IsInRange(_player))
                candidates.Add(skill);

        if (candidates.Count == 0) return;

        var chosen = candidates[Random.Range(0, candidates.Count)];
        StartCoroutine(CastRoutine(chosen));
    }

    private IEnumerator CastRoutine(IBossSkill skill)
    {
        IsBusy = true;

        yield return skill.Execute();

        IsBusy         = false;
        _nextSkillTime = Time.time + Random.Range(_minDelayBetweenSkills, _maxDelayBetweenSkills) / _cadenceMultiplier;
    }
}
