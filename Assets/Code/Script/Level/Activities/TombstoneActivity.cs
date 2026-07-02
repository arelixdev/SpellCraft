using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using Action = System.Action;

/// <summary>
/// Activité tombstone : le joueur entre dans le trigger, appuie sur Espace,
/// et fait apparaître soit un monstre spécial unique, soit un groupe de X ennemis
/// élites (X tiré aléatoirement entre Min et Max élites).
/// Une fois tous les ennemis spawnés par la tombe tués, un NodePickup apparaît
/// à la position du dernier ennemi tué.
/// </summary>
public class TombstoneActivity : ActivityBase
{
    // ── Prefabs ──────────────────────────────────────────────────────────────────
    [BoxGroup("Monstre spécial"), Required, LabelText("Prefab")]
    public GameObject SpecialMonsterPrefab;

    [BoxGroup("Élites"), Required, LabelText("Prefab")]
    public GameObject EliteEnemyPrefab;

    [BoxGroup("Élites"), MinValue(1), LabelText("Min élites")]
    public int MinEliteCount = 2;

    [BoxGroup("Élites"), MinValue(1), LabelText("Max élites")]
    public int MaxEliteCount = 4;

    // ── Tirage ───────────────────────────────────────────────────────────────────
    [BoxGroup("Tirage"), Range(0f, 1f), LabelText("Chance monstre spécial")]
    [Tooltip("Probabilité de faire apparaître le monstre spécial plutôt que le groupe d'élites")]
    public float SpecialMonsterChance = 0.5f;

    // ── Placement ────────────────────────────────────────────────────────────────
    [BoxGroup("Placement"), MinValue(0.5f), LabelText("Rayon de spawn")]
    public float SpawnRadius = 6f;

    [BoxGroup("Placement"), MinValue(0.1f), LabelText("Tolérance NavMesh")]
    public float NavMeshSampleDistance = 2f;

    // ── Récompense ───────────────────────────────────────────────────────────────
    [BoxGroup("Récompense"), Required, LabelText("Prefab Node Pickup")]
    [Tooltip("Instancié à la position du dernier ennemi tué une fois tous les spawns de la tombe éliminés")]
    public GameObject NodePickupPrefab;

    /// <summary>Déclenché une fois que tous les ennemis spawnés par cette tombe sont morts.</summary>
    public event Action OnEnemiesCleared;

    // ── État ─────────────────────────────────────────────────────────────────────
    private int _aliveSpawnCount;

    // ── Interaction ──────────────────────────────────────────────────────────────
    protected override void OnInteract(SpellCaster caster)
    {
        if (Random.value < SpecialMonsterChance)
            SpawnEnemy(SpecialMonsterPrefab);
        else
        {
            int count = Random.Range(MinEliteCount, MaxEliteCount + 1);
            for (int i = 0; i < count; i++)
                SpawnEnemy(EliteEnemyPrefab);
        }

        Complete();
    }

    private void SpawnEnemy(GameObject prefab)
    {
        if (prefab == null) return;
        if (!TryGetSpawnPosition(out Vector3 pos)) pos = transform.position;

        var enemy = EnemyPool.Instance != null
            ? EnemyPool.Instance.Get(prefab, pos, Quaternion.identity)
            : Instantiate(prefab, pos, Quaternion.identity);

        var health = enemy.GetComponent<EnemyHealth>();
        if (health == null)
        {
            Debug.LogWarning($"[TombstoneActivity] '{prefab.name}' n'a pas de EnemyHealth, sa mort ne sera pas suivie.");
            return;
        }

        _aliveSpawnCount++;
        health.OnDied += () => HandleSpawnedEnemyDied(health);
    }

    private void HandleSpawnedEnemyDied(EnemyHealth enemy)
    {
        _aliveSpawnCount--;
        if (_aliveSpawnCount > 0) return;

        if (NodePickupPrefab != null)
            Instantiate(NodePickupPrefab, enemy.transform.position, Quaternion.identity);

        OnEnemiesCleared?.Invoke();
    }

    private bool TryGetSpawnPosition(out Vector3 result)
    {
        const int maxAttempts = 30;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 rnd = Random.insideUnitCircle * SpawnRadius;
            Vector3 candidate = transform.position + new Vector3(rnd.x, 0f, rnd.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSampleDistance, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.1f, 0.8f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, SpawnRadius);
    }
}
