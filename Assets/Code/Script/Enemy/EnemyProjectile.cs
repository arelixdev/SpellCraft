using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyProjectile : MonoBehaviour
{
    private Vector3 _direction;
    private float   _speed;
    private float   _damage;

    public void Initialize(Vector3 direction, float speed, float damage)
    {
        _direction = direction;
        _speed     = speed;
        _damage    = damage;

        Destroy(gameObject, 5f);
    }

    private void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<SpellProjectile>(out _)) return;
        if (other.TryGetComponent<EnemyProjectile>(out _)) return;

        if (other.CompareTag("Player"))
            other.GetComponent<PlayerHealth>()?.TakeDamage(_damage);

        Destroy(gameObject);
    }
}
