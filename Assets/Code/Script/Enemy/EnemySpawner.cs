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

    // ── Prefab & paramètres fixes ──────────────────────────────────────────────
    [BoxGroup("Spawn"), Required, LabelText("Prefab ennemi")]
    public GameObject EnemyPrefab;

    [BoxGroup("Spawn"), MinValue(1f), LabelText("Rayon de spawn")]
    public float SpawnRadius = 15f;

    [BoxGroup("Spawn"), MinValue(0.1f), LabelText("Tolérance NavMesh")]
    public float NavMeshSampleDistance = 2f;

    [BoxGroup("Spawn"), LabelText("Démarrage automatique")]
    [Tooltip("Cocher uniquement si ce spawner est indépendant (pas contrôlé par un RunController)")]
    public bool AutoStart = false;

    [BoxGroup("Spawn"), MinValue(0f), LabelText("Délai initial (s)")]
    [Tooltip("Attente avant le premier spawn après StartSpawning()")]
    public float InitialDelay = 5f;

    // ── Paramètres mode Limité ─────────────────────────────────────────────────
    [BoxGroup("Mode Limité"), ShowIf("@Mode == SpawnMode.Limité")]
    [MinValue(1), LabelText("Max ennemis vivants")]
    public int MaxAliveEnemies = 80;

    // ── Courbes de progression ─────────────────────────────────────────────────
    [Title("Courbes de progression")]
    [InfoBox("X = secondes écoulées depuis le début du run. Les valeurs sont évaluées en temps réel par RunController.", InfoMessageType.Info)]

    [LabelText("Intervalle entre vagues (x=s, y=secondes)")]
    [Tooltip("Plus la courbe descend, plus les vagues arrivent vite")]
    public AnimationCurve SpawnIntervalCurve = new AnimationCurve(
        new Keyframe(0f,   0.8f),
        new Keyframe(60f,  0.4f),
        new Keyframe(180f, 0.15f));

    [LabelText("Ennemis par vague (x=s, y=nombre)")]
    [Tooltip("Nombre d'ennemis spawné à chaque vague")]
    public AnimationCurve EnemiesPerWaveCurve = new AnimationCurve(
        new Keyframe(0f,   3f),
        new Keyframe(60f,  5f),
        new Keyframe(180f, 10f));

    [LabelText("Taille (x=s, y=scale)")]
    [Tooltip("Multiplicateur de taille appliqué à chaque ennemi spawné")]
    public AnimationCurve ScaleCurve = new AnimationCurve(
        new Keyframe(0f,   1f),
        new Keyframe(60f,  1.3f),
        new Keyframe(180f, 2f));

    [LabelText("Dégâts (x=s, y=multiplicateur)")]
    [Tooltip("Multiplicateur de dégâts appliqué à chaque ennemi spawné")]
    public AnimationCurve DamageCurve = new AnimationCurve(
        new Keyframe(0f,   1f),
        new Keyframe(60f,  1.5f),
        new Keyframe(180f, 3f));

    [LabelText("PV max (x=s, y=PV)")]
    [Tooltip("Valeur absolue des PV max au moment du spawn. X=0 = premier spawn (après délai initial).")]
    public AnimationCurve HealthCurve = new AnimationCurve(
        new Keyframe(0f,   50f),
        new Keyframe(60f,  80f),
        new Keyframe(180f, 200f));

    // ── Valeurs courantes (lecture seule, pilotées par les courbes) ────────────
    [Title("Valeurs courantes")]
    [ShowInInspector, ReadOnly, LabelText("Intervalle actuel (s)")]
    public float SpawnInterval  { get; private set; } = 0.8f;

    [ShowInInspector, ReadOnly, LabelText("Ennemis/vague")]
    public int   EnemiesPerWave { get; private set; } = 3;

    [ShowInInspector, ReadOnly, LabelText("Scale")]
    public float CurrentScale   { get; private set; } = 1f;

    [ShowInInspector, ReadOnly, LabelText("Multiplicateur dégâts")]
    public float CurrentDamage  { get; private set; } = 1f;

    [ShowInInspector, ReadOnly, LabelText("PV max")]
    public float CurrentHealth  { get; private set; } = 25f;

    public void SetDangerLevel(float dangerSeconds)
    {
        // Courbes de spawn : utilisent le temps global (la pression monte dès le début)
        SpawnInterval  = Mathf.Max(0.1f, EvaluateInfinite(SpawnIntervalCurve,  dangerSeconds));
        EnemiesPerWave = Mathf.Max(1,    Mathf.RoundToInt(EvaluateInfinite(EnemiesPerWaveCurve, dangerSeconds)));

        // Courbes de stats : offset par InitialDelay pour que t=0 corresponde au premier spawn
        float statTime = Mathf.Max(0f, dangerSeconds - InitialDelay);
        CurrentScale   = Mathf.Max(0.1f, EvaluateInfinite(ScaleCurve,   statTime));
        CurrentDamage  = Mathf.Max(0f,   EvaluateInfinite(DamageCurve,  statTime));
        CurrentHealth  = Mathf.Max(1f,   EvaluateInfinite(HealthCurve,  statTime));
    }

    // Prolonge la courbe à l'infini en extrapolant linéairement avec la pente de fin.
    // Evite le clamp Unity qui bloque à la dernière keyframe.
    private static float EvaluateInfinite(AnimationCurve curve, float t)
    {
        if (curve.length == 0) return 0f;

        Keyframe last = curve[curve.length - 1];
        if (t <= last.time) return curve.Evaluate(t);

        // Pente approchée sur la dernière seconde de la courbe
        float sampleBack = Mathf.Max(0f, last.time - 1f);
        float slope      = (last.value - curve.Evaluate(sampleBack)) / (last.time - sampleBack);

        return last.value + slope * (t - last.time);
    }

    // ── Debug ──────────────────────────────────────────────────────────────────
    [FoldoutGroup("Debug"), ShowInInspector, ReadOnly, LabelText("Ennemis vivants")]
    private int DebugAliveCount => _spawnedEnemies.Count(e => e != null && e.activeInHierarchy);

    [FoldoutGroup("Debug"), ShowInInspector, ReadOnly, LabelText("Spawn actif")]
    private bool DebugIsRunning => _spawnCoroutine != null;

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

        // Initialise les valeurs depuis les courbes à t=0
        SetDangerLevel(0f);

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
        if (InitialDelay > 0f)
            yield return new WaitForSeconds(InitialDelay);

        while (true)
        {
            yield return new WaitForSeconds(SpawnInterval);

            if (_player == null || EnemyPrefab == null) continue;

            if (Mode == SpawnMode.Limité)
            {
                // activeInHierarchy gère le cas pool (GO inactif ≠ null)
                _spawnedEnemies.RemoveAll(e => e == null || !e.activeInHierarchy);
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

            var enemy = EnemyPool.Instance != null
                ? EnemyPool.Instance.Get(EnemyPrefab, pos, Quaternion.identity, _spawnParent)
                : Instantiate(EnemyPrefab, pos, Quaternion.identity, _spawnParent);

            enemy.transform.localScale = Vector3.one * CurrentScale;
            enemy.GetComponent<EnemyMeleeAttack>()?.ApplyMultiplier(CurrentDamage);
            enemy.GetComponent<EnemyHealth>()?.SetMaxHealth(CurrentHealth);

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
