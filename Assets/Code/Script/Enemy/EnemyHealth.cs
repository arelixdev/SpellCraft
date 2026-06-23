using System;
using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] public  bool  IsInvincible = false;

    [Header("UI")]
    [SerializeField] private DamagePopup _damagePopupPrefab;
    [SerializeField] private Vector3     _popupOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private float       _popupStagger = 0.2f;

    private float _nextPopupTime = 0f;

    private float _currentHealth;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth     => _maxHealth;
    public bool  IsDead        => _currentHealth <= 0f;

    public event Action OnDied;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(float damage, ElementType element, bool isCrit)
    {
        SpawnPopup(damage, element, isCrit);

        if (IsInvincible || IsDead) return;

        _currentHealth = Mathf.Max(0f, _currentHealth - damage);

        Debug.Log($"[EnemyHealth] {name} → -{damage} ({element}){(isCrit ? " [CRIT]" : "")} | HP: {_currentHealth}/{_maxHealth}");

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
        Destroy(gameObject);
    }
}
