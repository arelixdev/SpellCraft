using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] public  bool  IsInvincible = false;

    [Header("UI")]
    [SerializeField] private DamagePopup _damagePopupPrefab;
    [SerializeField] private Vector3     _popupOffset = new Vector3(0f, 2f, 0f);

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
        var popup = Instantiate(_damagePopupPrefab, transform.position + _popupOffset, Quaternion.identity);
        popup.Setup(damage, isCrit, element);
    }

    private void Die()
    {
        OnDied?.Invoke();
        Debug.Log($"[EnemyHealth] {name} died.");
        Destroy(gameObject);
    }
}
