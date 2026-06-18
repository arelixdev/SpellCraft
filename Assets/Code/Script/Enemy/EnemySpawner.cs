using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    public enum SpawnMode { Limité, Illimité }

    // ── Mode ───────────────────────────────────────────────────────────────────
    [Title("Mode de spawn")]
    [EnumToggleButtons, HideLabel]
    public SpawnMode Mode = SpawnMode.Limité;

    // ── Prefab & paramètres communs ────────────────────────────────────────────
    [BoxGroup("Spawn"), Required, LabelText("Prefab ennemi")]
    public GameObject EnemyPrefab;

    [BoxGroup("Spawn"), MinValue(1), LabelText("Ennemis par vague")]
    public int EnemiesPerWave = 1;

    [BoxGroup("Spawn"), MinValue(0.1f), LabelText("Intervalle (secondes)")]
    public float SpawnInterval = 3f;

    [BoxGroup("Spawn"), MinValue(1f), LabelText("Rayon de spawn")]
    public float SpawnRadius = 15f;

    [BoxGroup("Spawn"), MinValue(0.1f), LabelText("Tolérance NavMesh")]
    public float NavMeshSampleDistance = 2f;

    // ── Paramètres mode Limité ─────────────────────────────────────────────────
    [BoxGroup("Mode Limité"), ShowIf("@Mode == SpawnMode.Limité")]
    [MinValue(1), LabelText("Max ennemis vivants")]
    public int MaxAliveEnemies = 20;

    // ── Debug (lecture seule) ──────────────────────────────────────────────────
    [FoldoutGroup("Debug"), ShowInInspector, ReadOnly, LabelText("Ennemis vivants")]
    private int DebugAliveCount => _spawnedEnemies.Count(e => e != null);

    [FoldoutGroup("Debug"), ShowInInspector, ReadOnly, LabelText("Spawn actif")]
    private bool DebugIsRunning => _spawnCoroutine != null;

    [BoxGroup("Spawn"), LabelText("Démarrage automatique")]
    [Tooltip("Cocher uniquement si ce spawner est indépendant (pas contrôlé par un ZoneManager)")]
    public bool AutoStart = false;

    // ── Privé ──────────────────────────────────────────────────────────────────
    private Transform _player;
    private Camera _camera;
    private readonly List<GameObject> _spawnedEnemies = new();
    private Coroutine _spawnCoroutine;
    private Transform _spawnParent;

    // ── Unity ──────────────────────────────────────────────────────────────────
    private void Start()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null) _player = playerGO.transform;
        _camera = Camera.main;

        _spawnParent = new GameObject("[SpawnedEnemies]").transform;

        if (AutoStart)
            StartSpawning();
    }

    private void OnDestroy()
    {
        if (_spawnParent != null)
            Destroy(_spawnParent.gameObject);
    }

    // ── API publique ───────────────────────────────────────────────────────────
    [FoldoutGroup("Debug"), Button("▶  Démarrer")]
    public void StartSpawning()
    {
        if (_spawnCoroutine != null) return;
        _spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    [FoldoutGroup("Debug"), Button("■  Arrêter")]
    public void StopSpawning()
    {
        if (_spawnCoroutine == null) return;
        StopCoroutine(_spawnCoroutine);
        _spawnCoroutine = null;
    }

    // ── Logique de spawn ───────────────────────────────────────────────────────
    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(SpawnInterval);

            if (_player == null || EnemyPrefab == null) continue;

            if (Mode == SpawnMode.Limité)
            {
                _spawnedEnemies.RemoveAll(e => e == null);
                int slots = MaxAliveEnemies - _spawnedEnemies.Count;
                if (slots <= 0) continue;
                SpawnBatch(Mathf.Min(EnemiesPerWave, slots));
            }
            else
            {
                SpawnBatch(EnemiesPerWave);
            }
        }
    }

    private void SpawnBatch(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!TryGetSpawnPosition(out Vector3 pos)) continue;

            var enemy = Instantiate(EnemyPrefab, pos, Quaternion.identity, _spawnParent);

            if (Mode == SpawnMode.Limité)
                _spawnedEnemies.Add(enemy);
        }
    }

    private bool TryGetSpawnPosition(out Vector3 result)
    {
        const int maxAttempts = 30;

        for (int i = 0; i < maxAttempts; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 candidate = _player.position + dir * SpawnRadius;

            if (_camera != null && !IsOutsideCamera(candidate)) continue;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSampleDistance, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    private bool IsOutsideCamera(Vector3 worldPos)
    {
        Vector3 vp = _camera.WorldToViewportPoint(worldPos);
        return vp.z < 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f;
    }

    // ── Gizmos ─────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Transform origin = Application.isPlaying && _player != null ? _player : transform;
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(origin.position, SpawnRadius);
    }
}
