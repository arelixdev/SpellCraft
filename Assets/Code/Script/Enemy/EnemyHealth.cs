using System;
using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 25f;
    [SerializeField] public  bool  IsInvincible = false;

    [Header("UI")]
    [SerializeField] private DamagePopup _damagePopupPrefab;
    [SerializeField] private Vector3     _popupOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private float       _popupStagger = 0.2f;

    private float _nextPopupTime = 0f;

    private float _baseMaxHealth; // valeur inspector, jamais modifiée
    private float _currentHealth;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth     => _maxHealth;
    public bool  IsDead        => _currentHealth <= 0f;

    public event Action OnDied;

    private void Awake()
    {
        _baseMaxHealth = _maxHealth;
        _currentHealth = _maxHealth;
    }

    private void OnEnable()
    {
        // Réinitialise l'ennemi quand le pool le réactive
        _maxHealth     = _baseMaxHealth;
        _currentHealth = _maxHealth;
        OnDied         = null; // purge les abonnements de la vie précédente
    }

    // Utilisé par EnemySpawner et BossPortal : multiplie depuis la base inspector.
    public void ApplyMultiplier(float multiplier)
    {
        _maxHealth     = Mathf.Round(_baseMaxHealth * multiplier);
        _currentHealth = _maxHealth;
    }

    // Utilisé par EnemyGenericController : remplace la base (issue d'un EnemyDefinitionSO) avant tout ApplyMultiplier.
    public void SetBase(float baseHealth)
    {
        _baseMaxHealth = baseHealth;
        _maxHealth     = baseHealth;
        _currentHealth = baseHealth;
    }

    public void TakeDamage(float damage, ElementType element, bool isCrit)
    {
        SpawnPopup(damage, element, isCrit);

        if (IsInvincible || IsDead) return;

        _currentHealth = Mathf.Max(0f, _currentHealth - damage);

        Debug.Log($"[EnemyHealth] {name} → -{damage:F0} ({element}){(isCrit ? " [CRIT]" : "")} | HP: {_currentHealth:F0}/{_maxHealth:F0}");

        if (IsDead)
            Die();
    }

    private void SpawnPopup(float damage, ElementType element, bool isCrit)
    {
        if (_damagePopupPrefab == null) return;

        float now      = Time.time;
        float spawnAt  = Mathf.Max(now, _nextPopupTime);
        float delay    = spawnAt - now;
        _nextPopupTime = spawnAt + _popupStagger;

        StartCoroutine(DelayedPopup(damage, element, isCrit, delay));
    }

    private IEnumerator DelayedPopup(float damage, ElementType element, bool isCrit, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        Vector3 offset = _popupOffset + new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), 0f, 0f);
        var popup = Instantiate(_damagePopupPrefab, transform.position + offset, Quaternion.identity);
        popup.Setup(damage, isCrit, element);
    }

    private void Die()
    {
        OnDied?.Invoke();
        Debug.Log($"[EnemyHealth] {name} died.");

        if (EnemyPool.Instance != null)
            EnemyPool.Instance.Return(gameObject);
        else
            Destroy(gameObject);
    }
}
