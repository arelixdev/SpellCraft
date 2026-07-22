using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private bool  _isInvincible = false;

    [Header("Invincibility Frames")]
    [SerializeField] private float _invincibilityDuration = 0f;

    private float            _currentHealth;
    private float            _invincibilityTimer;
    private PlayerController _playerController;
    private SpellCaster      _spellCaster;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth     => _maxHealth;
    public bool  IsDead        => _currentHealth <= 0f;
    public float HealthRatio   => _currentHealth / _maxHealth;

    public event Action<float, float> OnHealthChanged; // current, max
    public event Action               OnDied;

    public void SetMaxHealth(float value) => _maxHealth = value;

    private void Awake()
    {
        _currentHealth    = _maxHealth;
        _playerController = GetComponent<PlayerController>();
        _spellCaster      = GetComponent<SpellCaster>();
    }

    private void Update()
    {
        if (_invincibilityTimer > 0f)
            _invincibilityTimer -= Time.deltaTime;
    }

    public void TakeDamage(float damage, ElementType element = ElementType.None, bool isCrit = false)
    {
        if (_isInvincible || IsDead || _invincibilityTimer > 0f) return;

        _currentHealth = Mathf.Max(0f, _currentHealth - damage);

        Debug.Log($"[PlayerHealth] -{damage} ({element}){(isCrit ? " [CRIT]" : "")} | HP: {_currentHealth}/{_maxHealth}");

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (_invincibilityDuration > 0f)
            _invincibilityTimer = _invincibilityDuration;

        if (IsDead)
            Die();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;

        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);

        Debug.Log($"[PlayerHealth] +{amount} heal | HP: {_currentHealth}/{_maxHealth}");

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    private void Die()
    {
        if (_playerController != null) _playerController.enabled = false;
        if (_spellCaster      != null) _spellCaster.enabled      = false;

        OnDied?.Invoke();
        Debug.Log("[PlayerHealth] Player died.");
    }

    // Le Player survit au rechargement de scène (PlayerPersistence), donc son Awake() ne
    // se relance pas au démarrage d'un nouveau run : sans cet appel explicite, un run
    // recommencé après une mort garderait 0 PV et un PlayerController désactivé.
    public void ResetForNewRun()
    {
        _currentHealth       = _maxHealth;
        _invincibilityTimer  = 0f;

        if (_playerController != null) _playerController.enabled = true;
        if (_spellCaster      != null) _spellCaster.enabled      = true;

        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }
}
